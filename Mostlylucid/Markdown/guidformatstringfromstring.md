# Guid format string from a string input extension.

<!--category-- C# -->
<datetime class="hidden">2024-08-07T17:17</datetime>

## Small Posts FTW
Small but potentially useful solution to a problem I was having. Namely, how to generate a GUID from a string input where the Guid is always valid but unique for any given input string.

I needed this for my [RSS feed generator](/blog/addinganrssfilewithaspnetcore) where I wanted to generate a GUID for each item in the feed which was repeatable but unique for each item.

It turns out that the `XxHash128` is kinda perfect for this as it always gives a 128 bit (or 16 Byte) hash. This means that it can be used to generate a GUID from a string input with no 'Array'.Copy nonsense. 

```csharp
     public  static string ToGuid(this string  name)
    {
        var buf = Encoding.UTF8.GetBytes(name);
        var guid = XxHash128.Hash(buf);
        var guidS =  string.Format("{0:X2}{1:X2}{2:X2}{3:X2}-{4:X2}{5:X2}-{6:X2}{7:X2}-{8:X2}{9:X2}-{10:X2}{11:X2}{12:X2}{13:X2}{14:X2}{15:X2}", 
            guid[0], guid[1], guid[2], guid[3], guid[4], guid[5], guid[6], guid[7], guid[8], guid[9], guid[10], guid[11], guid[12], guid[13], guid[14], guid[15]);
        return guidS.ToLowerInvariant();
    }
```

This is a simple extension method that takes a string input and returns a GUID. The `XxHash128` is from the `System.IO.Hashing` namespace.

You can of course use any hashing algorithm that gives a 128 bit hash. The `XxHash128` is just a good choice as it's fast and gives a good distribution of hash values.

You could also return a `new Guid(<string>)` from this to get an actual Guid that can be used in a database or other GUID specific use cases.