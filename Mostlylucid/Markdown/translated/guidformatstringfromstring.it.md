# Stringa di formato guida da un'estensione di ingresso stringa.

<!--category-- C# -->
<datetime class="hidden">2024-08-07T17:17</datetime>

## Piccoli messaggi FTW

Piccola ma potenzialmente utile soluzione ad un problema che stavo avendo. Vale a dire, come generare un GUID da una stringa in ingresso dove la Guida è sempre valida ma unica per qualsiasi stringa in ingresso.

Avevo bisogno di questo per il mio [Generatore di alimentazione RSS](/blog/addinganrssfilewithaspnetcore) dove ho voluto generare un GUID per ogni elemento nel feed che era ripetibile ma unico per ogni elemento.

E' venuto fuori che... `XxHash128` è un po 'perfetto per questo come dà sempre un 128 bit (o 16 Byte) hash. Questo significa che può essere usato per generare un GUID da una stringa di input senza 'Array'.Copiare sciocchezze.

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

Questo è un metodo di estensione semplice che prende una stringa di input e restituisce un GUID. La `XxHash128` proviene dal `System.IO.Hashing` namespace.

Naturalmente è possibile utilizzare qualsiasi algoritmo di hashing che dà un hash 128 bit. La `XxHash128` è solo una buona scelta in quanto è veloce e dà una buona distribuzione dei valori di hash.

Si potrebbe anche restituire un `new Guid(<string>)` da questo per ottenere una guida reale che può essere utilizzato in un database o altri casi d'uso specifici GUID.