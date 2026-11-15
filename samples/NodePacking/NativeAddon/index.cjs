const { join } = require('node:path');
const os = { darwin: 'osx', linux: 'linux', win32: 'win' }[process.platform];
if (!os) throw new Error(`Unsupported platform: ${process.platform}`);
let rid = `${os}-${process.arch}`;
if (process.platform === 'linux' && !process.report.getReport().header.glibcVersionRuntime)
    rid = `linux-musl-${process.arch}`;
module.exports = require(join(__dirname, 'native', rid, 'NativeInterop.node'));
