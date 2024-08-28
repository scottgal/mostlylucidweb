# Самозбереження сіка для ведення журналу ASP.NET

<datetime class="hidden">2024-08- 28T09: 37</datetime>

<!--category-- ASP.NET, Seq, Serilog, Docker -->
# Вступ

Seq - це програма, яка надає вам змогу переглядати і аналізувати журнали. Чудовий інструмент для усування вад та моніторингу вашої програми. У цьому полі я повідомлю про те, як я налаштував Seq для ведення журналу моєї програми ASP.NET.
Ніколи не може мати забагато панелі приладів :)

![SeqDashboard](seqdashboard.png)

[TOC]

# Налаштування Seq

Сек входит в пару пельмутов. Ви можете або використовувати версію хмари, або власну назву вузла. Я вирішив самостійно збирати колоди, оскільки хотів тримати їх у секреті.

Спочатку я відвідав веб-сайт Seq і знайшов [Настанови зі встановлення панелі](https://docs.datalust.co/docs/getting-started-with-docker).

## Локально

Щоб запустити локально, вам спочатку слід отримати пароль хешування. Ви можете зробити це за допомогою наступної команди:

```bash
echo '<password>' | docker run --rm -i datalust/seq config hash
```

Щоб запустити його локально, ви можете скористатися такою командою:

```bash
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -e SEQ_FIRSTRUN_ADMINPASSWORDHASH=<hashfromabove> -v C:\seq:/data -p 5443:443 -p 45341:45341 -p 5341:5341 -p 82:80 datalust/seq

```

На моїй локальній машині Ubuntu я створив скрипт sh:

```shell
#!/bin/bash
PH=$(echo 'Abc1234!' | docker run --rm -i datalust/seq config hash)

mkdir -p /mnt/seq
chmod 777 /mnt/seq

docker run \
  --name seq \
  -d \
  --restart unless-stopped \
  -e ACCEPT_EULA=Y \
  -e SEQ_FIRSTRUN_ADMINPASSWORDHASH="$PH" \
  -v /mnt/seq:/data \
  -p 5443:443 \
  -p 45341:45341 \
  -p 5341:5341 \
  -p 82:80 \
  datalust/seq
```

Потім

```shell
chmod +x seq.sh
./seq.sh
```

Це підніме тебе і побіжиш, а потім підеш до `http://localhost:82` / `http://<machineip>:82` щоб побачити, як встановлено ваш seq (типовий пароль адміністратора - це пароль, який ви ввели для <password> зверху.

## В панелі

Я додав secq до мого файла створення Docker таким чином:

```docker
  seq:
    image: datalust/seq
    container_name: seq
    restart: unless-stopped
    environment:
      ACCEPT_EULA: "Y"
      SEQ_FIRSTRUN_ADMINPASSWORDHASH: ${SEQ_DEFAULT_HASH}
    volumes:
      - /mnt/seq:/data
    networks:
      - app_network
```

Зауважте, що у мене є каталог з назвою `/mnt/seq` (для вікон скористайтеся шляхом до вікон). Тут буде збережено журнали.

У мене також є `SEQ_DEFAULT_HASH` змінна середовища, яка є паролем хешування для адміністративного користувача у моєму файлі. env.

# Налаштування ядра ASP. NET

Як я користуюся [Серілог](https://serilog.net/) Для моєї лісозаготівлі досить легко встановити Сек. Він навіть має досьє про те, як це зробити. [тут](https://docs.datalust.co/docs/using-serilog).

По суті, ви просто додаєте раковину до вашого проекту:

```shell
dotnet add package Serilog.Sinks.Seq
```

Я надаю перевагу використанню `appsettings.json` для мого налаштування, так що у моєму випадку я просто налаштував "стандартне" `Program.cs`:

```csharp
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
    Serilog.Debugging.SelfLog.Enable(Console.Error);
    Console.WriteLine($"Serilog Minimum Level: {configuration.MinimumLevel.ToString()}");
});
```

Потім у моїй ♫appsettings.json я маю цю конфігурацію

```json
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File", "Serilog.Sinks.Seq"],
    "MinimumLevel": "Warning",
    "WriteTo": [
        {
          "Name": "Seq",
          "Args":
          {
            "serverUrl": "http://seq:5341",
            "apiKey": ""
          }
        },
      {
        "Name": "Console"
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/applog-.txt",
          "rollingInterval": "Day"
        }
      }

    ],
    "Enrich": ["FromLogContext", "WithMachineName"],
    "Properties": {
      "ApplicationName": "mostlylucid"
    }
  }

```

Ви побачите, що у мене є `serverUrl` з `http://seq:5341`. Це тому, що secq працює у контейнері з назвою `seq` і він на порту. `5341`. Якщо ви запустите його локально, ви можете скористатися `http://localhost:5341`.
Крім того, я використовую ключ API, щоб вказати рівень журналювання динамічно (ви можете встановити ключ, який приймає лише певний рівень повідомлень журналу).

Ви встановите його у вашому випадку seq `http://<machine>:82` і натисніть кнопку з конструкцією, розташовану у верхній правій частині вікна. Потім натисніть `API Keys` tab і додавання нового ключа. Тоді ви можете використати цей ключ у вашому комп'ютері. `appsettings.json` файл.

![Seq](seqapikey.png)

# Докер Композитний

Тепер ми маємо цю установку, нам потрібно налаштувати нашу програму ASP.NET, щоб взяти ключ. I use a `.env` Постачание, чтобы сохранить мои секреты.

```dotenv
SEQ_DEFAULT_HASH="<adminpasswordhash>"
SEQ_API_KEY="<apikey>"
```

Потім у моєму файлі набору Docker Я вказую, що значення має бути введене як змінна середовища до моєї програми ASP. NET:

```docker
services:
  mostlylucid:
    image: scottgal/mostlylucid:latest
    ports:
      - 8080:8080
    restart: always
    labels:
        - "com.centurylinklabs.watchtower.enable=true"
    env_file:
      - .env
    environment:
      - Auth__GoogleClientId=${AUTH_GOOGLECLIENTID}
      - Auth__GoogleClientSecret=${AUTH_GOOGLECLIENTSECRET}
      - Auth__AdminUserGoogleId=${AUTH_ADMINUSERGOOGLEID}
      - SmtpSettings__UserName=${SMTPSETTINGS_USERNAME}
      - SmtpSettings__Password=${SMTPSETTINGS_PASSWORD}
      - Analytics__UmamiPath=${ANALYTICS_UMAMIPATH}
      - Analytics__WebsiteId=${ANALYTICS_WEBSITEID}
      - ConnectionStrings__DefaultConnection=${POSTGRES_CONNECTIONSTRING}
      - TranslateService__ServiceIPs=${EASYNMT_IPS}
      - Serilog__WriteTo__0__Args__apiKey=${SEQ_API_KEY}
    volumes:
      - /mnt/imagecache:/app/wwwroot/cache
      - /mnt/markdown/comments:/app/Markdown/comments
      - /mnt/logs:/app/logs
    networks:
      - app_network
```

Зауважте, що `Serilog__WriteTo__0__Args__apiKey` встановлюється у значення `SEQ_API_KEY` з `.env` файл. " 0 " - це індекс `WriteTo` масив у `appsettings.json` файл.

# КаддіCity in New Brunswick Canada

Нотатка для обох Sek і моєї програми ASP.NET Я визначив, що обидва вони належать моєму `app_network` мережа. Це тому, що я використовую "Caddy" як "поворотний проксі" і він в тій же мережі. Це означає, що я можу використовувати службове ім'я як адресу URL в моєму Cedyfile.

```caddy
{
    email scott.galloway@gmail.com
}
seq.mostlylucid.net
{
   reverse_proxy seq:80
}

http://seq.mostlylucid.net
{
   redir https://{host}{uri}
}
```

Отже, вона здатна на карті. `seq.mostlylucid.net` до мого бюлетеня.

# Висновки

Сек - це чудовий інструмент для ведення журналу та спостереження за вашою програмою. Легко налагодити, використовувати і інтегрувати з серилогом. Я вважаю, що це неоціненно для усування моїх застосувань і я впевнений, що ви також знайдете його.