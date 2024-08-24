# Повний пошук тексту (Pt 3 - OpenSearch з ядром ASP. NET)

<!--category-- OpenSearch, ASP.NET -->
<datetime class="hidden">2024- 08- 24T06: 40</datetime>

## Вступ

У попередніх частинах цієї серії ми ввели концепцію повноформатного пошуку тексту і того, як ним можна скористатися для пошуку тексту у базі даних. У цій частині ми познайомимося з використанням OpenSearch з ядром ASPNET.

Попередні частини:

- [Повне пошук тексту за допомогою Postgres](/blog/textsearchingpt1)
- [Панель пошуку з Postgres](/blog/textsearchingpt11)
- [Вступ до OpenSearch](/blog/textsearchingpt3)

У цій частині ми поговоримо про те, як почати використовувати новий блискучий екземпляр OpenSearch з ядром ASP.NET.

[TOC]

## Налаштування

Після того, як буде запущено екземпляр OpenSearch up і ми зможемо почати взаємодіяти з ним. Ми використаємо [Клієнт OpenSearch](https://opensearch.org/docs/latest/clients/OSC-dot-net/) НЕТ.
Спочатку ми налаштували клієнт у нашому розширенні Setup

```csharp
    var openSearchConfig = services.ConfigurePOCO<OpenSearchConfig>(configuration.GetSection(OpenSearchConfig.Section));
        var config = new ConnectionSettings(new Uri(openSearchConfig.Endpoint))
            .EnableHttpCompression()
            .EnableDebugMode()
            .ServerCertificateValidationCallback((sender, certificate, chain, errors) => true)
            .BasicAuthentication(openSearchConfig.Username, openSearchConfig.Password);
        services.AddSingleton<OpenSearchClient>(c => new OpenSearchClient(config));
```

Це встановлює клієнту кінцеву точку і посвідчення. Ми також вмикаємо режим зневаджування, щоб побачити, що відбувається. Крім того, оскільки ми не використовуємо сертифікати True SSL, ми вимикаємо перевірку сертифікатів (не робити цього у виробництві).

## Індексування даних

Основною концепцією OpenSearch є Індекс. Подумайте про індекс, наприклад, таблицю бази даних, це місце, де зберігаються всі ваші дані.

Для цього ми використаємо [Клієнт OpenSearch](https://opensearch.org/docs/latest/clients/OSC-dot-net/) НЕТ. Ви можете встановити це за допомогою NuGet:

Ви можете помітити двох таких: Opensearch.Net і Opensearch.Client. Перша - це такі речі низького рівня, як управління з'єднанням, друга - це такі речі високого рівня, як індексування та пошук.

Тепер, коли ми його встановили, ми можемо почати вивчати індексування даних.

Створення індексу є напівпрозорим. Ви просто визначаєте, як має виглядати ваш індекс, а потім створюєте його.
У коді нижче ви можете побачити нашу модель індексу " map " (спрощену версію моделі бази даних блогу).
Для кожного з полів цієї моделі ми визначаємо тип цього типу (текст, дата, ключове слово тощо) і який аналізатор слід використовувати.

Тип важливий, оскільки визначає спосіб зберігання даних і спосіб його пошуку. Наприклад, поле " text " аналізується і позначається, поле " keyword " - ні. Таким чином, ви очікуєте знайти поле ключових слів саме так, як зберігається, але текстове поле ви можете знайти частини тексту.

Крім того, у цьому розділі категорії є рядком[], але ключовий тип розуміє, як правильно ними користуватися.

```csharp
   public async Task CreateIndex(string language)
    {
        var languageName = language.ConvertCodeToLanguageName();
        var indexName = GetBlogIndexName(language);

      var response =  await client.Indices.CreateAsync(indexName, c => c
            .Settings(s => s
                .NumberOfShards(1)
                .NumberOfReplicas(1)
            )
            .Map<BlogIndexModel>(m => m
                .Properties(p => p
                    .Text(t => t
                        .Name(n => n.Title)
                        .Analyzer(languageName)
                    )
                    .Text(t => t
                        .Name(n => n.Content)
                        .Analyzer(languageName)
                    )
                    .Text(t => t
                        .Name(n => n.Language)
                    )
                    .Date(t => t
                        .Name(n => n.LastUpdated)
                    )
                    .Date(t => t
                        .Name(n => n.Published)
                    )
                    .Date(t => t
                        .Name(n => n.LastUpdated)
                    )
                    .Keyword(t => t
                        .Name(n => n.Id)
                    )
                    .Keyword(t=>t
                        .Name(n=>n.Slug)
                    )
                    .Keyword(t=>t
                        .Name(n=>n.Hash)
                    )
                    .Keyword(t => t
                        .Name(n => n.Categories)
                    )
                )
            )
        );
        
        if (!response.IsValid)
        {
           logger.LogError("Failed to create index {IndexName}: {Error}", indexName, response.DebugInformation);
        }
    }
```

## Додавання елементів до індексу

Коли ми маємо індекс, щоб додати до нього елементи, нам потрібно додати елементи до цього індексу. Тут, коли ми додаємо БУНК, ми використовуємо метод вставки громіздкості.

Ви можете побачити, що спочатку ми використовуємо метод, що називається`GetExistingPosts` те, що поверне всі дописи, які вже є у покажчику. Потім ми згрупуємо дописи за мовою і відфільтруємо мову "uk" (хоча не хочемо індексувати, що, оскільки потрібен додатковий додаток, у нас ще немає). Потім ми відфільтровуємо всі дописи, які вже є в індексі.
Ми використовуємо хеш і ІД, щоб визначити, чи вже є допис в індексі.

```csharp
    public async Task AddPostsToIndex(IEnumerable<BlogIndexModel> posts)
    {
        var existingPosts = await GetExistingPosts();
        var langPosts = posts.GroupBy(p => p.Language);
        langPosts=langPosts.Where(p => p.Key!="uk");
        langPosts = langPosts.Where(p =>
            p.Any(post => !existingPosts.Any(existing => existing.Id == post.Id && existing.Hash == post.Hash)));
        
        foreach (var blogIndexModels in langPosts)
        {
            
            var language = blogIndexModels.Key;
            var indexName = GetBlogIndexName(language);
            if(!await IndexExists(language))
            {
                await CreateIndex(language);
            }
            
            var bulkRequest = new BulkRequest(indexName)
            {
                Operations = new BulkOperationsCollection<IBulkOperation>(blogIndexModels.ToList()
                    .Select(p => new BulkIndexOperation<BlogIndexModel>(p))
                    .ToList()),
                Refresh = Refresh.True,
                ErrorTrace = true,
                RequestConfiguration = new RequestConfiguration
                {
                    MaxRetries = 3
                }
            };

            var bulkResponse = await client.BulkAsync(bulkRequest);
            if (!bulkResponse.IsValid)
            {
                logger.LogError("Failed to add posts to index {IndexName}: {Error}", indexName, bulkResponse.DebugInformation);
            }
            
        }
    }
```

Як тільки ми відфільтрували існуючі дописи і відсутній аналізатор, ми створюємо новий індекс (на основі назви, у моєму випадку "більш широковідомий-блог-<language>") і тоді витворює велику просьбу. Цей великий запит є збіркою дій, які слід виконати з індексом.
Це ефективніше, ніж додавання кожного елемента по одному.

Ви побачите це в `BulkRequest` ми встановимо `Refresh` властивість до `true`. Це означає, що після завершення вставки пучки буде оновлено індекс. Це не зовсім необхідно, але це корисно для зневаджування.

## Пошук індексу

Чудовий спосіб перевірити те, що насправді було створено, це перейти до інструментів Dev на OpenSearch Dashboards і виконати пошуковий запит.

```json
GET /mostlylucid-blog-*
{}
```

Цей запит поверне нам всі індекси, що відповідають шаблону `mostlylucid-blog-*`. (Так що всі наші індекси поки що).

```json
{
  "mostlylucid-blog-ar": {
    "aliases": {},
    "mappings": {
      "properties": {
        "categories": {
          "type": "keyword"
        },
        "content": {
          "type": "text",
          "analyzer": "arabic"
        },
        "hash": {
          "type": "keyword"
        },
        "id": {
          "type": "keyword"
        },
        "language": {
          "type": "text"
        },
        "lastUpdated": {
          "type": "date"
        },
        "published": {
          "type": "date"
        },
        "slug": {
          "type": "keyword"
        },
        "title": {
          "type": "text",
          "analyzer": "arabic"
        }
      }
    },
    "settings": {
      "index": {
        "replication": {
          "type": "DOCUMENT"
..MANY MORE
```

Інструменти Dev у OpenSearch Dashboards - це чудовий спосіб перевірити ваші запити, перш ніж вводити їх у ваш код.

![Інструменти Dev](devtools.png?width=900&quality=25)

## Пошук індексу

Тепер ми можемо почати шукати індекс. Ми можемо використати `Search` метод роботи з клієнтом.
Ось тут з'являється справжня сила OpenSearch. Вона має, буквально, [Десятки різних типів запитів](https://opensearch.org/docs/latest/query-dsl/) ви можете використовувати для пошуку ваших даних. Все: від простого пошуку ключових слів до комплексного " експоненціального " пошуку.

```csharp
    public async Task<List<BlogIndexModel>> GetSearchResults(string language, string query, int page = 1, int pageSize = 10)
    {
        var indexName = GetBlogIndexName(language);
        var searchResponse = await client.SearchAsync<BlogIndexModel>(s => s
                .Index(indexName)  // Match index pattern
                .Query(q => q
                    .Bool(b => b
                        .Must(m => m
                            .MultiMatch(mm => mm
                                .Query(query)
                                .Fields(f => f
                                    .Field(p => p.Title, boost: 2.0) 
                                    .Field(p => p.Categories, boost: 1.5) 
                                    .Field(p => p.Content)
                                )
                                .Type(TextQueryType.BestFields)
                                .Fuzziness(Fuzziness.Auto)
                            )
                        )
                    )
                )
                .Skip((page -1) * pageSize)  // Skip the first n results (adjust as needed)
                .Size(pageSize)  // Limit the number of results (adjust as needed)
        );

        if(!searchResponse.IsValid)
        {
            logger.LogError("Failed to search index {IndexName}: {Error}", indexName, searchResponse.DebugInformation);
            return new List<BlogIndexModel>();
        }
        return searchResponse.Documents.ToList();
    }

```

### Опис запиту

Цей метод `GetSearchResults`, розроблено для опитування певного покажчика OpenSearch для отримання дописів блогу. Вона потребує три параметри: `language`, `query`Параметри і параметри пагінії `page` і `pageSize`. Ось що він робить:

1. **Вибір індексу**:
   
   - Програма отримує назву індексу за допомогою `GetBlogIndexName` Даний метод ґрунтується на поданій мові. Індекс буде динамічно обрано відповідно до мови.

2. **Пошук запиту**:
   
   - Запит використовує a `Bool` запит за допомогою `Must` Термін, щоб переконатися, що результат відповідає певним критеріям.
   - Всередині `Must` пункт, а `MultiMatch` запит використовується для пошуку у декількох полях (`Title`, `Categories`, і `Content`).
     - **Boosing**: The `Title` поле надається стимуляції `2.0`, роблячи це важливішим у пошуку, і `Categories` має посилення `1.5`. Це означає, що документи, у яких буде показано запит щодо пошуку у заголовку або категоріях, будуть вищими.
     - **Тип запиту**: Використано `BestFields`, який намагається знайти найкраще відповідне поле для запиту.
     - **Розмитість**: The `Fuzziness.Auto` Параметр надає вам змогу приблизно відповідати (наприклад, обробляти малі друкарські копії).

3. **Pagination**:
   
   - The `Skip` метод пропускає перший `n` результати, залежно від номера сторінки, буде обчислено як `(page - 1) * pageSize`. Це допомагає робити екскурсію по випущених результатах.
   - The `Size` метод обмежує кількість повернених документів до вказаного `pageSize`.

4. **Обробка помилок**:
   
   - Якщо спроба запиту завершилася невдало, буде записано повідомлення про помилку і повернуто порожній список.

5. **Результат**:
   
   - Метод повертає список `BlogIndexModel` документи, що відповідають критеріям пошуку.

Отже, ви можете бачити, що ми можемо бути дуже гнучкими щодо пошуку наших даних. Ми можемо шукати окремі поля, збільшувати певні поля, навіть шукати у багатьох індексах.

Одна перевага BIG - це легкий qith, який ми можемо підтримувати у багатьох мовах. У нас є різні індекси для кожної мови і можливість пошуку в межах цього індексу. Це означає, що ми можемо використати правильний аналізатор для кожної мови і отримати найкращі результати.

## Новий API пошуку

На відміну від API пошуку, який ми бачили у попередніх частинах цієї серії, ми можемо значно спростити процес пошуку за допомогою OpenSearch. Ми можемо просто написати текст до цього запиту і отримати чудові результати.

```csharp
   [HttpGet]
    [Route("osearch/{query}")]
   [ValidateAntiForgeryToken]
    public async Task<JsonHttpResult<List<SearchResults>>> OpenSearch(string query, string language = MarkdownBaseService.EnglishLanguage)
    {
        var results = await indexService.GetSearchResults(language, query);
        
        var host = Request.Host.Value;
        var output = results.Select(x => new SearchResults(x.Title.Trim(), x.Slug, @Url.ActionLink("Show", "Blog", new{ x.Slug}, protocol:"https", host:host) )).ToList();
        return TypedResults.Json(output);
    }
```

Як бачите, ми маємо всі необхідні нам дані в індексі, щоб повернути результати. Потім ми можемо використати це, щоб створити URL для допису блогу. За допомогою цього пункту можна зняти навантаження на нашу базу даних і значно пришвидшити процес пошуку.

## Включення

У цьому полі ми побачили, як написати клієнт C# для взаємодії з нашим екземпляром OpenSearch.