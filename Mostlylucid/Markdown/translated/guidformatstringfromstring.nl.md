# Guid formaat string van een string input extensie.

<!--category-- C# -->
<datetime class="hidden">2024-08-07T17:17</datetime>

## Kleine berichten FTW

Kleine maar potentieel nuttige oplossing voor een probleem dat ik had. Namelijk, hoe een GUID te genereren van een string input waar de Guid is altijd geldig maar uniek voor een bepaalde invoer string.

Ik had dit nodig voor mijn[RSS-feedgenerator](/blog/addinganrssfilewithaspnetcore)waar ik een GUID wilde genereren voor elk item in de feed dat herhaalbaar maar uniek was voor elk item.

Het blijkt dat de`XxHash128`is hier een beetje perfect voor omdat het altijd een 128 bit (of 16 Byte) hash geeft. Dit betekent dat het gebruikt kan worden om een GUID te genereren uit een string input zonder 'Array'.

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

Dit is een eenvoudige extensie methode die een string input neemt en een GUID teruggeeft.`XxHash128`is van de`System.IO.Hashing`namespace.

U kunt natuurlijk gebruik maken van elk hashing algoritme dat geeft een 128 bit hash.`XxHash128`is gewoon een goede keuze als het is snel en geeft een goede verdeling van hash waarden.

U kunt ook een`new Guid(<string>)`van dit om een werkelijke Guid die kan worden gebruikt in een database of andere GUID specifieke gebruik gevallen te krijgen.