# Browser WebAssembly sketch, not a validated build

These are overlay files for a .NET 10 `wasmbrowser` project, not a published browser scheduler. No `wasm-tools` workload is installed in the environment used for the article, so this route was researched but not built or executed.

Start with Microsoft's WebAssembly Browser App template:

```shell
dotnet workload install wasm-tools
dotnet new install Microsoft.NET.Runtime.WebAssembly.Templates
dotnet new wasmbrowser -n BrowserInteropDemo
```

Copy `BrowserInterop.cs` into that project and replace its generated JS entry module with the `main.mjs` wrapper here, adjusting its filename to the template's asset conventions. Keep `AllowUnsafeBlocks` enabled. Remove/replace the generated greeting/DOM sample as needed. Publish with `dotnet publish -c Release`. To request WebAssembly AOT, use the template/SDK's `RunAOTCompilation` publish property and verify the resulting artifact. This is different from Native AOT's `PublishAot` switch.

Serve the complete generated runtime asset directory beside `main.mjs`. Then an existing JavaScript app can call:

```javascript
const { createInterop } = await import('./main.mjs');
const names = new Map([['alice', 'Alice'], ['sarah', 'Sarah']]);
const interop = await createInterop(id => names.get(id) ?? id);
console.log(interop.DescribeAppointment('alice', 'gels', 'sarah'));
```

The attributes here come from `System.Runtime.InteropServices.JavaScript`, not `Microsoft.JavaScript.NodeApi`. Node's `.node` addon cannot be loaded in a browser. A real browser scheduler needs a browser-compatible pure C# facade and a separate data-marshalling adapter. The JavaScript app fetches a snapshot; the server's existing booking API remains authoritative for committing it.

The current packing core uses reflection-based JSON serialization internally for canonical IDs and queries. Its AOT/trimming compatibility still needs work. This proof snippet deliberately exports only a name lookup callback, not the complete engine. Putting computation into a Web Worker and transferring a snapshot would avoid blocking the calendar UI, but no Worker implementation or WASM performance claim is supplied here.
