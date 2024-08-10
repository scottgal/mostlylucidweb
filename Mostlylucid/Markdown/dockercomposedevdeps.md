# Using Docker Compose for Development Dependencies

# Introduction
When developing software traditionally we'd spin up a database, a message queue, a cache, and maybe a few other services. This can be a pain to manage, especially if you're working on multiple projects. Docker Compose is a tool that allows you to define and run multi-container Docker applications. It's a great way to manage your development dependencies.

In this post, I'll show you how to use Docker Compose to manage your development dependencies.

[TOC]

# Prerequisites
First you'll need to install docker desktop on whatever platform you're using. You can download it from [here](https://www.docker.com/products/docker-desktop). 

**NOTE: I've found that on Windows you really need to run Docker Desktop installer as admin to ensure it installs correctly.**

# Creating a Docker Compose file
Docker Compose uses a YAML file to define the services you want to run. Here's an example of a simple `devdeps-docker-compose.yml.yml` file that defines a database service:

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

Note here I've specified volumes for persisting the data for each service, here I've specified 
```yaml
    volumes:
      # This is where smtp4dev stores the database..
      - e:/smtp4dev-data:/smtp4dev
    volumes:
        - e:/data:/var/lib/postgresql/data  # Map e:\data to the PostgreSQL data folder
```
This ensures the data is persisted between runs of the containers.

I also specify an `env_file` for the `postgres` service. This is a file that contains environment variables that are passed to the container.
You can see a list of environment variables that can be passed to the PostgreSQL container [here](https://www.docker.com/blog/how-to-use-the-postgres-docker-official-image/#1-Environment-variables).
Here's an example of a `.env` file:

```shell
POSTGRES_DB=postgres
POSTGRES_USER=postgres
POSTGRES_PASSWORD=<somepassword>
```
This configures a default database, password and user for PostgreSQL. 

Here I also run the SMTP4Dev service, this is a great tool for testing email functionality in your application. You can find more information about it [here](https://github.com/rnwood/smtp4dev/wiki/Installation#how-to-run-smtp4dev-in-docker). 

If you look in my `appsettings.Developmet.json` file you'll see I have the following configuration for the SMTP server:

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
This works for SMTP4Dev and it enables me to test this functionality (I can send to any address, and see the email in the SMTP4Dev interface at http://localhost:3002/).

Once you're sure it's all working you can test on a real SMTP server like GMAIL (e.g., see [here](addingasyncsendingforemails) for how to do that)

# Running the services
To run the services defined in the `devdeps-docker-compose.yml` file, you need to run the following command in the same directory as the file:

```shell
docker compose -f .\devdeps-docker-compose.yml up -d
```

Note you should run it initially like this; this ensures you can see the config elements passed in from the `.env` file.

```shell
docker compose -f .\devdeps-docker-compose.yml config
```

Now if you look in Docker Desktop you can see these services running

![Docker Desktop](dockerdesktopdev.png)