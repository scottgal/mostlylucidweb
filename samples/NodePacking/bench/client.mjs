import assert from 'node:assert/strict';
import { fileURLToPath } from 'node:url';
import { performance } from 'node:perf_hooks';
import { snapshotOffer } from '../offer-snapshot.mjs';

const mode = process.argv[2];
const address = process.argv[3];
const iterations = Number(process.env.BENCH_ITERATIONS ?? 1500);
const from = '2030-06-03T10:00:00Z';
const to = '2030-06-03T12:00:00Z';
const payload = JSON.stringify({ message: 'x'.repeat(1024) });
const started = performance.now();
let search, echo, runtimeMemory, close = () => {};

if (mode.startsWith('interop-')) {
    const { default: dotnet } = await import('node-api-dotnet/net10.0');
    const assembly = fileURLToPath(new URL('../PackingBridge/bin/Release/net10.0/PackingBridge.dll', import.meta.url));
    const { Salon, BenchmarkProbe } = dotnet.require(assembly);
    const salon = new Salon(':memory:');
    const window = { start: from, end: to };
    if (mode === 'interop-typed') {
        search = () => snapshotOffer(salon.findTogether(window));
        const value = { message: 'x'.repeat(1024) };
        echo = () => ({ message: BenchmarkProbe.echoTyped(value).message });
    } else {
        search = () => JSON.parse(salon.findTogetherJson(window));
        echo = () => JSON.parse(BenchmarkProbe.echo(payload));
    }
    runtimeMemory = () => JSON.parse(BenchmarkProbe.memory());
    close = () => salon.dispose();
} else if (mode === 'grpc') {
    const grpc = await import('@grpc/grpc-js');
    const loader = await import('@grpc/proto-loader');
    const definition = loader.loadSync(fileURLToPath(new URL('./packing.proto', import.meta.url)));
    const { packing } = grpc.loadPackageDefinition(definition);
    const client = new packing.PackingBench(address, grpc.credentials.createInsecure());
    const call = (method, request) => new Promise((resolve, reject) =>
        client[method](request, { deadline: Date.now() + 10_000 },
            (error, reply) => error ? reject(error) : resolve(reply.json)));
    await new Promise((resolve, reject) => client.waitForReady(Date.now() + 10_000,
        error => error ? reject(error) : resolve()));
    search = () => call('findTogether', { from, to }).then(JSON.parse);
    echo = () => call('echo', { json: payload }).then(JSON.parse);
    runtimeMemory = async () => JSON.parse(await call('memory', {}));
    close = () => client.close();
} else if (mode === 'baseline') {
    // No .NET or gRPC imports in this process.
    console.log(JSON.stringify({ mode, nodeRssBytes: process.memoryUsage().rss }));
    process.exit(0);
} else {
    throw new Error(`Unknown benchmark mode: ${mode}`);
}

const first = await search();
assert.equal(first.outcome, 'Found');
assert.equal(first.candidatesComplete, true);
assert.equal(first.appointments.length, 2);
assert.deepEqual(first.appointments.map(x => [x.clientId, x.serviceId, x.technician]),
    [['alice', 'gels', 'sarah'], ['beth', 'pedicure', 'anna']]);
assert.ok(first.appointments.every(x => Date.parse(x.start) === Date.parse(from)));
const readyMs = performance.now() - started;
const initialized = { nodeRssBytes: process.memoryUsage().rss, runtime: await runtimeMemory() };

async function measure(name, operation) {
    for (let i = 0; i < 200; i++) {
        if (mode === 'grpc') await operation();
        else operation();
    }
    const samples = [];
    const begin = process.hrtime.bigint();
    for (let i = 0; i < iterations; i++) {
        const at = process.hrtime.bigint();
        const parsed = mode === 'grpc' ? await operation() : operation();
        // Validate every result instead of timing failed/empty calls.
        if (name === 'packing') assert.equal(parsed.appointments.length, 2);
        else assert.equal(parsed.message.length, 1024);
        samples.push(Number(process.hrtime.bigint() - at) / 1000);
    }
    const elapsedMs = Number(process.hrtime.bigint() - begin) / 1e6;
    samples.sort((a, b) => a - b);
    const percentile = value => samples[Math.min(samples.length - 1, Math.floor(samples.length * value))];
    return { name, calls: iterations, meanUs: samples.reduce((a, b) => a + b, 0) / samples.length,
        p50Us: percentile(.5), p95Us: percentile(.95), p99Us: percentile(.99),
        sequentialCallsPerSecond: iterations / (elapsedMs / 1000) };
}

try {
    const control = await measure('echo-1KiB', echo);
    const packing = await measure('packing', search);
    const loaded = { nodeRssBytes: process.memoryUsage().rss, runtime: await runtimeMemory() };
    // A pending timer illustrates responsiveness during a 100-query burst.
    let timerDelay;
    const timerStart = performance.now();
    const timer = new Promise(resolve => setTimeout(() => {
        timerDelay = Math.max(0, performance.now() - timerStart - 10);
        resolve();
    }, 10));
    for (let i = 0; i < 100; i++) {
        if (mode === 'grpc') await search();
        else search();
    }
    await timer;
    console.log(JSON.stringify({ mode, iterations, readyMs, responseBytes: Buffer.byteLength(JSON.stringify(first)),
        correctness: { candidateId: first.candidateId, appointments: first.appointments },
        initialized, loaded, timerDelayDuring100QueriesMs: timerDelay, control, packing }));
} finally {
    close();
}
