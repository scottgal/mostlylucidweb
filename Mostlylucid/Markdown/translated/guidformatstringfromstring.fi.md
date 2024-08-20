# Guid-muodossa oleva merkkijono merkkijonon tulolaajennuksesta.

<!--category-- C# -->
<datetime class="hidden">2024-08-07-T17:17</datetime>

## Pienet postit FTW

Pieni mutta mahdollisesti hyödyllinen ratkaisu ongelmaan, joka minulla oli. Erityisesti, miten luoda GUID merkkijono syötöstä, jossa Guid on aina pätevä, mutta ainutlaatuinen minkä tahansa syötejon kohdalla.

Tarvitsin tätä omaani varten. [RSS-syötegeneraattori](/blog/addinganrssfilewithaspnetcore) Jossa halusin luoda jokaiselle syötteessä olevalle esineelle GUID:n, joka oli toistettavissa mutta ainutlaatuinen jokaiselle esineelle.

Kävi ilmi, että `XxHash128` on tavallaan täydellinen tähän, koska se antaa aina 128 bittiä (tai 16 Byte) hasista. Tämä tarkoittaa, että sitä voidaan käyttää GUID:n tuottamiseen merkkijonon syötöstä ilman Arrayta.Copy-hölynpölyä.

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

Tämä on yksinkertainen laajennusmenetelmä, joka vie merkkijonon syötteeseen ja palauttaa GUIDin. Erytropoietiini `XxHash128` on peräisin `System.IO.Hashing` namespace.

Voit tietysti käyttää mitä tahansa hashing-algoritmia, joka antaa 128 bittiä hashia. Erytropoietiini `XxHash128` on vain hyvä valinta, koska se on nopea ja antaa hyvän hasisarvojen jakautumisen.

Voit myös palauttaa `new Guid(<string>)` Tästä saat varsinaisen Guidin, jota voi käyttää tietokannassa tai muissa GUID-erityiskäyttötapauksissa.