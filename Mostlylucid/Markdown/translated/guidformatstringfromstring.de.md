# Guid Format String aus einer String-Eingabe-Erweiterung.

<!--category-- C# -->
<datetime class="hidden">2024-08-07T17:17</datetime>

## Kleine Posts FTW

Kleine, aber potenziell nützliche Lösung für ein Problem, das ich hatte. Nämlich, wie man eine GUID aus einem String-Input generiert, bei dem der Guid immer gültig ist, aber für jeden angegebenen Eingabestring einzigartig ist.

Ich brauchte das für meine[RSS-Feed-Generator](/blog/addinganrssfilewithaspnetcore)wo ich für jedes Element im Feed eine GUID generieren wollte, die aber für jedes Element wiederholbar war.

Es stellt sich heraus, dass die`XxHash128`ist für diese Art perfekt, da es immer einen 128 Bit (oder 16 Byte) Hash gibt. Dies bedeutet, dass es verwendet werden kann, um eine GUID aus einem String-Eingang ohne 'Array' zu generieren.Kopieren Sie Unsinn.

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

Dies ist eine einfache Erweiterungsmethode, die eine String-Eingabe benötigt und eine GUID zurückgibt.`XxHash128`ist aus der`System.IO.Hashing`Namespace.

Sie können natürlich jeden Hashing-Algorithmus verwenden, der einen 128 bit Hash gibt.`XxHash128`ist nur eine gute Wahl, da es schnell ist und gibt eine gute Verteilung der Hash-Werte.

Sie könnten auch eine`new Guid(<string>)`aus diesem, um eine tatsächliche Guid zu erhalten, die in einer Datenbank oder anderen GUID spezifischen Anwendungsfällen verwendet werden kann.