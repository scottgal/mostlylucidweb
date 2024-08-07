# 带有标记的超级简单注释系统

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024-008-006T18:50</datetime>

注:进展中的工作

我一直在为我的博客寻找一个使用 Markdown 的简单评论系统。 我找不到我喜欢的, 所以我决定自己写。 这是一个简单的评论系统, 使用 Markdown 格式化格式化。 其第二部分会将电子邮件通知添加到这个系统, 它会给我发送一个电子邮件, 与评论链接, 允许我在网站显示之前“ 批准 ” 。

对于一个生产系统来说,它通常会使用数据库, 但对于这个例子,我只需要使用标记。

## 评论系统

批注系统非常简单。 我只是为每个批注保存了一个标记文件, 包括用户名称、 电子邮件和注释。 批注会按收到的顺序在页面上显示 。

要输入注释, 我使用基于 Javascript 的 Markdown 编辑器SimpleMDE 。
这个包含在我的_布局.cshtml如下:

```html
<!-- Include the SimpleMDE CSS, here I use a dark theme -->
  <link rel="stylesheet" href="https://cdn.rawgit.com/xcatliu/simplemde-theme-dark/master/dist/simplemde-theme-dark.min.css">

<!--Later in the page include the JS for SimpleMDE -->
<script src="https://cdn.jsdelivr.net/simplemde/latest/simplemde.min.js"></script>

```

然后,我先在页面载荷和HTMX载荷上初始化了简易MDE编辑器:

```javascript
    var simplemde;
    document.addEventListener('DOMContentLoaded', function () {
    
        if (document.getElementById("comment") != null)
        {
        
       simplemde = new SimpleMDE({ element: document.getElementById("comment") });
       }
        
    });
    document.body.addEventListener('htmx:afterSwap', function(evt) {
        if (document.getElementById("comment") != null)
        {
        simplemde = new SimpleMDE({ element: document.getElementById("comment") });
        
        }
    });
```

在此我指定我的评论文本区域名为“ 备注 ”, 并且只有在检测到后才初始化 。 在这里, 我将表格包装在“ 验证” 中( 我将其传送到视图模式中 ) 。 这意味着我可以确保只有登录过( 目前与谷歌一起) 的人才能添加评论 。

```razor
@if (Model.Authenticated)
    {
        
  
        <div class=" max-w-none border-b border-grey-lighter py-8 dark:prose-dark sm:py-12">
            <p class="font-body text-lg font-medium text-primary dark:text-white">Welcome @Model.Name please comment below.</p>
            <textarea id="comment"></textarea>
       <button class="btn btn-primary" hx-action="Comment" hx-controller="Blog" hx-post hx-vals="js:{comment: simplemde.value()}" hx-route-slug="@Model.Slug" hx-swap="outerHTML" hx-target="#comment" onclick="prepareForSubmission()">Comment</button>
        </div>
    }
    else
    {
       ...
    }
```

您也会注意到我在这里使用 HTMX 来发布批注。 我使用 hx- vals 属性和 JS 调用来获取批注的值 。 此调用将随“ Comment” 动作发布在博客控制器上。 然后用新的批注转换出来 。

```csharp
    [HttpPost]
    [Route("comment")]
    [Authorize]
    public async Task<IActionResult> Comment(string slug, string comment)
    {
        var principal = HttpContext.User;
        principal.Claims.ToList().ForEach(c => logger.LogInformation($"{c.Type} : {c.Value}"));
        var nameIdentifier = principal.FindFirst("sub");
        var userInformation = GetUserInfo();
       await commentService.AddComment(slug, userInformation, comment, nameIdentifier.Value);
        return RedirectToAction(nameof(Show), new { slug });
    }

```