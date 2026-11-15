# Native AOT npm addon proof

This builds a small C# function that calls a JavaScript name provider. It demonstrates local npm distribution of native code, not a complete Native AOT scheduler.

```shell
dotnet publish NativeAddon -c Release -r osx-arm64
node NativeAddon/test.cjs
```

Run those from `samples/NodePacking`. The publish produces `NativeAddon/native/osx-arm64/NativeInterop.node` and runs npm pack into `NativeAddon/pkg`. The package has no node-api-dotnet npm runtime dependency. Its loader selects the OS/architecture, including a distinct Linux musl RID.

Install the tarball into another Node project to test the distribution:

```shell
npm install /absolute/path/to/NativeAddon/pkg/mostlylucid-native-interop-demo-0.1.0.tgz
```

```javascript
const { describeAppointment } = require('@mostlylucid/native-interop-demo');
const names = new Map([['alice', 'Alice'], ['sarah', 'Sarah']]);
console.log(describeAppointment('alice', 'gels', 'sarah', id => names.get(id)));
```

The local binary, callback error and separately installed tarball were tested on macOS ARM64. Only that platform binary is included in the demonstrated package. Build other supported RIDs on appropriate OS/toolchain runners, collect their artifacts and pack the complete set. For a release, replace the private demo package metadata before publishing. No registry publication was performed.

RuntimeFrameworkVersion is pinned to 10.0.14 because the SDK's default compiler/runtime package produced an advisory warning. Do not treat the locally installed .NET SDK's patch as automatically suitable for a distributed native artifact.

The packing core currently uses reflection-based JSON serialization internally for canonical definitions, queries and IDs. Its complete Native AOT compatibility requires separate changes and published-artifact tests. This helper intentionally has no dependency on that core.
