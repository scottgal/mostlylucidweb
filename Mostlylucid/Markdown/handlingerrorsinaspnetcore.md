# Handling (unhandled) errors in ASP.NET Core

<!--category-- ASP.NET, Umami -->

<datetime class="hidden">2024-08-17T02:00</datetime>

## Introduction
In any web application it's important to handle errors gracefully. This is especially true in a production environment where you want to provide a good user experience and not expose any sensitive information. In this article we'll look at how to handle errors in an ASP.NET Core application.

[TOC]

## The Problem
When an unhandled exception occurs in an ASP.NET Core application, the default behavior is to return a generic error page with a status code of 500. This is not ideal for a number of reasons:
1. It's ugly and doesn't provide a good user experience.
2. It doesn't provide any useful information to the user.
3. It's often hard to debug the issue because the error message is so generic.
4. It's ugly; the generic browser error page is just a grey screen with some text.

## The Solution
In ASP.NET Core there's a neat feature build in which allows us to handle these sort of errors. 

```csharp
app.UseStatusCodePagesWithReExecute("/error/{0}");
```

We put this in our `Program.cs` file early on in the pipeline. This will catch any status code that is not a 200 and redirect to the `/error` route with the status code as a parameter.

Our error controller will look something like this:

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

This controller will handle the error and return a custom view based on the status code. We can also log the original URL that caused the error and pass it to the view.
If we had a central logging / analytics service we could log this error to that service.

Our Views are as follows:

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
Pretty simple right? We can also log the error to a logging service like Application Insights or Serilog. This way we can keep track of errors and fix them before they become a problem.
In our case we log this as an event to our Umami analytics service. This way we can keep track of how many 404 errors we have and where they are coming from.

This also keeps your page in accordance with your chosen layout and design. 

![404 Page](new404.png)


## In Conclusion
This is a simple way to handle errors in an ASP.NET Core application. It provides a good user experience and allows us to keep track of errors. It's a good idea to log errors to a logging service so you can keep track of them and fix them before they become a problem.
```