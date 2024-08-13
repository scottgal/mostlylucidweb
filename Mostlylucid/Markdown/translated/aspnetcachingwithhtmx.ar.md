# تجميع البيانات الأساسية مع HTMX

<!--category-- ASP.NET, HTMX -->
<datetime class="hidden">2024-08-08-12-T00:50</datetime>

## أولاً

الكثب هو تقنية مهمة لتحسين خبرة المستخدم من خلال تحميل المحتوى بشكل أسرع وتخفيض الحمل على خادمك. في هذه المقالة سأريكم كيف تستخدمون خصائص كبسولات ASP.Net Corre مع HTMX لإخفاء المحتوى على جانب العميل.

[رابعاً -

## إنشاء

وهناك نوعان من أنواع المعجنات المعروضة في قاعدة البيانات الإحصائية الأساسية (ASP.net)

- Reponse Cache - هذه بيانات مخبأة على العميل أو في خوادم procy الوسيطة (أو كلاهما) وتستخدم لإخفاء كامل الرد على الطلب.
- مخرجات الـ هو البيانات يعمل خادم و هو مُستخدَم إلى إخفاء مخرجات من a متحكم إجراء.

لإنشاء هذه في ASP.net الأساسية تحتاج إلى إضافة بعض الخدمات في خدماتك `Program.cs` & لا شيء

### رد الرد

```csharp
builder.Services.AddResponseCaching();
app.UseResponseCaching();
```

### الناتج كِِِِِِِِِِ مِِِكِِِكِِِِكِكِكِِكِكِكِكِكِكِكِكِكِ

```csharp
services.AddOutputCache();
app.UseOutputCache();
```

## رد الرد

في حين أنه من الممكن وضع رد الاستجابة في `Program.cs` غالبا ما يكون غير مرن قليلا (خاصة عند استخدام طلبات HTMX كما اكتشفت). يمكنك إنشاء ردّ الضبط في إجراءات تحكمك باستخدام الـ `ResponseCache` ........... ،..............................................................................................................

```csharp
        [ResponseCache(Duration = 300, VaryByHeader = "hx-request", VaryByQueryKeys = new[] {"page", "pageSize"}, Location = ResponseCacheLocation.Any)]
```

هذا سيخبأ الرد لـ 300 ثانية ويغيّر المخبأ بـ `hx-request` رأس رأساً و رأساً و رأساً `page` وقد عقد مؤتمراً بشأن `pageSize` البارامترات المرجعية. نحن أيضاً نحدد `Location` ثالثاً - `Any` وهذا يعني أن الرد يمكن أن يخبئ على العميل أو على خوادم وسيطة أو كلاهما.

(هنا) `hx-request` الرأس هو الرأس الذي يرسله HTMX مع كل طلب. هذا مهم لأنه يسمح لك بإخفاء الرد بشكل مختلف بناءً على ما إذا كان طلب HTMX أو طلب عادي.

هذا هو الحالي `Index` طريقة العمل. يُرى يو يُرى أنّنا نقبل a صفحة و صفحة Siz الحجم المعامل هنا و أضفنا هذه كمتغيرات `ResponseCache` ........... ،.............................................................................................................. بمعنى أن الردود "فهرسة" بواسطة هذه المفاتيح و تخزين محتوى مختلف استناداً إلى هذه.

نحن أيضاً لدينا ما يلي: `if(Request.IsHtmx())` هذا مبني على [رزمة الشبكة HTMTMX.Net](https://github.com/khalidabuhakmeh/Htmx.Net)  وفي الأساس التحقق من نفس الشيء `hx-request` الرأس الذي نستخدمه لإختلاف المخبأ هنا نرجع وجهة نظر جزئية إذا كان الطلب من HTMX.

```csharp
    public async Task<IActionResult> Index(int page = 1,int pageSize = 5)
    {
            var authenticateResult = GetUserInfo();
            var posts =await blogService.GetPosts(page, pageSize);
            posts.LinkUrl= Url.Action("Index", "Home");
            if (Request.IsHtmx())
            {
                return PartialView("_BlogSummaryList", posts);
            }
            var indexPageViewModel = new IndexPageViewModel
            {
                Posts = posts, Authenticated = authenticateResult.LoggedIn, Name = authenticateResult.Name,
                AvatarUrl = authenticateResult.AvatarUrl
            };
            
            return View(indexPageViewModel);
    }
```

## الناتج كِِِِِِِِِِ مِِِكِِِكِِِِكِكِكِِكِكِكِكِكِكِكِكِكِ

Caching مخرجات هو خادم جانب من استجابة Caching. إنه يخبئ مخرجات عمل تحكم. وفي جوهر الأمر، يخزن خادوم الشبكة نتيجة الطلب ويقدمه للطلبات اللاحقة.

```csharp
       [OutputCache(Duration = 3600,VaryByHeaderNames = new[] {"hx-request"},VaryByQueryKeys = new[] {"page", "pageSize"})]
```

هنا نحن نقوم بكح الناتج الناتج من إجراء المتحكّم لعمل لـ 3600 ثانية `hx-request` رأس رأساً و رأساً و رأساً `page` وقد عقد مؤتمراً بشأن `pageSize` البارامترات المرجعية.
كما نقوم بتخزين جانب خادم البيانات لوقت مهم (الوظائف فقط تحديث مع دفعة docker) هذا هو تعيين إلى أطول من الاستجابة Cach; يمكن في الواقع أن يكون لا نهائي في حالتنا ولكن 3600 ثانية هو حل وسط جيد.

كما مع ردّ المحفظة نحن نستعمل `hx-request` ترويسة إلى تغيير المخبأ على أساس ما إذا كان الطلب من HTMX أم لا.

## 

كما توفر الشبكة دعماً مدمجاً أيضاً لملفات تجميع الملفات الثابتة. يتم القيام بذلك من خلال وضع `Cache-Control` الرأس في الإستجابة. يمكنك وضع هذا في `Program.cs` ملف ملفّيّاً.
ملاحظة أن الترتيب 'i' مهم هنا، إذا كانت ملفاتك الساكنة بحاجة لدعم دعم الترخيص، ينبغي أن تقوم بنقل `UseAuthorization` قبل `UseStaticFiles` (اليونيفيا الوسطى الوسطى). The استخدام Httpsrefure يجب أن يكون الوازع الوسيط قبل الواجهة الوسيطة لـ IF تعتمد على هذه الميزة.

```csharp
app.UseHttpsRedirection();
var cacheMaxAgeOneWeek = (60 * 60 * 24 * 7).ToString();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append(
            "Cache-Control", $"public, max-age={cacheMaxAgeOneWeek}");
    }
});
app.UseRouting();
app.UseCors("AllowMostlylucid");
app.UseAuthentication();
app.UseAuthorization();
```

## ثالثاً - استنتاج

تَشْدِيدُ ٱلْأَسْفَارِ ٱلْمُقَدَّسَةِ لِتَحْسِينِ أَعْدَادِكَ ٱلَّتِي تُقَدِّمُهَا. باستخدام ميزات الكثبان المضمنة لـ ASP.net Kory يمكنك بسهولة إخفاء المحتوى على جانب العميل أو الخادم. باستخدام HTMX يمكنك اخفاء المحتوى على جانب العميل وتقديم وجهات نظر جزئية لتحسين تجربة المستخدم.