# Використання умамі у місцевих аналітичних закладах

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024- 08T15: 53</datetime>

## Вступ

Одна з речей, яка дратувала мене через мою поточну конфігурацію, полягала в тому, що мені довелося використовувати Google Analtics, щоб отримати дані для відвідувачів (що з цього мало?). Тому я хотів знайти те, що не передавав дані Google або будь-якій іншій третій стороні. Я знайшов [Умаміzambia_ districts. kgm](https://umami.is/) що є простим самопідтриманим веб аналітичним рішенням. Це чудова альтернатива для аналітики Google, її легко налаштувати.

[TOC]

## Встановлення

Встановлення є простим, але досить простим, щоб дійсно отримати результат...

### Докер Композитний

Так як я хотів додати Умамі до моїх поточних конфігурацій докер-комбінувати, мені потрібно було додати нову службу до мого `docker-compose.yml` файл. Я додав наступне до нижньої частини файла:

```yaml
  umami:
    image: ghcr.io/umami-software/umami:postgresql-latest
    env_file: .env
    environment:
      DATABASE_URL: ${DATABASE_URL}
      DATABASE_TYPE: ${DATABASE_TYPE}
      HASH_SALT: ${HASH_SALT}
      APP_SECRET: ${APP_SECRET}
      TRACKER_SCRIPT_NAME: getinfo
      API_COLLECT_ENDPOINT: all
    ports:
      - "3000:3000"
    depends_on:
      - db
    networks:
      - app_network
    restart: always
  db:
    image: postgres:16-alpine
    env_file:
      - .env
    networks:
      - app_network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER}"]
      interval: 5s
      timeout: 5s
      retries: 5
    volumes:
      - /mnt/umami/postgres:/var/lib/postgresql/data
    restart: always
  cloudflaredumami:
    image: cloudflare/cloudflared:latest
    command: tunnel --no-autoupdate run --token ${CLOUDFLARED_UMAMI_TOKEN}
    env_file:
      - .env
    restart: always
    networks:
      - app_network


```

Цей файл docker- compose.yml містить такі налаштування:

1. Нова служба називається `umami` який використовує `ghcr.io/umami-software/umami:postgresql-latest` зображення. Ця служба використовується для управління аналітичною службою Умамі.
2. Нова служба називається `db` який використовує `postgres:16-alpine` зображення. Ця служба використовується для запуску бази даних Postgres, якими Амамі зберігає свої дані.
   Зауважте, що для цієї служби я прив' язую її до каталогу на моєму сервері так, щоб дані залишалися між перезапусками.

```yaml
    volumes:
      - /mnt/umami/postgres:/var/lib/postgresql/data
```

Для того, щоб користувач докера на вашому сервері міг існувати і мати запис цього режисера (знову ж не експерта з Linux, отже, 777, ймовірно, забагато!).

```shell
chmod 777 /mnt/umami/postgres
```

3. Нова служба називається `cloudflaredumami` який використовує `cloudflare/cloudflared:latest` зображення. Ця служба використовується для тунелю служби "Уамамі" через Hamflare, щоб надати доступ до неї з інтернету.

### Файл Env

Для підтримки цього я також оновив мій `.env` файл для включення таких файлів:

```shell
CLOUDFLARED_UMAMI_TOKEN=<cloudflaretoken>
DATABASE_TYPE=postgresql
HASH_SALT=<salt>

POSTGRES_DB=postgres
POSTGRES_USER=<postgresuser>
POSTGRES_PASSWORD=<postgrespassword>
UMAMI_SECRET=<umamisecret>

APP_SECRET=${UMAMI_SECRET}
UMAMI_USER=${POSTGRES_USER}
UMAMI_PASS=${POSTGRES_PASSWORD}
DATABASE_URL=postgresql://${UMAMI_USER}:${UMAMI_PASS}@db:5432/${POSTGRES_DB}
```

За допомогою цього пункту можна налаштувати набір об' єктів (це `<>` Очевидно, есемітам потрібно замінити вашими власними значеннями). The `cloudflaredumami` Служба використовується для тунелю служби Умамі через Hamaflare, щоб надати доступ до неї з інтернету. МОЖЛИВО використовувати BASE_PATH, але для Умамі потрібно перебудувати, щоб змінити базовий шлях, тому я залишив його як кореневий шлях на даний момент.

### Тунель " Хмарфлер" object name (optional)

Щоб налаштувати тунель wamflare для цього (який працює як шлях до файла js, використаного для аналітики - getinfo.js), я використав веб- сайт:

![Тунель " Хмарфлер" object name (optional)](umamisetup.png)

Це встановлює тунель на службу "Уамі" і дозволяє доступ до нього з інтернету. Зауважте, я вказую це на `umami` Служба у файлі docker- compose (так само, як це робиться у мережі, що і тунель з хмарами, це коректна назва).

### Налаштування umami на сторінці

Щоб увімкнути шлях до скрипту (закликаний) `getinfo` у моєму налаштуванні вище) Я додав запис налаштування до моїх параметрів

