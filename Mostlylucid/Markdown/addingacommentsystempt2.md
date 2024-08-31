# Adding a Comment System Part 2 - Saving Comments

<!--category-- ASP.NET, Alpine.js, HTMX  -->
<datetime class="hidden">2024-08-31T09:00</datetime>

# Introduction
In the previous [part in this series](/blog/addingacommentsystempt1), I set up the database for the comments system. In this post, I'll cover how saving the comments are managed client side and in ASP.NET Core.

[TOC]

## Add New Comment

### `_CommentForm.cshtml`
This is a Razor partial view that contains the form for adding a new comment. You can see on first load it calls to `window.mostlylucid.comments.setup()` which initializes the editor. This is a simple textarea that uses the `SimpleMDE` editor to allow for rich text editing.

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

Here we use the Alpine.js `x-init` call to initialize the editor. This is a simple textarea that uses the `SimpleMDE` editor to allow for rich text editing (because why not :)) .

```html
<div x-data="{ initializeEditor() { window.mostlylucid.comments.setup()  } }"
     x-init="initializeEditor()" class="max-w-none dark:prose-dark" id="commentform">
```    

####  `window.mostlylucid.comments.setup()`
This lives in the `comment.js` and is responsible for initializing the simpleMDE editor. 

```javascript
    function setup ()  {
    if (mostlylucid.simplemde && typeof mostlylucid.simplemde.initialize === 'function') {
        mostlylucid.simplemde.initialize('commenteditor', true);
    } else {
        console.error("simplemde is not initialized correctly.");
    }
};
```

This is a simple function that checks if the `simplemde` object is initialized and if so calls the `initialize` function on it.

## Saving the comment
To save the comment we use HTMX to do a POST to the `CommentController` which then saves the comment to the database. 

```razor
  <button class="btn btn-outline btn-sm mb-4" hx-action="Comment" hx-controller="Comment" hx-post hx-vals x-on:click.prevent="window.mostlylucid.comments.setValues($event)"  hx-swap="outerHTML" hx-target="#commentform">Comment</button>
```
This uses the [HTMX tag helper](https://www.nuget.org/packages/Htmx.TagHelpers) to post back to the `CommentController` and then swaps the form with the new comment.

Then we hook into the `mostlylucid.comments.setValues($event)` which we use to populate the `hx-values` atribute (this is only necessary as simplemde needs to be updated manually). 

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

### CommentController 
The comment controller's `save-comment` action is responsible for saving the comment to the database. It also sends an email to the blog owner (me) when a comment is added.

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

You'll see that this does  a few things:
1. Adds the comment to the DB (this also does a MarkDig transformation to convert markdown to HTML).
2. If there's an error it returns the form with the error. (Note I also now have a tracing activity that logs the error to Seq).
3. If the comment is saved it sends an email to me with the comment and the post URL.

This post URL then lets me click the post, if I'm logged in as me (using [my Google Auth thing](/blog/addingidentityfreegoogleauth)). This just checks for my Google ID then sets the 'IsAdmin' property which lets me see the comments and delete them if necessary.

# In Conclusion
So that's part 2, how I save the comments. There's still a couple of pieces missing; threading (so you can reply to a comment), listing your own comments and deleting comments. I'll cover those in the next post.