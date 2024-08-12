# Cadena de formato de guía de una extensión de entrada de cadena.

<!--category-- C# -->
<datetime class="hidden">2024-08-07T17:17</datetime>

## Puestos pequeños FTW

Solución pequeña pero potencialmente útil a un problema que estaba teniendo. Es decir, cómo generar un GUID a partir de una entrada de cadena donde el Guid es siempre válido pero único para cualquier cadena de entrada dada.

Necesitaba esto para mi[Generador de alimentación RSS](/blog/addinganrssfilewithaspnetcore)donde quería generar una GUIA para cada elemento en la fuente que era repetible pero único para cada elemento.

Resulta que la`XxHash128`es un poco perfecto para esto, ya que siempre da un hash de 128 bits (o 16 bytes). Esto significa que se puede utilizar para generar un GUID a partir de una entrada de cadena sin 'Array'.

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

Este es un método de extensión simple que toma una entrada de cadena y devuelve una GUID.`XxHash128`es de la`System.IO.Hashing`espacio de nombres.

Por supuesto, puede utilizar cualquier algoritmo de hashing que da un hash de 128 bit.`XxHash128`es sólo una buena opción, ya que es rápido y da una buena distribución de valores de hash.

También podría devolver un`new Guid(<string>)`de esto para obtener un Guid real que se puede utilizar en una base de datos u otros casos de uso específicos de GUID.