```json
 "Analytics":{
   "UmamiPath" : "https://umamilocal.mostlylucid.net/getinfo",
   "WebsiteId" : "32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
 },
```

Крім того, ви можете додати ці змінні до вашого файла. env і передати їх як змінні середовища до файла docker- common.

```shell
ANALYTICS__UMAMIPATH="https://umamilocal.mostlylucid.net/getinfo"
ANALYTICS_WEBSITEID="32c2aa31-b1ac-44c0-b8f3-ff1f50403bee"
```

```yaml
  mostlylucid:
    image: scottgal/mostlylucid:latest
    ports:
      - 8080:8080
    restart: always
    environment:
    ...
      - Analytics__UmamiPath=${ANALYTICS_UMAMIPATH}
      - Analytics__WebsiteId=${ANALYTICS_WEBSITEID}
```

Ви встановили веб-Ід на панелі приладів "Уамамі," коли встановили сайт. (Зауважте, що типовим ім' я користувача і пароль служби " Умамі " є `admin` і `umami`, Вам потрібно змінити їх після налаштування).
![Umami Dockboard](umamiaddwebsite.png)

З відповідним файлом параметрів cs:

```csharp
public class AnalyticsSettings : IConfigSection
{
    public static string Section => "Analytics";
    public string? UmamiPath { get; set; }
}
```

Знову це використовує мої налаштування POCO ([тут](/blog/addingidentityfreegoogleauth#configuring-google-auth-with-poco)) налаштування параметрів.
Налаштуйте це у моїй програмі.cs:

```csharp
builder.Configure<AnalyticsSettings>();
```

І нарешті в моєму `BaseController.cs` `OnGet` метод, який я додав, щоб вказати шлях до скрипту аналітики:

```csharp
   public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        if (!Request.IsHtmx())
        {
            ViewBag.UmamiPath = _analyticsSettings.UmamiPath;
            ViewBag.UmamiWebsiteId = _analyticsSettings.WebsiteId;
        }
        base.OnActionExecuting(filterContext);
    }
    
```

За допомогою цього пункту можна вказати шлях до скрипту аналітики, який буде використано у файлі компонування.

### Файл компонування

Нарешті, я додав до свого файла компонування такі рядки, щоб включити скрипт аналітики:

```html
<script defer src="@ViewBag.UmamiPath" data-website-id="@ViewBag.UmamiWebsiteId"></script>
```

Ця команда включає скрипт на сторінці і встановлює ідентифікатор веб- сайта служби аналітики.

## Виключення себе від аналітичних аналітичних засобів

Для того, щоб виключити ваші власні відвідини з аналітичних даних, ви можете додати до вашого переглядача такі локальні дані:

У інструментах rhrome dev (Ctrl+Shift+I на вікнах) ви можете додати такі рядки до консолі:

```javascript
localStorage.setItem("umami.disabled", 1)
```

## Висновки

Це було трохи не те, що я мав зробити, але я задоволений результатом. Тепер у мене є аналітична служба, яка не передає дані Google або будь-якій іншій третій стороні. Настроить немного боли, но когда всё закончено, его легко использовать. Я задоволений результатом і рекомендую його кожному, хто шукає самовладного аналітичного рішення.