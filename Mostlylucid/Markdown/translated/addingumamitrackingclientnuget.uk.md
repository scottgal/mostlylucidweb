# Додавання пакунка для стеження за клієнтом Nuget

<!--category-- ASP.NET, Umami, Nuget -->
<datetime class="hidden">2024- 08- 28T02: 00</datetime>

# Вступ

Тепер у мене є клієнт "Умамі," мені потрібно пакувати його і зробити його доступним у пакунку Nuget. Це досить простий процес, але є декілька речей, про які слід пам'ятати.

[TOC]

# Створення пакунка Nuget

## Версія

Я вирішив скопіювати [Халідafghanistan. kgm](https://khalidabuhakmeh.com/) і використай чудовий пакунок minver для версії мого пакунка Nueget. Це простий пакунок, який використовує теґ версії git для визначення номеру версії.

Щоб використовувати його я просто додав наступне до мого `Umami.Net.csproj` файл:

```xml
    <PropertyGroup>
    <MinVerTagPrefix>v</MinVerTagPrefix>
</PropertyGroup>
```

Таким чином я можу підписати мою версію за допомогою `v` і пакунок буде правильно перевидано.

```bash
 git tag v0.0.8       
 git push origin v0.0.8

```

Натисни цю мітку, а потім я налаштую дію GitHub, щоб зачекати на цю мітку і зібрати пакунок Nuget.

## Побудова пакунка Nuget

У мене є дія GitHub, яка будує пакет Nuget і штовхає його до сховища пакунків GitHub. Це простий процес, який використовує `dotnet pack` команда для збирання пакунка, а потім команди `dotnet nuget push` команду для пересування його до сховища nuget.

```yaml
name: Publish Umami.NET
on:
  push:
    tags:
      - 'v*.*.*'  # This triggers the action for any tag that matches the pattern v1.0.0, v2.1.3, etc.

jobs:
  publish:
    runs-on: ubuntu-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.x' # Specify the .NET version you need

    - name: Restore dependencies
      run: dotnet restore ./Umami.Net/Umami.Net.csproj

    - name: Build project
      run: dotnet build --configuration Release ./Umami.Net/Umami.Net.csproj --no-restore

    - name: Pack project
      run: dotnet pack --configuration Release ./Umami.Net/Umami.Net.csproj --no-build --output ./nupkg

    - name: Publish to NuGet
      run: dotnet nuget push ./nupkg/*.nupkg --source https://api.nuget.org/v3/index.json --api-key ${{ secrets.UMAMI_NUGET_API_KEY }}
      env:
        NUGET_API_KEY: ${{ secrets.UMAMI_NUGET_API_KEY }}
```

### Додавання " Читати " і " Піктограма "

Це досить просто, я додаю `README.md` файл до кореневої частини проекту і `icon.png` файл до кореневої частини проекту. The `README.md` файл використовується як опис пакунка і `icon.png` файл використовується як піктограма пакунка.

```xml
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <LangVersion>12</LangVersion>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>

        <IsPackable>true</IsPackable>
        <PackageId>Umami.Net</PackageId>
        <Authors>Scott Galloway</Authors>
        <PackageIcon>icon.png</PackageIcon>
        <RepositoryUrl>https://github.com/scottgal/mostlylucidweb/tree/main/Umami.Net</RepositoryUrl>
        <PackageLicenseExpression>MIT</PackageLicenseExpression>
        <PackageTags>web</PackageTags>
        <PackageReadmeFile>README.md</PackageReadmeFile>
        <Description>
           Adds a simple Umami endpoint to your ASP.NET Core application.
        </Description>
    </PropertyGroup>
```

У моєму файлі README.md є посилання на сховище GitHub і опис пакунка GitHub.

Репродукція нижче:

# Umami.Net

Це клієнт.NET core для API стеження за API Umami.
Він заснований на клієнті вузла Umami, який можна знайти [тут](https://github.com/umami-software/node).

Ви можете бачити, як зробити Амамі контейнером для докерів [тут](https://www.mostlylucid.net/blog/usingumamiforlocalanalytics).
Ви можете прочитати більше подробиць про це створення у моєму блозі [тут](https://www.mostlylucid.net/blog/addingumamitrackingclientfollowup).

Щоб скористатися цим клієнтом, вам слід встановити такі налаштування програми. json:

```json
{
  "Analytics":{
    "UmamiPath" : "https://umamilocal.mostlylucid.net",
    "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
  },
}
```

Де `UmamiPath` це шлях до вашого примірника Умамі і `WebsiteId` є ідентифікатором веб- сайта, який ви бажаєте відстежити.

Щоб скористатися клієнтом, вам слід додати такі дані до вашої програми `Program.cs`:

```csharp
using Umami.Net;

services.SetupUmamiClient(builder.Configuration);
```

Это добавит клиента Умами в коллекцию служб.

Після цього ви можете скористатися клієнтом у два способи:

1. Підписати `UmamiClient` у вашому класі і викликайте його `Track` метод:

```csharp
 // Inject UmamiClient umamiClient
 await umamiClient.Track("Search", new UmamiEventData(){{"query", encodedQuery}});
```

2. Використовувати `UmamiBackgroundSender` для стеження за подіями у тлі (це використовує a `IHostedService` для надсилання подій з тла:

```csharp
 // Inject UmamiBackgroundSender umamiBackgroundSender
await umamiBackgroundSender.Track("Search", new UmamiEventData(){{"query", encodedQuery}});
```

Клієнт надішле цю подію до API Umami і зберігатиме її.

The `UmamiEventData` є словником пар ключових цінностей, які будуть відправлені до API Амамі як дані про події.

Крім того, існують більш низькі методи, які можуть бути використані для надсилання подій в API Umami.

На обох `UmamiClient` і `UmamiBackgroundSender` ви можете назвати такий спосіб.

```csharp


 Send(UmamiPayload? payload = null, UmamiEventData? eventData = null,
        string eventType = "event")
```

Якщо ти не пройдеш `UmamiPayload` об' єкт, клієнт створить його для вас за допомогою `WebsiteId` від apponts.json.

```csharp
    public  UmamiPayload GetPayload(string? url = null, UmamiEventData? data = null)
    {
        var httpContext = httpContextAccessor.HttpContext;
        var request = httpContext?.Request;

        var payload = new UmamiPayload
        {
            Website = settings.WebsiteId,
            Data = data,
            Url = url ?? httpContext?.Request?.Path.Value,
            IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
            UserAgent = request?.Headers["User-Agent"].FirstOrDefault(),
            Referrer = request?.Headers["Referer"].FirstOrDefault(),
           Hostname = request?.Host.Host,
        };
        
        return payload;
    }

```

Ви можете бачити, що це населяє `UmamiPayload` об' єкт з об' єктом `WebsiteId` від appsetds.json, `Url`, `IpAddress`, `UserAgent`, `Referrer` і `Hostname` з `HttpContext`.

ЗАУВАЖЕННЯ: Тип подій може бути лише " оптимізованим" або "ідентифікувати" так само, як і у API Umami.

# Включення

Таким чином, ви можете встановити Umami.Net з Nuget і скористатися ним у своїй програмі ASP.NET. Сподіваюся, вам це знадобиться. Я продовжу налаштовувати та додавати тести в майбутніх постах.