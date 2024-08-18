# Sträng i orientformat från en stränginmatningsförlängning.

<!--category-- C# -->
<datetime class="hidden">2024-08-07T17:17</datetime>

## Små inlägg FTW

Liten men potentiellt användbar lösning på ett problem jag hade. Nämligen, hur man skapar ett GUID från en stränginmatning där Guid alltid är giltig men unik för varje given inmatningssträng.

Jag behövde den här för min [RSS-flödesgenerator](/blog/addinganrssfilewithaspnetcore) där jag ville generera en GUID för varje objekt i fodret som var repeterbar men unik för varje objekt.

Det visar sig att `XxHash128` är ganska perfekt för detta eftersom det alltid ger en 128 bitars (eller 16 Byte) hasch. Detta innebär att det kan användas för att generera ett GUID från en stränginmatning utan 'Array'.Copy nonsens.

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

Detta är en enkel förlängningsmetod som tar en stränginmatning och returnerar en GUID. I detta sammanhang är det viktigt att se till att `XxHash128` är från `System.IO.Hashing` Namnrymd.

Du kan naturligtvis använda alla hashing algoritm som ger en 128 bit hash. I detta sammanhang är det viktigt att se till att `XxHash128` är bara ett bra val eftersom det är snabbt och ger en bra fördelning av hashvärden.

Du kan också lämna tillbaka en `new Guid(<string>)` från detta för att få en verklig Guid som kan användas i en databas eller andra GUID specifika användningsfall.