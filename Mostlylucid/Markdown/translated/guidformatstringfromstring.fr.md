# Chaîne de format Guid à partir d'une extension d'entrée de chaîne.

<!--category-- C# -->
<datetime class="hidden">2024-08-07T17:17</datetime>

## Petits postes FTW

Une solution petite mais potentiellement utile à un problème que j'avais. À savoir, comment générer un GUID à partir d'une entrée de chaîne où la Guid est toujours valide mais unique pour toute chaîne d'entrée donnée.

J'avais besoin de ça pour mon [Générateur de flux RSS](/blog/addinganrssfilewithaspnetcore) où je voulais générer un GUID pour chaque élément du flux qui était répétable mais unique pour chaque élément.

Il s'avère que les `XxHash128` est un peu parfait pour cela car il donne toujours un hachage de 128 bits (ou 16 octets). Cela signifie qu'il peut être utilisé pour générer un GUID à partir d'une entrée de chaîne sans 'Array'.

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

Il s'agit d'une méthode d'extension simple qui prend une entrée de chaîne et renvoie un GUID. Les `XxHash128` est de la `System.IO.Hashing` Espace de noms.

Vous pouvez bien sûr utiliser n'importe quel algorithme de hachage qui donne un hachage 128 bits. Les `XxHash128` est juste un bon choix car il est rapide et donne une bonne distribution des valeurs de hachage.

Vous pourriez aussi retourner un `new Guid(<string>)` à partir de cela pour obtenir un Guid réel qui peut être utilisé dans une base de données ou d'autres cas d'utilisation spécifiques GUID.