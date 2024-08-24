# Повний пошук тексту (Pt 1)

<!--category-- Postgres, Entity Framework -->
<datetime class="hidden">2024- 08- 20T12: 40</datetime>

# Вступ

Пошук змісту є важливою частиною будь-якого важкого веб-сайту. Це підвищує здатність до виявлення та досвід користувача. У цьому полі я оприлюдню те, як я додала повний текст для пошуку цього сайту

Наступні частини цієї серії:

- [Панель пошуку з Postgres](/blog/textsearchingpt11)
- [Вступ до OpenSearch](/blog/textsearchingpt2)
- [Opensearch з C#](/blog/textsearchingpt3)

[TOC]

# Наближається

Існує декілька способів виконати повний пошук тексту, зокрема

1. Просто пошук у структурі даних пам'яті (на зразок списку), це відносно просто для реалізації, але не дуже добре. Крім того, він не підтримує складні запити без великої роботи.
2. Використання бази даних на зразок сервера SQL або Postgres. Хоча це працює і має підтримку майже всіх типів баз даних, це не завжди найкраще рішення для більш складних структур даних або складних запитів; але це те, що буде обговорюватися у цій статті.
3. Використання легкої технології пошуку на зразок [ЛюсенCity in New Jersey USA (optional, probably does not need a translation)](https://lucenenet.apache.org/) або SQLite FTS. Це посередині між двома висними рішеннями. Це складніше, ніж просто шукати список, але менш складний, ніж повний розв'язок бази даних. Тим не менш, це все ще досить складно реалізувати (особливо для отримання даних) і не масштабувати так само, як повний пошук розв'язків. Справді, багато інших пошукових технологій. [використовувати Lucene під капюшоном для ](https://www.elastic.co/search-labs/blog/elasticsearch-lucene-vector-database-gains) це дивовижні векторні можливості пошуку.
4. Використання пошукової системи, на зразок ElasticSearch, OpenSearch або Azure Search. Name Це найскладніше і найяскравіше рішення & ресурсів, але також найсильніше. Він також найбільш масштабований і може легко впоратися зі складними запитами. Я буду заглиблюватися в нестерпну глибину протягом наступного тижня або близько того, як налаштувати себе, налаштувавши і використовуючи OpenSearch з C#.

# База даних Повне пошук тексту за допомогою Postgres

У цьому блозі я недавно переїхав до використання Postgres для моєї бази даних. У Postgres є можливість повнотекстового пошуку, яка є дуже потужною і (дещо) простою для використання. Він також дуже швидкий і може легко впоратися зі складними запитами.

Коли будуєш Yout `DbContext` ви можете вказати поля, для яких буде увімкнено повнофункціональну можливість пошуку тексту.

Postgres використовує концепцію векторів пошуку для пришвидшення, ефективного повнотекстового пошуку. Вектор пошуку - це структура даних, яка містить слова у документі та їх позиції. По суті, додавання вектора пошуку для кожного рядка у базі даних надає змогу Postgres шукати слова у документі дуже швидко.
Для цього він використовує два спеціальні типи даних:

- TSVector: Особливий тип даних PostgreSQL, який зберігає список лексикмів (вважати його вектором слів). Це індексована версія документа, яка використовується для швидкого пошуку.
- TSquery: ще один особливий тип даних, який зберігає запит щодо пошуку, у якому містяться критерії пошуку і логічні оператори (наприклад, AN, NO).

Крім того, за допомогою цієї функції ви можете отримати оцінку результатів, залежно від того, наскільки добре вони збігаються з запитом щодо пошуку. Цей спосіб дуже потужний і дозволяє впорядкувати результати за допомогою респектабельності.
PostgreSQL призначає оцінку результатів на основі рецензування. Доступність можна підрахувати, розглянувши такі фактори, як близькість умов пошуку і те, як часто вони з'являються у документі.
Для обчислення цього рейтингу використовуються функції ts_ brank_ cd.

Докладніші відомості щодо можливостей пошуку у тексті можна знайти у Postgres [тут](https://www.postgresql.org/docs/current/textsearch.html)

## Робота з блоками сутностей

Пакунок кадрів сутностей Postgres [тут](https://www.npgsql.org/efcore/mapping/full-text-search.html?tabs=pg12%2Cv5) підтримує повнофункціональний пошук тексту. За допомогою цього пункту ви можете вказати, які поля буде індексовано, і спосіб їх опитування.

Для цього ми додаємо специфічні типи індексів до наших елементів, як це визначено у `DbContext`:

```csharp
   modelBuilder.Entity<BlogPostEntity>(entity =>
        {
            entity.HasIndex(x => new { x.Slug, x.LanguageId });
            entity.HasIndex(x => x.ContentHash).IsUnique();
            entity.HasIndex(x => x.PublishedDate);

                entity.HasIndex(b => new { b.Title, b.PlainTextContent})
                .HasMethod("GIN")
                .IsTsVectorExpressionIndex("english");
  ...
```

Тут ми додаємо повний індекс до `Title` і `PlainTextContent` наші поля `BlogPostEntity`. Ми також визначаємо, що індекс повинен використовувати `GIN` тип індексу і тип індексу `english` мовою. Це важливо, оскільки вказує Postgres на те, як індексувати дані і яку мову використовувати для слів, що стискаються і зупиняються.

Це, очевидно, проблема для нашого блогу, оскільки ми маємо багато мов. На жаль, зараз я просто використовую `english` мова для всіх дописів. Це те, про що я буду змушений поговорити в майбутньому, але зараз це працює досить добре.

Ми також додаємо індекс до нашого `Category` Сутність:

```csharp
     modelBuilder.Entity<CategoryEntity>(entity =>
        {
            entity.HasIndex(b => b.Name).HasMethod("GIN").IsTsVectorExpressionIndex("english");;
...

```

За допомогою цього Postgres можна створити пошуковий вектор для кожного рядка у базі даних. Цей вектор містить слова у `Title` і `PlainTextContent` поля. Тоді ми можемо використовувати цей вектор для пошуку слів у документі.

Це переводить на функцію to_ tsvector у SQL, яка створює вектор пошуку для рядка. Тоді ми можемо використати функцію ts_rank для оцінки результатів на основі рецензування.

```postgresql
SELECT to_tsvector('english', 'a fat  cat sat on a mat - it ate a fat rats');
to_tsvector
-----------------------------------------------------
'ate':9 'cat':3 'fat':2,11 'mat':7 'rat':12 'sat':4
```

Застосуйте це як міграцію до нашої бази даних і ми готові почати пошук.

# Пошук

## Індекс TsVector

Для пошуку ми використаємо `EF.Functions.ToTsVector` і `EF.Functions.WebSearchToTsQuery` функції для створення вектора пошуку і запиту. Тоді ми можемо використати `Matches` функція для пошуку запиту у векторі пошуку.

```csharp
  var posts = await context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .Where(x =>
                EF.Functions.ToTsVector("english", x.Title + " " + x.PlainTextContent)
                    .Matches(EF.Functions.WebSearchToTsQuery("english", query)) // Search in title and content
                && x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.WebSearchToTsQuery("english", query))) // Search in categories
                && x.LanguageEntity.Name == "en") // Filter by language
            .OrderByDescending(x =>
                EF.Functions.ToTsVector("english", x.Title + " " + x.PlainTextContent)
                    .Rank(EF.Functions.WebSearchToTsQuery("english", query))) // Rank by relevance
            .Select(x => new { x.Title, x.Slug })
            .ToListAsync();
       
```

Функція EF.Functions. WebSearchToTShoodry створює запит для рядка, заснованого на спільному синтаксичному синтаксисі веб- пошукового рушія.

```postgresql
SELECT websearch_to_tsquery('english', '"sad cat" or "fat rat"');
       websearch_to_tsquery
-----------------------------------
 'sad' <-> 'cat' | 'fat' <-> 'rat'
```

У цьому прикладі ви можете побачити, що це створює запит, який шукає слова "sad cat" або "fat ric" у документі. Це могутня риса, яка дозволяє нам з легкістю шукати складні запити.

Як було сказано, befrate ці методи обидва створюють вектор пошуку і запит для рядка. Потім ми використовуємо `Matches` функція для пошуку запиту у векторі пошуку. Ми також можемо використати `Rank` функція, що визначає результати за релевантністю.

Як ви бачите, це не простий запит, але він дуже потужний і дозволяє нам шукати слова в `Title`, `PlainTextContent` і `Category` наші поля `BlogPostEntity` і пов'язати їх з практичністю.

## WebAPI

Щоб скористатися цими даними (у майбутньому), ми можемо створити просту кінцеву точку WebAPI, яка приймає запит і повертає результати. Це простий контролер, який отримує запит і повертає результати:

```csharp
[ApiController]
[Route("api/[controller]")]
public class SearchApi(MostlylucidDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<JsonHttpResult<List<SearchResults>>> Search(string query)
    {;

        var posts = await context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .Where(x =>
                EF.Functions.ToTsVector("english", x.Title + " " + x.PlainTextContent)
                    .Matches(EF.Functions.WebSearchToTsQuery("english", query)) // Search in title and content
                && x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.WebSearchToTsQuery("english", query))) // Search in categories
                && x.LanguageEntity.Name == "en") // Filter by language
            .OrderByDescending(x =>
                EF.Functions.ToTsVector("english", x.Title + " " + x.PlainTextContent)
                    .Rank(EF.Functions.WebSearchToTsQuery("english", query))) // Rank by relevance
            .Select(x => new { x.Title, x.Slug })
            .ToListAsync();
        
        var output = posts.Select(x => new SearchResults(x.Title.Trim(), x.Slug)).ToList();
        
        return TypedResults.Json(output);
    }

```

## Створений стовпчик і TypeAhead

Альтернативний підхід до використання цих " простотних " індексів є використання створеного стовпчика для збереження вектора пошуку, а потім використання цього для пошуку. Це складніший підхід, але дозволяє краще працювати.
Тут ми змінюємо наше `BlogPostEntity` для додавання особливого типу стовпчика:

```csharp
   [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public NpgsqlTsVector SearchVector { get; set; }
```

Це обчислений стовпчик, який створює вектор пошуку для рядка. Тоді ми можемо використовувати цей стовпчик для пошуку слів у документі.

Потім ми встановлюємо цей індекс всередині визначення сутності (хоча це можна підтвердити, але це також може дозволити нам мати декілька мов, окреслюючи колонку мови для кожного допису).

```csharp
   entity.Property(b => b.SearchVector)
                .HasComputedColumnSql("to_tsvector('english', coalesce(\"Title\", '') || ' ' || coalesce(\"PlainTextContent\", ''))", stored: true);
```

Ви побачите, що ми використовуємо `HasComputedColumnSql` для явного визначення функції PostGreSQL для створення вектора пошуку. Ми також визначаємо, що колона зберігається у базі даних. Це важливо, оскільки вказує Postgres на збереження вектора пошуку у базі даних. За допомогою цього пункту ми можемо шукати слова у документі за допомогою вектора пошуку.

У базі даних цей рядок було створено для кожного з рядків, які є " лексиками " у документі і їх розташуваннями:

```csharp
"'1992':464 '1996':468 '20':480 '200':115 '2007':426 '2009':428 '2012':88 '2015':397 '2018':370 '2020':372 '2021':288,327,329,399 '2022':196,243,245,290 '2024':156,158,198 '25':21,477,486,522 '3d':346 '6':203,256 '8':179,485 '90':120,566 'ab':282 'access':221 'accomplish':14 'achiev':118 'across':60 'adapt':579 'advanc':134 'applic':168,316,526 'apr':155,197 'architect':83,97,159 'architectur':307,337 ...
```

### SearchAPI

Тоді ми можемо використовувати цей стовпчик для пошуку слів у документі. Ми можемо використати `Matches` функція для пошуку запиту у векторі пошуку. Ми також можемо використати `Rank` функція, що визначає результати за релевантністю.

```csharp
       var posts = await context.BlogPosts
            .Include(x => x.Categories)
            .Include(x => x.LanguageEntity)
            .Where(x =>
                // Search using the precomputed SearchVector
                x.SearchVector.Matches(EF.Functions.ToTsQuery("english", query + ":*")) // Use precomputed SearchVector for title and content
                && x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.ToTsQuery("english", query + ":*"))) // Search in categories
                && x.LanguageEntity.Name == "en") // Filter by language
            .OrderByDescending(x =>
                // Rank based on the precomputed SearchVector
                x.SearchVector.Rank(EF.Functions.ToTsQuery("english", query + ":*"))) // Use precomputed SearchVector for ranking
            .Select(x => new { x.Title, x.Slug })
            .ToListAsync();
```

Тут ви бачите, що ми також використовуємо інший конструктор запитів `EF.Functions.ToTsQuery("english", query + ":*")`  Це дозволяє нам запропонувати функцію типу TypeAhead (де ми можемо ввести тип e.g. 'cat' і get 'cat', 'cats', 'caterpilar' тощо).

Крім того, це надає змогу спростити основний запит блогу для пошуку запиту у `SearchVector` колонка. Це могутня риса, яка дозволяє нам шукати слова в `Title`, `PlainTextContent`. Ми все ще використовуємо індекс, який ми показали вище для `CategoryEntity`.

```csharp
x.Categories.Any(c =>
                    EF.Functions.ToTsVector("english", c.Name)
                        .Matches(EF.Functions.ToTsQuery("english", query + ":*"))) 
```

Потім ми використовуємо `Rank` функція, за допомогою якої слід впорядкувати результати за активністю на основі запиту.

```csharp
 x.SearchVector.Rank(EF.Functions.ToTsQuery("english", query + ":*")))
```

За допомогою цього пункту ми можемо використати кінцеву точку як наступну, де ми можемо передати перші декілька літер слова і повернутися до всіх дописів, які відповідають цьому слову:

Ви можете побачити [API в дії](https://www.mostlylucid.net/swagger/index.html) шукати `/api/SearchApi`. (Зауваження; я увімкнув Swagger для цього сайту так, щоб ви могли бачити API в дії, але більшість часу це повинно бути зарезервовано для ⇩IsDevelopment).

![API](searchapi.png?width=900&format=webp&quality=50)

У майбутньому я додам функцію TypeAhead до поля пошуку на сайті, який використовує цю функціональність.

# Включення

Ви можете бачити, що можна отримати потужні функціональні можливості пошуку за допомогою Postgres і Framework сутностей. Незважаючи на те, що вона має складні риси й обмеження, то ми мусимо враховувати (як і мову). В наступній частині я розповім про те, як ми зробимо це за допомогою OpenSearch - який має безліч інших конфігурацій, але більш потужний і масштабований.