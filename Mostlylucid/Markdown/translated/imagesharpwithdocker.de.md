# ImageSharp mit Docker

<datetime class="hidden">2024-08-01T01:00</datetime>

<!--category-- Docker, ImageSharp -->
ImageSharp ist eine großartige Bibliothek für die Arbeit mit Bildern in.NET. Es ist schnell, einfach zu bedienen und hat viele Funktionen. In diesem Beitrag werde ich Ihnen zeigen, wie man ImageSharp mit Docker verwenden, um einen einfachen Bildverarbeitungsservice zu erstellen.

## Was ist ImageSharp?

ImageSharp ermöglicht es mir, nahtlos mit Bildern in.NET zu arbeiten. Es ist eine plattformübergreifende Bibliothek, die eine Vielzahl von Bildformaten unterstützt und eine einfache API für die Bildverarbeitung bietet. Es ist schnell, effizient und einfach zu bedienen.

Allerdings gibt es ein Problem in meinem Setup mit Docker und ImageSharp. Wenn ich versuche, ein Bild aus einer Datei zu laden, bekomme ich den folgenden Fehler:
'Zugriff verweigert auf den Pfad /wwroot/cache/ etc...'
Dies wird durch Docker ASP.NET Installationen verursacht, die keinen Schreibzugriff auf das Cache-Verzeichnis erlauben ImageSharp verwendet, um temporäre Dateien zu speichern.

## Lösung

Die Lösung besteht darin, ein Volumen in den Docker-Container einzubinden, das auf ein Verzeichnis auf dem Host-Rechner verweist. So kann die ImageSharp-Bibliothek ohne Probleme in das Cache-Verzeichnis schreiben.

Hier ist, wie es zu tun:

```dockerfile
mostlylucid:
image: scottgal/mostlylucid:latest
volumes:
- /mnt/imagecache:/app/wwwroot/cache
```

Hier sehe ich die Datei /app/wwwroot/cache in ein lokales Verzeichnis auf meinem Rechner. Auf diese Weise kann ImageSharp ohne Probleme in das Cache-Verzeichnis schreiben.

Auf meinem Ubuntu-Rechner habe ich ein Verzeichnis /mnt/imagecache erstellt und dann den Folowing-Befehl ausgeführt, um es schreibbar zu machen (ich weiß, dass dies nicht sicher ist, aber ich bin kein Linux-Guru :))

```shell
chmod  777 -p /mnt/imagecache
```

In meinem programm.cs habe ich diese Zeile:

```csharp
builder.Services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
```

Als Cacheroot-Standard auf wwwroot wird dies nun in das Verzeichnis /mnt/imagecache auf dem Rechner geschrieben.