# ImageSharp met Docker

<datetime class="hidden">2024-08-01T01:00</datetime>

<!--category-- Docker, ImageSharp -->
ImageSharp is een geweldige bibliotheek voor het werken met afbeeldingen in.NET. Het is snel, makkelijk te gebruiken, en heeft veel functies. In dit bericht, Ik zal u laten zien hoe ImageSharp met Docker te gebruiken om een eenvoudige beeldverwerking dienst te maken.

## Wat is ImageSharp?

ImageSharp stelt me in staat om naadloos te werken met afbeeldingen in.NET. Het is een cross-platform bibliotheek die een breed scala aan afbeeldingsformaten ondersteunt en biedt een eenvoudige API voor beeldverwerking. Het is snel, efficiënt en eenvoudig te gebruiken.

Er is echter een probleem in mijn setup met behulp van docker en ImageSharp. Wanneer ik probeer om een afbeelding uit een bestand te laden, krijg ik de volgende fout:
'Toegang geweigerd op pad /wroot/cache/ etc...'
Dit wordt veroorzaakt door Docker ASP.NET installaties die geen schrijftoegang toestaan tot de cache directory ImageSharp gebruikt om tijdelijke bestanden op te slaan.

## Oplossing

De oplossing is om een volume aan te koppelen in de docker container die naar een directory op de host machine wijst. Op deze manier kan de ImageSharp-bibliotheek zonder problemen naar de cache directory schrijven.

Hier is hoe het te doen:

```dockerfile
mostlylucid:
image: scottgal/mostlylucid:latest
volumes:
- /mnt/imagecache:/app/wwwroot/cache
```

Hier zie je dat ik het bestand /app/wwwroot/cache naar een lokale map op mijn host machine map. Op deze manier kan ImageSharp zonder problemen naar de cache directory schrijven.

Op mijn Ubuntu machine heb ik een directory /mnt/imagecache gemaakt en vervolgens het volgende commando uitgevoerd om het schrijfbaar te maken (door wie dan ook, ik weet dat dit niet veilig is maar ik ben geen Linux goeroe:))

```shell
chmod  777 -p /mnt/imagecache
```

In mijn programma.cs heb ik deze regel:

```csharp
builder.Services.AddImageSharp().Configure<PhysicalFileSystemCacheOptions>(options => options.CacheFolder = "cache");
```

Als de cacheroot standaard op wwwroot staat zal dit nu schrijven naar de directory /mnt/imagecache op de host machine.