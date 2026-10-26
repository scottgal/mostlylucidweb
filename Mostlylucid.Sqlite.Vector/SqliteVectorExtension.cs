using System.Runtime.InteropServices;

namespace Mostlylucid.Sqlite.Vector;

/// <summary>Locates the NuGet-provided SQLite extension without reflection, extraction or runtime downloads.</summary>
public static class SqliteVectorExtension
{
    /// <summary>The explicit SQLite entry point exported by the pinned upstream build.</summary>
    public const string EntryPoint = "sqlite3_sqlitevectorrs_init";
    /// <summary>Pinned upstream source identity, retained alongside packaged native assets.</summary>
    public const string Source = "sqlite-vector-rs 0.3.1 / 7ee2baf7b2783c1798f285cdff3b2285da38de04";

    /// <summary>Absolute native path for the current supported platform; fails clearly if the asset is missing.</summary>
    /// <remarks>Initial package targets linux-x64, osx-arm64 and win-x64. Native AOT still requires its native dependencies.</remarks>
    public static string Path
    {
        get
        {
            var rid = (OperatingSystem.IsMacOS(), OperatingSystem.IsLinux(), OperatingSystem.IsWindows(), RuntimeInformation.ProcessArchitecture) switch
            {
                (true, _, _, Architecture.Arm64) => "osx-arm64",
                (_, true, _, Architecture.X64) => "linux-x64",
                (_, _, true, Architecture.X64) => "win-x64",
                _ => throw new PlatformNotSupportedException("SQLite vector assets currently support osx-arm64, linux-x64 and win-x64.")
            };
            var name = OperatingSystem.IsWindows() ? "sqlite_vector_rs.dll"
                : OperatingSystem.IsMacOS() ? "libsqlite_vector_rs.dylib" : "libsqlite_vector_rs.so";
            foreach (var path in new[] { System.IO.Path.Combine(AppContext.BaseDirectory, name),
                         System.IO.Path.Combine(AppContext.BaseDirectory, "runtimes", rid, "native", name) })
                if (File.Exists(path)) return path;
            throw new FileNotFoundException($"SQLite vector native asset missing for {rid}. Reference Mostlylucid.Sqlite.Vector and publish for that RID.", name);
        }
    }
}
