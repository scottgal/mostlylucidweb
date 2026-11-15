import { readFile } from 'node:fs/promises';

const result = JSON.parse(await readFile(new URL('./results.json', import.meta.url), 'utf8'));
const median = values => {
    const sorted = values.toSorted((a, b) => a - b);
    const middle = Math.floor(sorted.length / 2);
    return sorted.length % 2 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
};

console.log('| Work | Path | p50 µs | p95 µs | Sequential calls/s |');
console.log('| --- | --- | ---: | ---: | ---: |');
for (const work of ['control', 'packing']) {
    for (const mode of ['interop-typed', 'interop-json', 'grpc']) {
        const runs = result.runs.filter(run => run.mode === mode);
        const value = key => median(runs.map(run => run[work][key]));
        console.log(`| ${work} | ${mode} | ${value('p50Us').toFixed(1)} | ${value('p95Us').toFixed(1)} | ${Math.round(value('sequentialCallsPerSecond'))} |`);
    }
}

console.log('\n| Path | Initialized RSS MiB | Loaded RSS MiB |');
console.log('| --- | ---: | ---: |');
for (const mode of ['interop-typed', 'interop-json', 'grpc']) {
    const runs = result.runs.filter(run => run.mode === mode);
    const value = stage => median(runs.map(run =>
        (run[stage].nodeRssBytes + (mode === 'grpc' ? run[stage].runtime.workingSetBytes : 0)) / 1048576));
    console.log(`| ${mode} | ${value('initialized').toFixed(1)} | ${value('loaded').toFixed(1)} |`);
}
