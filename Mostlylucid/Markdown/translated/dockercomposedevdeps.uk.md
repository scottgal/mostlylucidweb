# Використання " Докера " у зв'язку з залежностями розвитку

<!--category-- Docker -->
<datetime class="hidden">2024- 08- 09T17: 17</datetime>

# Вступ

Коли ми розробляли програмне забезпечення традиційно, ми накопичували базу даних, чергу повідомлень, кеш і, можливо, кілька інших послуг. Це може завдати болю, особливо, якщо ви працюєте над багатьма проектами. Докер Комбінація - це інструмент, який надає вам змогу визначати і запускати програми для роботи з декількома областями. Чудовий спосіб керувати залежностями розвитку.

На цьому дописі я покажу вам, як користуватися Докер Комосом, щоб керувати залежностями розвитку.

[TOC]

# Передумови

Спочатку вам слід встановити на будь-якій платформі комп'ютер docker. Ви можете звантажити його за допомогою [тут](https://www.docker.com/products/docker-desktop).

**ЗАУВАЖЕННЯ: для того, щоб встановити програму, вам слід запустити засіб встановлення стільниці Docker як адміністратор.**

# Створення файла компонування Docker

Для визначення служб, які ви бажаєте запустити Docker Compose використовує файл YML. Ось приклад простого `devdeps-docker-compose.yml` файл, який визначає службу бази даних:

```yaml
services: 
  smtp4dev:
    image: rnwood/smtp4dev
    ports:
      - "3002:80"
      - "2525:25"
    volumes:
      # This is where smtp4dev stores the database..
      - e:/smtp4dev-data:/smtp4dev
    restart: always
  postgres:
    image: postgres:16-alpine
    container_name: postgres
    ports:
      - "5432:5432"
    env_file:
      - .env
    volumes:
      - e:/data:/var/lib/postgresql/data  # Map e:\data to the PostgreSQL data folder
    restart: always	
networks:
  mynetwork:
        driver: bridge
```

Зауважте, що тут я вказала томи для збереження даних для кожної служби, тут я вказала

```yaml
    volumes:
      # This is where smtp4dev stores the database..
      - e:/smtp4dev-data:/smtp4dev
    volumes:
        - e:/data:/var/lib/postgresql/data  # Map e:\data to the PostgreSQL data folder
```

Це гарантує, що дані зберігаються між перевантаженнями контейнерів.

Також я визначаю `env_file` для `postgres` Служи. Цей файл містить змінні середовища, які передаються до контейнера.
Ви можете бачити список змінних середовища, які можна передати до контейнера PostgreSQL [тут](https://www.docker.com/blog/how-to-use-the-postgres-docker-official-image/#1-Environment-variables).
Ось приклад `.env` файл:

```shell
POSTGRES_DB=postgres
POSTGRES_USER=postgres
POSTGRES_PASSWORD=<somepassword>
```

За допомогою цього пункту можна налаштувати типову базу даних, пароль і користувача для PostgreSQL.

Тут я також запустив службу SMTP4Dev, це чудовий інструмент для тестування функціональних можливостей електронної пошти у вашій програмі. Докладніші відомості щодо цього можна знайти у розділі. [тут](https://github.com/rnwood/smtp4dev/wiki/Installation#how-to-run-smtp4dev-in-docker).

Якщо ти заглянеш в мою `appsettings.Developmet.json` файл, який ви побачите, має такі налаштування для сервера SMTP:

```json
  "SmtpSettings":
{
"Server": "smtp.gmail.com",
"Port": 587,
"SenderName": "Mostlylucid",
"Username": "",
"SenderEmail": "scott.galloway@gmail.com",
"Password": "",
"EnableSSL": "true",
"EmailSendTry": 3,
"EmailSendFailed": "true",
"ToMail": "scott.galloway@gmail.com",
"EmailSubject": "Mostlylucid"

}
```

Це працює для SMTP4Dev і надає мені змогу перевірити цю функціональність (можна надіслати на будь- яку адресу і побачити повідомлення електронної пошти у інтерфейсі SMTP4Dev за адресою http: // localhost: 3002 /).

Якщо ви впевнені, що все працює, ви можете перевірити на справжньому сервері SMTP, на зразок GMAIL (наприклад, див. [тут](addingasyncsendingforemails) для того, щоб зробити це)

# Виконання служб

Щоб запустити служби, визначені у `devdeps-docker-compose.yml` файл, вам слід виконати таку команду у тому самому каталозі, що і у файлі:

```shell
docker compose -f .\devdeps-docker-compose.yml up -d
```

Зауважте, що спочатку вам слід запустити програму так, як це робить програма. За допомогою цього пункту ви зможете бачити елементи налаштування, які передаються з `.env` файл.

```shell
docker compose -f .\devdeps-docker-compose.yml config
```

Тепер, якщо ви поглянете на стільницю Docker, ви побачите, як виконуються ці служби

![Стільниця Docker](dockerdesktopdev.png)