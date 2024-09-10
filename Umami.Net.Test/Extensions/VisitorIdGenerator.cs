using System.IO.Hashing;
using System.Text;

namespace Umami.Net.Test.Extensions;

public static class VisitorIdGenerator
{
    //NOTE: THis isn't exactly the same format as the original code, but it's close enough for a test.
    public static string ToGuid(this string name)
    {
        var buf = Encoding.UTF8.GetBytes(name);
        var guid = XxHash128.Hash(buf);
        var guidS = string.Format(
            "{0:X2}{1:X2}{2:X2}{3:X2}-{4:X2}{5:X2}-{6:X2}{7:X2}-{8:X2}{9:X2}-{10:X2}{11:X2}{12:X2}{13:X2}{14:X2}{15:X2}",
            guid[0], guid[1], guid[2], guid[3], guid[4], guid[5], guid[6], guid[7], guid[8], guid[9], guid[10],
            guid[11], guid[12], guid[13], guid[14], guid[15]);
        return guidS.ToLowerInvariant();
    }
}