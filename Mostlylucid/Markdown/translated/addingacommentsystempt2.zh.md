# 添加注释系统第2部分 - 保存注释

<!--category-- ASP.NET, Alpine.js, HTMX  -->
<datetime class="hidden">2024-08-31-009:00</datetime>

# 一. 导言 导言 导言 导言 导言 导言 一,导言 导言 导言 导言 导言 导言

上一个 [本系列的一部分](/blog/addingacommentsystempt1)我为评论系统建立了数据库。 我将报导如何在客户方面 以及ASP.NET Core 上管理这些评论。

[技选委

## 添加新注释

### `_CommentForm.cshtml`

这是 Razor 部分观点, 包含添加新评论的形式 。 你可以在第一个上载时看到它调用到 `window.mostlylucid.comments.setup()` 初始化编辑器。 这是一个简单的文本区域, 使用 `SimpleMDE` 编辑器允许内容丰富的文本编辑 。

```razor
@model Mostlylucid.Models.Comments.CommentInputModel

 
<div x-data="{ initializeEditor() { window.mostlylucid.comments.setup()  } }"
     x-init="initializeEditor()" class="max-w-none dark:prose-dark" id="commentform">
    <section id="commentsection" ></section>
    
    <input type="hidden" asp-for="BlogPostId" />
    <div asp-validation-summary="All" class="text-red-500" id="validationsummary"></div>
    <p class="font-body text-lg font-medium text-primary dark:text-white pb-8">Welcome @Model.Name please comment below.</p>
    
    <div asp-validation-summary="All" class="text-red-500" id="validationsummary"></div>
    <!-- Username Input -->
    <div class="flex space-x-4"> <!-- Flexbox to keep Name and Email on the same line -->

        <!-- Username Input -->
        <label class="input input-bordered flex items-center gap-2 mb-2 dark:bg-custom-dark-bg bg-white w-1/2">
            <i class='bx bx-user'></i>
            <input type="text" class="grow text-black dark:text-white bg-transparent border-0"
                   asp-for="Name" placeholder="Name (required)" />
        </label>

        <!-- Email Input -->
        <label class="input input-bordered flex items-center gap-2 mb-2 dark:bg-custom-dark-bg bg-white w-1/2">
            <i class='bx bx-envelope'></i>
            <input type="email" class="grow text-black dark:text-white bg-transparent border-0"
                   asp-for="Email" placeholder="Email (optional)" />
        </label>

    </div>

    <textarea id="commenteditor" class="hidden w-full h-44 dark:bg-custom-dark-bg bg-white text-black dark:text-white rounded-2xl"></textarea>

    <input type="hidden" asp-for="ParentId"></input>
    <button class="btn btn-outline btn-sm mb-4" hx-action="Comment" hx-controller="Comment" hx-post hx-vals x-on:click.prevent="window.mostlylucid.comments.setValues($event)"  hx-swap="outerHTML" hx-target="#commentform">Comment</button>
</div>
```

在这里,我们使用阿尔卑斯山 `x-init` 调用初始化编辑器 。 这是一个简单的文本区域, 使用 `SimpleMDE` 编辑器可以允许内容丰富的文字编辑( 因为为什么不: ) 。 ) 。

```html
<div x-data="{ initializeEditor() { window.mostlylucid.comments.setup()  } }"
     x-init="initializeEditor()" class="max-w-none dark:prose-dark" id="commentform">
```

#### `window.mostlylucid.comments.setup()`

这活在 `comment.js` 并负责初始化简单的 MDE 编辑器 。

```javascript
    function setup ()  {
    if (mostlylucid.simplemde && typeof mostlylucid.simplemde.initialize === 'function') {
        mostlylucid.simplemde.initialize('commenteditor', true);
    } else {
        console.error("simplemde is not initialized correctly.");
    }
};
```

这是一个简单的函数, 检查是否 `simplemde` 对象被初始化,如果如此调用 `initialize` 功能在它上。

## 保存注释

为了保存注释, 我们使用 HTMX 将 POPST 到 `CommentController` ,然后将注释保存到数据库。

```razor
  <button class="btn btn-outline btn-sm mb-4" hx-action="Comment" hx-controller="Comment" hx-post hx-vals x-on:click.prevent="window.mostlylucid.comments.setValues($event)"  hx-swap="outerHTML" hx-target="#commentform">Comment</button>
```

此处使用 [HTMX 标签助手](https://www.nuget.org/packages/Htmx.TagHelpers) 返回到 `CommentController` 然后将表格与新注释互换。

然后我们勾入 `mostlylucid.comments.setValues($event)` 我们用它来弹出 `hx-values` 属性( 仅有必要, 因为简单模式需要手动更新) 。

```javascript
    function setValues (evt)  {
    const button = evt.currentTarget;
    const element = mostlylucid.simplemde.getinstance('commenteditor');
    const content = element.value();
    const email = document.getElementById("Email");
    const name = document.getElementById("Name");
    const blogPostId = document.getElementById("BlogPostId");

    const parentId = document.getElementById("ParentId")
    const values = {
        content: content,
        email: email.value,
        name: name.value,
        blogPostId: blogPostId.value,
        parentId: parentId.value
    };

    button.setAttribute('hx-vals', JSON.stringify(values));
};
}
```

### 评论员

注释控制器 `save-comment` 动作负责将注释保存到数据库中。 也向博客所有者(me)发送电子邮件,

```csharp
    [HttpPost]
    [Route("save-comment")]
    public async Task<IActionResult> Comment([Bind(Prefix = "")] CommentInputModel model )
    {
        if (!ModelState.IsValid)
        {
            return PartialView("_CommentForm", model);
        }
        var postId = model.BlogPostId;
        ;
        var name = model.Name ?? "Anonymous";
        var email = model.Email ?? "Anonymous";
        var comment = model.Content;

        var parentCommentId = model.ParentId;
        
      var htmlContent=  await commentService.Add(postId, parentCommentId, name, comment);
      if (string.IsNullOrEmpty(htmlContent))
      {
          ModelState.AddModelError("Content", "Comment could not be saved");
          return PartialView("_CommentForm", model);
      }
        var slug = await blogService.GetSlug(postId);
        var url = Url.Action("Show", "Blog", new {slug }, Request.Scheme);
        var commentModel = new CommentEmailModel
        {
            SenderEmail = email ?? "",
            Comment = htmlContent,
            PostUrl = url??string.Empty,
        };
        await sender.SendEmailAsync(commentModel);
        model.Content = htmlContent;
        return PartialView("_CommentResponse", model);
    }
```

你会看到这能做一些事情:

1. 将批注添加到 DB 中( 这也可以进行 MarkDig 转换, 将标记下调转换为 HTML )。
2. 如果有错误, 它会以错误返回表单 。 (注I现在还有一项追踪活动,记录到Seq的错误)。
3. 如果评论被保存, 它会发送电子邮件给我 与注释和邮址 URL 。

如果我以我的身份登录(使用 [我的Google Auth(谷歌Auth)的东西](/blog/addingidentityfreegoogleauth)) ) 这只检查我的 Google ID 然后设置“ IsAdmin” 属性, 让我看到评论, 必要时删除它们 。

# 在结论结论中

因此,这就是第二部分,我如何保存这些评论。 还有几块遗漏; 线条( 这样您就可以对评论做出回应) 列出您自己的评论并删除评论 。 下个职位我负责