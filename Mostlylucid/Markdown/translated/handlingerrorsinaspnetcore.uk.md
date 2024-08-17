# Обробка (без обробки) помилок у ядрі ASP. NET

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">2024- 08- 17T02: 00</datetime>

## Вступ

В будь-якій веб-програмі важливо з приємністю поводитися з помилками. Це особливо стосується виробничого середовища, де ви хочете добре використовувати і не розкривати конфіденційної інформації. У цій статті ми розглянемо, як поводитися з помилками у програмі ASP.NET.

[TOC]

## Проблема

Якщо у основній програмі ASP. NET без обробки відбувається виключення, типовою поведінкою програми є повернення загальної сторінки помилок зі кодом стану 500. Це не є ідеальним з багатьох причин:

1. Це потворно і не надає хорошого досвіду.
2. Вона не надасть користувачеві жодної корисної інформації.
3. Часто важко з'ясувати проблему, тому що повідомлення про помилку таке загальне.
4. Це потворно; сторінка загальної помилки навігатора - це лише сірий екран з якимось текстом.

## Розв'язання

В ядрах ASPNET є акуратна структура, яка дозволяє нам впоратися з такими помилками.

```csharp
app.UseStatusCodePagesWithReExecute("/error/{0}");
```

Ми вкладаємо це в нашу `Program.cs` файл на початку трубопроводу. За допомогою цього пункту можна отримати будь- який код стану, який не є 200 і переспрямовує стан на `/error` маршрут з кодом стану як параметром.

Наш контролер помилок виглядатиме приблизно так:

```csharp
    [Route("/error/{statusCode}")]
    public IActionResult HandleError(int statusCode)
    {
        // Retrieve the original request information
        var statusCodeReExecuteFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
        
        if (statusCodeReExecuteFeature != null)
        {
            // Access the original path and query string that caused the error
            var originalPath = statusCodeReExecuteFeature.OriginalPath;
            var originalQueryString = statusCodeReExecuteFeature.OriginalQueryString;

            
            // Optionally log the original URL or pass it to the view
            ViewData["OriginalUrl"] = $"{originalPath}{originalQueryString}";
        }

        // Handle specific status codes and return corresponding views
        switch (statusCode)
        {
            case 404:
            return View("NotFound");
            case 500:
            return View("ServerError");
            default:
            return View("Error");
        }
    }
```

Цей контролер оброблятиме помилку і поверне нетипову область перегляду на основі коду стану. Ми також можемо записати оригінальний URL, який спричинив помилку і передав її на перегляд.
Якби у нас була центральна служба ведення лісозаготівель і аналітичної служби, ми б могли зареєструвати цю помилку до цієї служби.

Ось наші погляди:

```razor
<h1>404 - Page not found</h1>

<p>Sorry that Url doesn't look valid</p>
@section Scripts {
    <script>
            document.addEventListener('DOMContentLoaded', function () {
                if (!window.hasTracked) {
                    umami.track('404', { page:'@ViewData["OriginalUrl"]'});
                    window.hasTracked = true;
                }
            });

    </script>
}
```

Досить просто, правильно? Ми також можемо зареєструвати помилку до служби лісозаготівлі на зразок "Програмі" або "серілога." Таким чином ми зможемо стежити за помилками і виправляти їх, перш ніж вони стануть проблемою.
У нашому випадку ми записуємо це як випадок для нашої аналітичної служби Умамі. Таким чином ми можемо відстежити скільки 404 помилок ми маємо і звідки вони походять.

Це також тримає вашу сторінку в гармонії з обраним планом та дизайном.

![404 Сторінка](new404.png)

## Включення

Це простий спосіб роботи з помилками у програмі ASP. NET. Вона дає добрий досвід для користувача і дозволяє нам стежити за помилками. Непогана ідея записувати помилки до служби лісозаготівлі, щоб ви могли стежити за ними і виправити їх, перш ніж вони стануть проблемою.


