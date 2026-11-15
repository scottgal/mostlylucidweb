import { dotnet } from './_framework/dotnet.js';

export async function createInterop(getName) {
    const runtime = await dotnet.create();
    runtime.setModuleImports('calendarApi', { getName });
    const exports = await runtime.getAssemblyExports(runtime.getConfig().mainAssemblyName);
    return exports.BrowserInterop;
}
