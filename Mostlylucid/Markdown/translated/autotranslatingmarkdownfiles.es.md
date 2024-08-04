# Traduciendo automáticamente archivos de marcado con EasyNMT

## Introducción

EasyNMT es un servicio instalable localmente que proporciona una interfaz sencilla a una serie de servicios de traducción automática. En este tutorial, utilizaremos EasyNMT para traducir automáticamente un archivo Markdown del inglés a varios idiomas.

Usted puede encontrar todos los archivos de este tutorial en el[Repositorio GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/MarkdownTranslator)para este proyecto.

NOTA: Esto sigue siendo bastante duro, voy a seguir refinando a medida que voy.

Sólo he traducido este archivo en (bueno y[sobre mí](/blog/aboutme)a medida que refina el método; hay algunos problemas con la traducción que necesito resolver.

[TOC]

## Requisitos previos

Se requiere una instalación de EasyNMT para seguir este tutorial. Usualmente lo ejecuto como un servicio Docker. Puede encontrar las instrucciones de instalación[aquí](https://github.com/UKPLab/EasyNMT/blob/main/docker/README.md)que cubre cómo ejecutarlo como un servicio de docker.

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0-cpu
```

O si tiene una GPU NVIDIA disponible:

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0.2-cuda11.3
```

Las variables de entorno MAX_WORKERS_BACKEND y MAX_WORKERS_FRONTEND establecen el número de trabajadores que EasyNMT utilizará. Puede ajustarlos para adaptarlos a su máquina.

NOTA: EasyNMT no es el servicio SMOOTHEST para ejecutar, pero es el mejor que he encontrado para este propósito. Es un poco persnickety sobre la cadena de entrada que se ha pasado, por lo que es posible que tenga que hacer un poco de pre-procesamiento de su texto de entrada antes de pasarlo a EasyNMT.

## Enfoque ingenuo para equilibrar la carga

Easy NMT es una bestia sed cuando se trata de recursos, por lo que en mi MarkdownTranslatorService tengo un selector de IP súper simple al azar que sólo toma la IP al azar de la lista de máquinas que tengo. Esto es un poco ingenuo y podría ser mejorado mediante el uso de un algoritmo de equilibrio de carga más sofisticado.

```csharp
    private string[] IPs = new[] { "http://192.168.0.30:24080", "http://localhost:24080", "http://192.168.0.74:24080" };

     var ip = IPs[random.Next(IPs.Length)];
     logger.LogInformation("Sendign request to {IP}", ip);
     var response = await client.PostAsJsonAsync($"{ip}/translate", postObject, cancellationToken);

```

## Traducción de un archivo Markdown

Este es el código que tengo en el archivo MarkdownTranslatorService.cs. Es un servicio simple que toma una cadena Markdown y un idioma de destino y devuelve la cadena Markdown traducida.

```csharp
    public async Task<string> TranslateMarkdown(string markdown, string targetLang, CancellationToken cancellationToken)
    {
        var document = Markdig.Markdown.Parse(markdown);
        var textStrings = ExtractTextStrings(document);
        var batchSize = 50;
        var stringLength = textStrings.Count;
        List<string> translatedStrings = new();
        for (int i = 0; i < stringLength; i += batchSize)
        {
            var batch = textStrings.Skip(i).Take(batchSize).ToArray();
            translatedStrings.AddRange(await Post(batch, targetLang, cancellationToken));
        }


        ReinsertTranslatedStrings(document, translatedStrings.ToArray());
        return document.ToMarkdownString();
    }
```

Como puede ver, tiene una serie de pasos:

1. `  var document = Markdig.Markdown.Parse(markdown);`- Esto analiza la cadena Markdown en un documento.
2. `  var textStrings = ExtractTextStrings(document);`- Esto extrae las cadenas de texto del documento.
3. `  var batchSize = 50;`- Esto establece el tamaño del lote para el servicio de traducción. EasyNMT tiene un límite en el número de caracteres que puede traducir de una sola vez.
4. `csharp await Post(batch, targetLang, cancellationToken)`
   Esto llama al método que luego envía el lote al servicio EasyNMT.

```csharp
    private async Task<string[]> Post(string[] elements, string targetLang, CancellationToken cancellationToken)
    {
        try
        {
            var postObject = new PostRecord(targetLang, elements);
            var response = await client.PostAsJsonAsync("/translate", postObject, cancellationToken);

            var phrase = response.ReasonPhrase;
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<PostResponse>(cancellationToken: cancellationToken);

            return result.translated;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error translating markdown: {Message} for strings {Strings}", e.Message, string.Concat( elements, Environment.NewLine));
            throw;
        }
    }
```

5. `  ReinsertTranslatedStrings(document, translatedStrings.ToArray());`- Esto reinserta las cadenas traducidas de nuevo en el documento. Usando la capacidad de MarkDig para caminar el documento y reemplazar las cadenas de texto.

## Servicio alojado

Para ejecutar todo esto utilizo un IHostedLifetimeService que se inicia en el archivo Program.cs. Este servicio lee en un archivo Markdown, lo traduce a varios idiomas y escribe los archivos traducidos al disco.

```csharp
    public async Task StartedAsync(CancellationToken cancellationToken)
    {
        var files = Directory.GetFiles("Markdown", "*.md");

        var outDir = "Markdown/translated";

        var languages = new[] { "es", "fr", "de", "it", "jap", "uk", "zh" };
        foreach (var language in languages)
        {
            foreach (var file in files)
            {
                var fileChanged = await file.IsFileChanged(outDir);
                var outName = Path.GetFileNameWithoutExtension(file);

                var outFileName = $"{outDir}/{outName}.{language}.md";
                if (File.Exists(outFileName) && !fileChanged)
                {
                    continue;
                }

                var text = await File.ReadAllTextAsync(file, cancellationToken);
                try
                {
                    logger.LogInformation("Translating {File} to {Language}", file, language);
                    var translatedMarkdown = await blogService.TranslateMarkdown(text, language, cancellationToken);
                    await File.WriteAllTextAsync(outFileName, translatedMarkdown, cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error translating {File} to {Language}", file, language);
                }
            }
        }

        logger.LogInformation("Background translation service started");
    }
```

Como puede ver también comprueba el hash del archivo para ver si ha cambiado antes de traducirlo. Esto es para evitar traducir archivos que no han cambiado.

Esto se hace computando un hash rápido del archivo Markdown original y luego probando para ver si ese archivo ha cambiado antes de intentar traducirlo.

```csharp
    private static async Task<string> ComputeHash(string filePath)
    {
        await using var stream = File.OpenRead(filePath);
        stream.Position = 0;
        var bytes = new byte[stream.Length];
        await stream.ReadAsync(bytes);
        stream.Position = 0;
        var hash = XxHash64.Hash(bytes);
        var hashString = Convert.ToBase64String(hash);
        hashString = InvalidCharsRegex.Replace(hashString, "_");
        return hashString;
    }
```

La configuración en Program.cs es bastante simple:

```csharp

    builder.Services.AddHostedService<BackgroundTranslateService>();
services.AddHttpClient<MarkdownTranslatorService>(options =>
{
    options.Timeout = TimeSpan.FromMinutes(15);
    options.BaseAddress = new Uri("http://192.168.0.30:24080");
});
```

Configuro el HostedService (BackgroundTranslateService) y el HttpClient para el MarkdownTranslatorService.
Un Servicio Hosted es un servicio de larga duración que se ejecuta en segundo plano. Es un buen lugar para poner servicios que necesitan ejecutarse continuamente en segundo plano o simplemente tomar un tiempo para completar. La nueva interfaz de IHostedLifetimeService es un poco más flexible que la antigua interfaz de IHostedService y nos permite ejecutar tareas completamente en segundo plano más fácilmente que el antiguo IHostedService.

Aquí puede ver que estoy configurando el tiempo de espera para el HttpClient a 15 minutos. Esto se debe a que EasyNMT puede ser un poco lento para responder (especialmente la primera vez que se utiliza un modelo de idioma). También estoy configurando la dirección base a la dirección IP de la máquina que ejecuta el servicio EasyNMT.

I