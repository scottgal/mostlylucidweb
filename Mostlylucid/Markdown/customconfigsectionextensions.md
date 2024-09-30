# Custom Config Section Extensions
<!--category-- C#, ASP.NET -->
<datetime class="hidden">2024-09-27T06:20</datetime>
# Introduction
It seems like everyone has a version of this code, I first came across this approach from [Filip W](https://www.strathweb.com/2016/09/strongly-typed-configuration-in-asp-net-core-without-ioptionst/) and recently my old colleague Phil Haack has [his version](https://haacked.com/archive/2024/07/18/better-config-sections/). 

Just for completion this is how I do it.

[TOC]

# Why?
The why is to make it easier to work with configuration. At the moment the premier way is using `IOptions<T>` and `IOptionsSnapshot<T>` but this is a bit of a pain to work with. This approach allows you to work with the configuration in a more natural way.
The limitations are that you can't use the `IOptions<T>` pattern, so you don't get the ability to reload the configuration without restarting the application. However honestly this is something I almost never want to do.

# The Code
In my version I use the recent static Interface members to specify that all instances of this class must declare a `Section` property. This is used to get the section from the configuration. 

```csharp
public interface IConfigSection {
    public static abstract string Section { get; }
}
```
So for each implementation you then specify what section this should be bound to:

```csharp
public class NewsletterConfig : IConfigSection
{
    public static string Section => "Newsletter";
    
    public string SchedulerServiceUrl { get; set; } = string.Empty;
    public string AppHostUrl { get; set; } = string.Empty;
}
```

In this case it's looking for a section in the configuration called `Newsletter`. 

```json
  "Newsletter": {
      "SchedulerServiceUrl" : "http://localhost:5000",
        "AppHostUrl" : "https://localhost:7240"
    
  }

```

We will then be able to bind this in the `Program.cs` file like so:

```csharp

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
services.ConfigurePOCO<NewsletterConfig>(config);

```

We can also get the value of the config in the `Program.cs` file like so:

```csharp
var newsletterConfig = services.ConfigurePOCO<NewsletterConfig>(config);

```

Or even for the `WebApplicationBuilder` like so:

```csharp
var newsletterConfig = builder.Configure<NewsletterConfig>();
```
Note: As the builder has access to the `ConfigurationManager` it doesn't need to have that passed in.

Meaning you now have options about how to bind config. 

Another advantage is that if you need to use these values later in your `Program.cs` file then the object is available to you.

# The Extension Method
To enable all this we have a fairly simple extension method that does the work of binding the configuration to the class. 

The code below allows the following:

```csharp
// These get the values and bind them to the class while adding these to Singleton Scope
var newsletterConfig = services.ConfigurePOCO<NewsletterConfig>(config);
var newsletterConfig = services.ConfigurePOCO<NewsletterConfig>(configuration.GetSection(NewsletterConfig.Section));
// Or for Builder...These obviously only work for ASP.NET Core applications, take them out if you are using this in a different context.
var newsletterConfig = builder.Configure<NewsletterConfig>();
var newsletterConfig = builder.Configure<NewsletterConfig>(NewsletterConfig.Section);
// These just return a dictionary, which can be useful to get all the values in a section
var newsletterConfig = builder.GetConfigSection<NewsletterConfig>();

```

This is all enabled by the following extension class.

You can see that the main impetus of this is using the static interface members to specify the section name. This is then used to get the section from the configuration.


```csharp
namespace Mostlylucid.Shared.Config;

public static class ConfigExtensions {
    public static TConfig ConfigurePOCO<TConfig>(this IServiceCollection services, IConfigurationSection configuration)
        where TConfig : class, new() {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        
        var config = new TConfig();
        configuration.Bind(config);
        services.AddSingleton(config);
        return config;
    }

    public static TConfig ConfigurePOCO<TConfig>(this IServiceCollection services, ConfigurationManager configuration)
        where TConfig : class, IConfigSection, new()
    {
        var sectionName = TConfig.Section;
        var section = configuration.GetSection(sectionName);
        return services.ConfigurePOCO<TConfig>(section);
    }
    
    public static TConfig Configure<TConfig>(this WebApplicationBuilder builder)
        where TConfig : class, IConfigSection, new() {
        var services = builder.Services;
        var configuration = builder.Configuration;
        var sectionName = TConfig.Section;
        return services.ConfigurePOCO<TConfig>(configuration.GetSection(sectionName));
    }
    

    public static TConfig GetConfig<TConfig>(this WebApplicationBuilder builder)
        where TConfig : class, IConfigSection, new() {
        var configuration = builder.Configuration;
        var sectionName = TConfig.Section;
        var section = configuration.GetSection(sectionName).Get<TConfig>();
        return section;
        
    }
    
    public static Dictionary<string, object> GetConfigSection(this IConfiguration configuration, string sectionName) {
        var section = configuration.GetSection(sectionName);
        var result = new Dictionary<string, object>();
        foreach (var child in section.GetChildren()) {
            var key = child.Key;
            var value = child.Value;
            result.Add(key, value);
        }
        
        return result;
    }
    
    public static Dictionary<string, object> GetConfigSection<TConfig>(this WebApplicationBuilder builder)
        where TConfig : class, IConfigSection, new() {
        var configuration = builder.Configuration;
        var sectionName = TConfig.Section;
        return configuration.GetConfigSection(sectionName);
    }
}
```

# In Use
To use these is pretty simple. In any class where you need this config you can simply inject it like so:

```csharp
public class NewsletterService(NewsletterConfig config {
 
}
```


# In Conclusion
Well that's it...pretty simple but it's a technique I use in all of my projects. It's a nice way to work with configuration and I think it's a bit more natural than the `IOptions<T>` pattern.