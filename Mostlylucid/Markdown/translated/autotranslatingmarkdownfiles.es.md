# Traduciendo automáticamente archivos de marcado con EasyNMT

<datetime class="hidden">2024-08-03T13:30</datetime>

<!--category-- EasyNMT, Markdown -->
## Introducción

EasyNMT es un servicio instalable localmente que proporciona una interfaz sencilla a una serie de servicios de traducción automática. En este tutorial, usaremos EasyNMT para traducir automáticamente un archivo Markdown del inglés a varios idiomas.

Usted puede encontrar todos los archivos de este tutorial en el [Repositorio GitHub](https://github.com/scottgal/mostlylucidweb/tree/main/Mostlylucid/MarkdownTranslator) para este proyecto.

La salida de esto generó un BUNCH de nuevos archivos Markdown en los idiomas de destino. Esta es una manera súper simple de conseguir una entrada de blog traducida a varios idiomas.

[Mensajes traducidos](/translatedposts.png)

[TOC]

## Requisitos previos

Se requiere una instalación de EasyNMT para seguir este tutorial. Usualmente lo ejecuto como un servicio Docker. Puede encontrar las instrucciones de instalación [aquí](https://github.com/UKPLab/EasyNMT/blob/main/docker/README.md) que cubre cómo ejecutarlo como un servicio de docker.

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0-cpu
```

O si tiene una GPU NVIDIA disponible:

```shell
docker run -d -p 24080:80 --env MAX_WORKERS_BACKEND=6 --env MAX_WORKERS_FRONTEND=6 easynmt/api:2.0.2-cuda11.3
```

Las variables de entorno MAX_WORKERS_BACKEND y MAX_WORKERS_FRONTEND establecen el número de trabajadores que EasyNMT utilizará. Puede ajustarlos para que se adapten a su máquina.

NOTA: EasyNMT no es el servicio SMOOTHEST para funcionar, pero es el mejor que he encontrado para este propósito. Es un poco persnickety sobre la cadena de entrada que se ha pasado, por lo que es posible que tenga que hacer un poco de pre-procesamiento de su texto de entrada antes de pasarlo a EasyNMT.

Oh y también tradujo "Conclusión" a algunas tonterías sobre la presentación de la propuesta a la UE...traicionar su conjunto de formación.

## Enfoque ingenuo para equilibrar la carga

Easy NMT es una bestia sed cuando se trata de recursos, así que en mi MarkdownTranslatorService tengo un selector de IP súper simple al azar que gira a través de la lista de IPs de una lista de máquinas que utilizo para ejecutar EasyNMT.

Inicialmente esto hace un get en el `model_name` método en el servicio EasyNMT, esta es una forma rápida y sencilla de comprobar si el servicio está terminado. Si lo es, añade la IP a una lista de IPs en funcionamiento. Si no lo es, no lo añade a la lista.

```csharp
    private string[] IPs = translateServiceConfig.IPs;
    public async ValueTask<bool> IsServiceUp(CancellationToken cancellationToken)
    {
        var workingIPs = new List<string>();

        try
        {
            foreach (var ip in IPs)
            {
                logger.LogInformation("Checking service status at {IP}", ip);
                var response = await client.GetAsync($"{ip}/model_name", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    workingIPs.Add(ip);
                }
            }

            IPs = workingIPs.ToArray();
            if (!IPs.Any()) return false;
            return true;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error checking service status");
            return false;
        }
    }
```

Entonces dentro de la `Post` método de `MarkdownTranslatorService` rotamos a través de las IPs de trabajo para encontrar la siguiente.

```csharp
          if(!IPs.Any())
            {
                logger.LogError("No IPs available for translation");
                throw new Exception("No IPs available for translation");
            }
            var ip = IPs[currentIPIndex];
            
            logger.LogInformation("Sending request to {IP}", ip);
        
            // Update the index for the next request
            currentIPIndex = (currentIPIndex + 1) % IPs.Length;
```

Esta es una manera súper simple de cargar el equilibrio de las peticiones a través de una serie de máquinas. No es perfecto (no cuenta con una máquina súper ocupada para el examen), pero es lo suficientemente bueno para mis propósitos.

El schmick ` currentIPIndex = (currentIPIndex + 1) % IPs.Length;` sólo gira a través de la lista de IPs a partir de 0 y va a la longitud de la lista.

## Traducción de un archivo Markdown

Este es el código que tengo en el archivo MarkdownTranslatorService.cs. Es un servicio simple que toma una cadena de marcado y un idioma de destino y devuelve la cadena de marcado traducido.

```csharp
    public async Task<string> TranslateMarkdown(string markdown, string targetLang, CancellationToken cancellationToken)
    {
        var document = Markdig.Markdown.Parse(markdown);
        var textStrings = ExtractTextStrings(document);
        var batchSize = 10;
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

1. `  var document = Markdig.Markdown.Parse(markdown);` - Esto analiza la cadena Markdown en un documento.
2. `  var textStrings = ExtractTextStrings(document);` - Esto extrae las cadenas de texto del documento.
   Esto utiliza el método

```csharp
  private bool IsWord(string text)
    {
        var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg" };
        if (imageExtensions.Any(text.Contains)) return false;
        return text.Any(char.IsLetter);
    } 
```

Esto comprueba si la 'palabra' es realmente una obra; los nombres de imágenes pueden arruinar la funcionalidad de división de frases en EasyNMT.

3. `  var batchSize = 10;` - Esto establece el tamaño del lote para el servicio de traducción. EasyNMT tiene un límite en el número de palabras que puede traducir de una sola vez (alrededor de 500, por lo que 10 líneas es generalmente un buen tamaño de lote aquí).
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

5. `  ReinsertTranslatedStrings(document, translatedStrings.ToArray());` - Esto reinserta las cadenas traducidas en el documento. Usando la capacidad de MarkDig para caminar el documento y reemplazar cadenas de texto.

```csharp

    private void ReinsertTranslatedStrings(MarkdownDocument document, string[] translatedStrings)
    {
        int index = 0;

        foreach (var node in document.Descendants())
        {
            if (node is LiteralInline literalInline && index < translatedStrings.Length)
            {
                var content = literalInline.Content.ToString();
         
                if (!IsWord(content)) continue;
                literalInline.Content = new Markdig.Helpers.StringSlice(translatedStrings[index]);
                index++;
            }
        }
    }
```

## Servicio alojado

Para ejecutar todo esto utilizo un IHostedLifetimeService que se inicia en el archivo Program.cs. Este servicio lee en un archivo Markdown, lo traduce a varios idiomas y escribe los archivos traducidos al disco.

```csharp
  public async Task StartedAsync(CancellationToken cancellationToken)
    {
        if(!await blogService.IsServiceUp(cancellationToken))
        {
            logger.LogError("Translation service is not available");
            return;
        }
        ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = blogService.IPCount, CancellationToken = cancellationToken};
        var files = Directory.GetFiles(markdownConfig.MarkdownPath, "*.md");

        var outDir = markdownConfig.MarkdownTranslatedPath;

        var languages = translateServiceConfig.Languages;
        foreach(var language in languages)
        {
            await Parallel.ForEachAsync(files, parallelOptions, async (file,ct) =>
            {
                var fileChanged = await file.IsFileChanged(outDir);
                var outName = Path.GetFileNameWithoutExtension(file);

                var outFileName = $"{outDir}/{outName}.{language}.md";
                if (File.Exists(outFileName) && !fileChanged)
                {
                    return;
                }

                var text = await File.ReadAllTextAsync(file, cancellationToken);
                try
                {
                    logger.LogInformation("Translating {File} to {Language}", file, language);
                    var translatedMarkdown = await blogService.TranslateMarkdown(text, language, ct);
                    await File.WriteAllTextAsync(outFileName, translatedMarkdown, cancellationToken);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error translating {File} to {Language}", file, language);
                }
            });
        }
       
```

Como se puede ver también comprueba el hash del archivo para ver si ha cambiado antes de traducirlo. Esto es para evitar traducir archivos que no han cambiado.

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
});
```

Configuro el HostedService (BackgroundTranslateService) y el HttpClient para el MarkdownTranslatorService.
Un Servicio Hosted es un servicio de larga duración que funciona en segundo plano. Es un buen lugar para poner servicios que necesitan funcionar continuamente en segundo plano o simplemente tomar un tiempo para completar. La nueva interfaz de IHostedLifetimeService es un poco más flexible que la antigua interfaz de IHostedService y nos permite ejecutar tareas completamente en segundo plano más fácilmente que la anterior IHostedService.

Aquí pueden ver que estoy fijando el tiempo de espera para el HttpClient en 15 minutos. Esto se debe a que EasyNMT puede ser un poco lento para responder (especialmente la primera vez que se utiliza un modelo de idioma). También estoy configurando la dirección base a la dirección IP de la máquina que ejecuta el servicio EasyNMT.

## Conclusión

Esta es una manera bastante simple de traducir un archivo Markdown a varios idiomas. No es perfecto, pero es un buen comienzo. Generalmente ejecuto esto para cada nuevo post del blog y se utiliza en el `MarkdownBlogService` para sacar los nombres traducidos para cada entrada de blog.