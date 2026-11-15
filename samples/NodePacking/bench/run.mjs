import { spawn, execFileSync } from 'node:child_process';
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { createInterface } from 'node:readline';
import { once } from 'node:events';
import { cpus, platform, arch, release, totalmem } from 'node:os';
import { fileURLToPath } from 'node:url';
import { readFile, writeFile } from 'node:fs/promises';

const root = fileURLToPath(new URL('../', import.meta.url));
const trials = Number(process.env.BENCH_TRIALS ?? 3);
const runs = [];

async function client(mode, address = '') {
    const child = spawn(process.execPath, ['bench/client.mjs', mode, address], { cwd: root, stdio: ['ignore', 'pipe', 'inherit'] });
    let output = '';
    child.stdout.on('data', data => output += data);
    const [code] = await once(child, 'exit');
    if (code !== 0) throw new Error(`${mode} client exited ${code}`);
    return JSON.parse(output.trim());
}

async function sidecar() {
    const child = spawn('dotnet', ['Sidecar/bin/Release/net10.0/Sidecar.dll'],
        { cwd: root, stdio: ['ignore', 'pipe', 'inherit'] });
    const address = await new Promise((resolve, reject) => {
        const timeout = setTimeout(() => { child.kill(); reject(new Error('Sidecar readiness timeout')); }, 20_000);
        child.once('exit', code => { clearTimeout(timeout); reject(new Error(`Sidecar exited ${code}`)); });
        const lines = createInterface({ input: child.stdout });
        lines.on('line', line => {
            if (line.startsWith('READY ')) { clearTimeout(timeout); resolve(new URL(line.slice(6)).host); }
        });
    });
    return { child, address };
}

for (let trial = 1; trial <= trials; trial++) {
    // Rotate all three paths so each gets a turn at each position.
    const modes = ['interop-typed', 'interop-json', 'grpc'];
    const offset = (trial - 1) % modes.length;
    const order = [...modes.slice(offset), ...modes.slice(0, offset)];
    runs.push({ trial, ...await client('baseline') });
    for (const mode of order) {
        let server;
        try {
            if (mode === 'grpc') server = await sidecar();
            const result = await client(mode, server?.address);
            runs.push({ trial, ...result });
            console.error(`Trial ${trial} ${mode}: packing p50=${result.packing.p50Us.toFixed(1)}us, p95=${result.packing.p95Us.toFixed(1)}us`);
        } finally {
            if (server) { server.child.kill('SIGTERM'); await once(server.child, 'exit'); }
        }
    }
    const pair = runs.filter(run => run.trial === trial && run.correctness);
    for (const run of pair.slice(1))
        assert.deepEqual(pair[0].correctness, run.correctness, 'Every path must return the same booking.');
}

const checksums = {};
for (const name of ['PackingBridge', 'Mostlylucid.Packing', 'Mostlylucid.Packing.Sqlite']) {
    const path = `${root}PackingBridge/bin/Release/net10.0/${name}.dll`;
    checksums[name] = createHash('sha256').update(await readFile(path)).digest('hex');
}
const result = { measuredAt: new Date().toISOString(), environment: {
    node: process.version, dotnetSdk: execFileSync('dotnet', ['--version'], { encoding: 'utf8' }).trim(),
    os: platform(), arch: arch(), osRelease: release(),
    cpu: cpus()[0].model, logicalCpus: cpus().length, totalMemoryBytes: totalmem()
}, artifactSha256: checksums, methodology: { trials, concurrency: 1, transport: 'loopback TCP, HTTP/2, plaintext; reused channel',
    warmupPerOperation: 200, garbageCollection: 'natural; .NET workstation GC in both modes',
    payload: 'same C# result model; typed JS property snapshot, JSON string, or JSON in protobuf',
    memory: 'RSS snapshots; sum client+sidecar for gRPC; shared pages may be counted twice' }, runs };
const output = fileURLToPath(new URL('./results.json', import.meta.url));
await writeFile(output, JSON.stringify(result, null, 2) + '\n');
console.log(`Saved ${output}`);
