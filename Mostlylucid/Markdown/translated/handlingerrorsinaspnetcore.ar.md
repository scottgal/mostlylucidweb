# أخطاء المناولة (غير المهونة) في الشبكة

<!--category-- ASP.NET, Umami -->
<datetime class="hidden">الساعة24/2024-00/08-00/17</datetime>

## أولاً

في أي تطبيق على الإنترنت من المهم التعامل مع الأخطاء برشاقة وينطبق هذا بصفة خاصة على بيئة الإنتاج حيث ترغب في توفير تجربة جيدة للمستعملين وعدم الكشف عن أي معلومات حساسة. في هذه المقالة سوف ننظر في كيفية التعامل مع الأخطاء في تطبيق ASP.net الأساسي.

[رابعاً -

## المشكلة

عندما يحدث استثناء غير مسند في تطبيق ASP.net الأساسي، السلوك الافتراضي هو إعادة صفحة خطأ عام مع رمز وضع 500. وهذا ليس مثالياً لعدد من الأسباب:

1. إنها قبيحة ولا توفر خبرة مستعملة جيدة
2. إنها لا توفر أي معلومات مفيدة للمستخدم
3. غالباً ما يكون من الصعب تصحيح هذه المسألة لأن رسالة الخطأ عامة جداً.
4. إنها قبيحة ؛ الصفحة العامة للخطأ في المتصفح هي مجرد شاشة رمادية مع بعض النص.

## الإحلال

في قاعدة ASP.net هناك ميزة أنيق بناء في الذي يسمح لنا للتعامل مع هذا النوع من الأخطاء.

```csharp
app.UseStatusCodePagesWithReExecute("/error/{0}");
```

نحن نضع هذا في `Program.cs` ملف مبكر في خط الأنابيب. هذا سيمسك بأي رمز وضع ليس 200 ويعاد توجيهه إلى `/error` المسار مع حالة رمز كمعامل.

متحكم الخطأ سوف ينظر إلى شيء كهذا:

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

هذا المتحكم سيتعامل مع الخطأ وإرجاع a مخصص اعرض مستند على حالة رمز. يمكننا ايضاً ان نسجل الURL الأصلي الذي تسبب بالخطأ ونمرره الى الواجهة.
إذا كان لدينا خدمة تسجيل/ تحليل مركزية يمكننا أن نسجل هذا الخطأ في تلك الخدمة.

وفيما يلي آراءنا:

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

حق بسيط جداً؟ ويمكننا أيضاً تسجيل الخطأ في خدمة قطع الأشجار مثل Application Insights أو Serilog. بهذه الطريقة يمكننا أن نتتبع الأخطاء ونصلحها قبل أن تصبح مشكلة
في حالتنا نسجل هذا كحدث لخدمة تحليل الامامي وبهذه الطريقة يمكننا أن نتتبع عدد الأخطاء 404 التي لدينا ومن أين تأتي.

هذا يحتفظ بصفحتك أيضاً وفقاً للتصميم والمخطط المختارين.

![الصفحة](new404.png)

## في الإستنتاج

هذه طريقة بسيطة للتعامل مع الأخطاء في تطبيق ASP.net الأساسي. فهو يوفر تجربة جيدة للمستعملين ويسمح لنا بتتبع الأخطاء. انها فكرة جيدة لسجل الأخطاء في خدمة قطع الأشجار حتى تتمكن من متابعة لهم وإصلاحها قبل أن تصبح مشكلة.