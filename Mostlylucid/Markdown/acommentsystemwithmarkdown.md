# A Super Simple Comment System with Markdown
<!--category-- ASP.NET, Markdown -->
<datetime class="hidden">2024-08-06T18:50</datetime>

NOTE: WORK IN PROGRESS

I've been looking for a simple comment system for my blog that uses Markdown. I couldn't find one that I liked, so I decided to write my own. This is a simple comment system that uses Markdown for formatting. The second part of this will add email notifications to the system which will send me an email with a link to the comment, allowing me to 'approve' it before it is displayed on the site.

Again for a production system this would normally use a database, but for this example I'm just going to use markdown.


## The Comment System
The comment system is incredibly simple. I just have a markdown file being saved for each comment with the user's name, email and comment. The comments are then displayed on the page in the order they were received.

To enter the comment I use SimpleMDE, a Javascript based Markdown editor.
This is included in my _Layout.cshtml as follows:

```html
<!-- Include the SimpleMDE CSS, here I use a dark theme -->
  <link rel="stylesheet" href="https://cdn.rawgit.com/xcatliu/simplemde-theme-dark/master/dist/simplemde-theme-dark.min.css">

<!--Later in the page include the JS for SimpleMDE -->
<script src="https://cdn.jsdelivr.net/simplemde/latest/simplemde.min.js"></script>

```

I then initialize the SimpleMDE editor on both page load and HTMX load:

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

Here I specify that my comment textarea is called 'comment' and only initialize once it's detected. Here I wrap the form in a 'IsAuthenticated' (which I pass into the ViewModel). This means I can ensure that only those who have logged in (at present with Google) can add comments. 

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

You'll also notice I use HTMX here for the comment posting. Where I use the hx-vals attribute and a JS call to get the value for the comment. This is then posted to the Blog controller with the 'Comment' action. This is then swapped out with the new comment. 

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