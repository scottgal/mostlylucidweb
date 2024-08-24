# 带有标记的超级简单注释系统

<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024-008-006T18:50</datetime>

注:进展中的工作

我一直在为我的博客寻找一个简单的评论系统 使用Markdown。 我找不到我喜欢的 所以我决定自己写 这是一个简单的注释系统, 使用 Markdown 格式化 。 其第二部分将在系统上添加电子邮件通知, 发送电子邮件给我, 与评论链接, 允许我在网站显示前“ 赞同 ” 。

对于一个生产系统来说,它通常会使用数据库, 但对于这个例子,我只需要使用标记。

## 评论系统

评论系统非常简单。 我只是为每个评论保存了一个标记文件, 包括用户的姓名、电子邮件和评论。 评论随后按收到的先后顺序在页面上显示。

要输入注释, 我使用基于 Javascript 的 Markdown 编辑器SimpleMDE 。
这个包含在我的 _布局.cshtml如下:

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

在此我具体说明我的评论文本区域 被称为“ 评论 ”, 只有在检测到后才初始化 。 在这里,我将表格包装在“经认证的”中(我把它传送到视图模式中)。 这意味着,我可以确保只有登入(目前与谷歌一起登入)的人才能补充评论。

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

你也会注意到我在这里使用HTMX来发布评论。 我使用 hx-vals 属性和联署材料来获取评论的价值。 并张贴到博客控制器“Comment ” 动作 。 然后用新评论转换出来。

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