# Ensuring your IHostedService (or IHostedLifecycleService) is a single instance
<!--category-- ASP.NET -->
<datetime class="hidden">2024-08-21T16:08</datetime>
## Introduction
This is a dumb little article because I was a bit confused about how to ensure that my `IHostedService` was a single instance. I thought it was a bit more complicated than it actually was. So I thought I'd write a little article about it. Just in case anyone else was confused about it.

In the [prior article](/blog/addingasyncsendingforemails), we covered how to create a background service using the `IHostedService` interface for sending emails. This article will cover how to ensure that your `IHostedService` is a single instance.
This might be obvious to some, but it's not to others (and wasn't immediately to me!).

[TOC]

## Why is this an issue?
Well its an issue as most of the articles out these cover how to use a `IHostedService` but they don't cover how to ensure that the service is a single instance. This is important as you don't want multiple instances of the service running at the same time.

What do I mean? Well in ASP.NET the way to register an IHostedService or IHostedlifeCycleService (basically the same with more overrides for lifecycle management) you use this

```csharp
  services.AddHostedService(EmailSenderHostedService);
```
What that does is calls into this backend code:

```csharp
public static IServiceCollection AddHostedService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THostedService>(this IServiceCollection services)
            where THostedService : class, IHostedService
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, THostedService>());

            return services;
        }

```
Which is fine and dandy but what if you want to post a new message directly to this service from say a `Controller` action?

```csharp

public class ContactController(EmailSenderHostedService sender,ILogger<BaseController> logger) ...
{
   [HttpPost]
    [Route("submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit([Bind(Prefix = "")] ContactViewModel comment)
    {
        ViewBag.Title = "Contact";
        //Only allow HTMX requests
        if(!Request.IsHtmx())
        {
            return RedirectToAction("Index", "Contact");
        }
      
        if (!ModelState.IsValid)
        {
            return PartialView("_ContactForm", comment);
        }

        var commentHtml = commentService.ProcessComment(comment.Comment);
        var contactModel = new ContactEmailModel()
        {
            SenderEmail = string.IsNullOrEmpty(comment.Email) ? "Anonymous" : comment.Email,
            SenderName = string.IsNullOrEmpty(comment.Name) ? "Anonymous" : comment.Name,
            Comment = commentHtml,
        };
        await sender.SendEmailAsync(contactModel);
        return PartialView("_Response",
            new ContactViewModel() { Email = comment.Email, Name = comment.Name, Comment = commentHtml });

        return RedirectToAction("Index", "Home");
    }
   }
```
Either you need to create an interface which itself implements `IHostedService` then call into the method on that or you need to ensure that the service is a single instance. The latter is the easiest way to do this (depends on your scenario though, for testing the Interface method might be preferred).



### IHostedService
You'll note here that it registers the service as an `IHostedService`, this is to do with the lifecycle management of this service as the ASP.NET framework will use this registration to fire the events of this service (`StartAsync` and `StopAsync` for IHostedService). See below, `IHostedlifeCycleService` is just a more detailed version of IHostedService.

```csharp
  /// <summary>
  /// Defines methods for objects that are managed by the host.
  /// </summary>
  public interface IHostedService
  {
    /// <summary>
    /// Triggered when the application host is ready to start the service.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous Start operation.</returns>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the shutdown process should no longer be graceful.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous Stop operation.</returns>
    Task StopAsync(CancellationToken cancellationToken);
  }

namespace Microsoft.Extensions.Hosting
{
  /// <summary>
  /// Defines methods that are run before or after
  /// <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StartAsync(System.Threading.CancellationToken)" /> and
  /// <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" />.
  /// </summary>
  public interface IHostedLifecycleService : IHostedService
  {
    /// <summary>
    /// Triggered before <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StartAsync(System.Threading.CancellationToken)" />.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    Task StartingAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Triggered after <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StartAsync(System.Threading.CancellationToken)" />.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    Task StartedAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Triggered before <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" />.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the start process has been aborted.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    Task StoppingAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Triggered after <see cref="M:Microsoft.Extensions.Hosting.IHostedService.StopAsync(System.Threading.CancellationToken)" />.
    /// </summary>
    /// <param name="cancellationToken">Indicates that the stop process has been aborted.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that represents the asynchronous operation.</returns>
    Task StoppedAsync(CancellationToken cancellationToken);
  }
}
```

## How to ensure that your IHostedService is a single instance

### Interface Approach
The Interface approach might be simpler depending on your scenario. Here you'd add an interface that inherits from `IHostedService` and then add a method to that interface that you can call from your controller.

```csharp
    public interface IEmailSenderHostedService : IHostedService
    {
        Task SendEmailAsync(BaseEmailModel message);
        void Dispose();
    }
```
 
All we then need do is register this as a singleton and then use this in our controller.

```csharp
        services.AddSingleton<IEmailSenderHostedService, EmailSenderHostedService>();
```

ASP.NET will see that this has the correct interface decorated and will use this registration to run the `IHostedService`.


### Factory Method Approach
Another to ensure that your `IHostedService` is a single instance is to use the `AddSingleton` method to register your service then pass the `IHostedService` registration as a 'factory method'. This will ensure that only one instance of your service is created and used throughout the lifetime of the application.

* A *factory* method is just a fancy way of saying a method that creates an instance of an object.

```csharp
        services.AddSingleton<EmailSenderHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<EmailSenderHostedService>());
```

So as you see here I first register my `IHostedService` (or `IHostedLifeCycleService`) as a singleton and then I use the `AddHostedService` method to register the service as a factory method. This will ensure that only one instance of the service is created and used throughout the lifetime of the application.


## In Conclusion
As usual there's a couple of ways to skin a cat. The interface approach is probably the easiest way to ensure that your `IHostedService` is a single instance. But the factory method approach is also a good way to ensure that your service is a single instance. It's up to you which approach you take. I hope this article has helped you understand how to ensure that your `IHostedService` is a single instance.