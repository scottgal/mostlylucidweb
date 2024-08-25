# (Simple) Unit Testing The Blog Part 1 - DbContext

<datetime class="hidden">2024-08-25T23:00</datetime>
<!--category-- xUnit, Moq, Unit Testing -->

## Introduction
In this post I'll be starting adding Unit Testing for this site. This won't be a full tutorial on Unit Testing, but rather a series of posts on how I'm adding Unit Testing to this site.

[TOC]

## Why Unit Test?
Unit Testing is a way of testing individual components of your code in isolation. This is useful for a number of reasons:
1. It isolates each component of your code making it simple to see any issues in specific areas.
2. It's a way of documenting your code. If you have a test that fails, you know that something has changed in that area of your code.

### What other types of testing are there?
There are a number of other types of testing that you can do. Here are a few:
1. Integration Testing - Testing how different components of your code work together. In ASP.NET we could use tools like [Verify](https://github.com/VerifyTests/Verify) to test the output of endpoints and compare them to expected results. We'll add this in future.
2. End-to-End Testing - Testing the whole application from the user's perspective. This could be done with tools like [Selenium](https://www.selenium.dev/). 
3. Performance Testing - Testing how your application performs under load. This could be done with tools like [Apache JMeter](https://jmeter.apache.org/), [PostMan](https://www.postman.com/). My preferred option however is a tool called [k6](https://k6.io/).
4. Security Testing - Testing how secure your application is. This could be done with tools like [OWASP ZAP](https://www.zaproxy.org/), [Burp Suite](https://portswigger.net/burp), [Nessus](https://www.tenable.com/products/nessus).
5. End User Testing - Testing how your application works for the end user. This could be done with tools like [UserTesting](https://www.usertesting.com/), [UserZoom](https://www.userzoom.com/), [Userlytics](https://www.userlytics.com/).

## Setting up the Test Project
I'm going to be using xUnit for my tests. This is used by default in ASP.NET Core projects. I'm also going to be using Moq to mock the DbContext along with 
- MoqQueryable - This has useful extensions for mocking IQueryable objects.
- Moq.EntityFrameworkCore - This has useful extensions for mocking DbContext objects.

## Mocking the DbContext
In preparation for this I added an Interface for my DbContext. This is so that I can mock the DbContext in my tests. Here is the interface:
```csharp
namespace Mostlylucid.EntityFramework;

public interface IMostlylucidDBContext
{
    public DbSet<CommentEntity> Comments { get; set; }
    public DbSet<BlogPostEntity> BlogPosts { get; set; }
    public DbSet<CategoryEntity> Categories { get; set; }

    public DbSet<LanguageEntity> Languages { get; set; }
    
    public DatabaseFacade Database { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

}

```
It's pretty simple, just exposing our DBSets and the SaveChangesAsync method.

I *don't* use a repository pattern in my code. This is because Entity Framework Core is already a repository pattern. I use a service layer to interact with the DbContext. This is because I don't want to abstract away the power of Entity Framework Core.

We then add a new class to our `Mostlylucid.Test` project with an extension method to set up our querying:

```csharp
public static class MockDbSetExtensions
{
    public static Mock<DbSet<T>> CreateDbSetMock<T>(this IEnumerable<T> sourceList) where T : class
    {
        // Use the MockQueryable.Moq extension method to create the mock
        return sourceList.AsQueryable().BuildMockDbSet();
    }

    // SetupDbSet remains the same, just uses the updated CreateDbSetMock
    public static void SetupDbSet<T>(this Mock<IMostlylucidDBContext> mockContext, IEnumerable<T> entities,
        Expression<Func<IMostlylucidDBContext, DbSet<T>>> dbSetProperty) where T : class
    {
        var dbSetMock = entities.CreateDbSetMock();
        mockContext.Setup(dbSetProperty).Returns(dbSetMock.Object);
    }
}
```
You'll see that this is using the `MockQueryable.Moq` extension method to create the mock. Which then sets up our IQueryable objects and IAsyncQueryable objects.

### Setting up the Test
A core tenet of Unit Testing is that each test should be a 'unit' of work and not depend on the result of any other test (this is why we mock our DbContext).

In our new `BlogServiceFetchTests` class we set up our test context in the constructor:

```csharp
  public BlogServiceFetchTests()
    {
        // 1. Setup ServiceCollection for DI
        var services = new ServiceCollection();
        // 2. Create a mock of IMostlylucidDbContext
        _dbContextMock = new Mock<IMostlylucidDBContext>();
        // 3. Register the mock of IMostlylucidDbContext into the ServiceCollection
        services.AddSingleton(_dbContextMock.Object);
        // Optionally register other services
        services.AddScoped<IBlogService, EFBlogService>(); // Example service that depends on IMostlylucidDbContext
        services.AddLogging(configure => configure.AddConsole());
        services.AddScoped<MarkdownRenderingService>();
        // 4. Build the service provider
        _serviceProvider = services.BuildServiceProvider();
    }
```
I've commented this pretty heavily so you can see what's going on. We're setting up a `ServiceCollection` which is a collection of services that we can use in our tests. We then create a mock of our `IMostlylucidDBContext` and register it in the `ServiceCollection`. We then register any other services that we need for our tests. Finally we build the `ServiceProvider` which we can use to get our services from.

## Writing the Test
I started by adding a single test class, the aforementioned `BlogServiceFetchTests` class. This is a test class for the Post getting methods of my `EFBlogService` class. 

Each test uses a common `SetupBlogService` method to get a new populated `EFBlogService` object. This is so that we can test the service in isolation.

```csharp
    private IBlogService SetupBlogService(List<BlogPostEntity>? blogPosts = null)
    {
        blogPosts ??= BlogEntityExtensions.GetBlogPostEntities(5);

        // Setup the DbSet for BlogPosts in the mock DbContext
        _dbContextMock.SetupDbSet(blogPosts, x => x.BlogPosts);

        // Resolve the IBlogService from the service provider
        return _serviceProvider.GetRequiredService<IBlogService>();
    }

```

### BlogEntityExtensions
This is a simple extension class which gives us a number of pupulated `BlogPostEntity` objects. This is so that we can test our service with a number of different objects.

```csharp
 public static List<BlogPostEntity> GetBlogPostEntities(int count, string? langName = "")
    {
        var langs = LanguageExtensions.GetLanguageEntities();

        if (!string.IsNullOrEmpty(langName)) langs = new List<LanguageEntity> { langs.First(x => x.Name == langName) };

        var langCount = langs.Count;
        var categories = CategoryEntityExtensions.GetCategoryEntities();
        var entities = new List<BlogPostEntity>();

        var enLang = langs.First();
        var cat1 = categories.First();

        // Add a root post to the list to test the category filter.
        var rootPost = new BlogPostEntity
        {
            Id = 0,
            Title = "Root Post",
            Slug = "root-post",
            HtmlContent = "<p>Html Content</p>",
            PlainTextContent = "PlainTextContent",
            Markdown = "# Markdown",
            PublishedDate = DateTime.ParseExact("2025-01-01T07:01", "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
            UpdatedDate = DateTime.ParseExact("2025-01-01T07:01", "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
            LanguageEntity = enLang,
            Categories = new List<CategoryEntity> { cat1 }
        };
        entities.Add(rootPost);
        for (var i = 1; i < count; i++)
        {
            var langIndex = (i - 1) % langCount;
            var language = langs[langIndex];
            var postCategories = categories.Take(i - 1 % categories.Count).ToList();
            var dayDate = (i + 1 % 30 + 1).ToString("00");
            entities.Add(new BlogPostEntity
            {
                Id = i,
                Title = $"Title {i}",
                Slug = $"slug-{i}",
                HtmlContent = $"<p>Html Content {i}</p>",
                PlainTextContent = $"PlainTextContent {i}",
                Markdown = $"# Markdown {i}",
                PublishedDate = DateTime.ParseExact($"2025-01-{dayDate}T07:01", "yyyy-MM-ddTHH:mm",
                    CultureInfo.InvariantCulture),
                UpdatedDate = DateTime.ParseExact($"2025-01-{dayDate}T07:01", "yyyy-MM-ddTHH:mm",
                    CultureInfo.InvariantCulture),
                LanguageEntity = new LanguageEntity
                {
                    Id = language.Id,
                    Name = language.Name
                },
                Categories = postCategories
            });
        }

        return entities;
    }
```
You can see that all this does is return a set number of blog posts with Languages and Categories. However we always add a 'root' object allowing us to be able to rely on a known object in our tests.

### The Tests
Each test is designed to test one aspect of the posts results.

For example in the two below we simply test that we can get all the posts and that we can get posts by language.

```csharp
    [Fact]
    public async Task TestBlogService_GetBlogsByLanguage_ReturnsBlogs()
    {
        var blogService = SetupBlogService();

        // Act
        var result = await blogService.GetPostsForLanguage(language: "es");

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task TestBlogService_GetAllBlogs_ReturnsBlogs()
    {
        var blogs = BlogEntityExtensions.GetBlogPostEntities(2);
        var blogService = SetupBlogService(blogs);
        // Act
        var result = await blogService.GetAllPosts();

        // Assert
        Assert.Equal(2, result.Count());
    }
```

#### Test For Failure
An important concept in Unit testing is 'testing failure' where you establish that your code fails in the way you expect it to.

In the tests below we first test that our paging code works as expected. We then test that if we ask for more pages than we have, we get an empty result (and not an error).

```csharp
    [Fact]
    public async Task TestBlogServicePagination_GetBlogsByCategory_ReturnsBlogs()
    {
        var blogPosts = BlogEntityExtensions.GetBlogPostEntities(10, "en");
        var blogService = SetupBlogService(blogPosts);

        // Act
        var result = await blogService.GetPagedPosts(2, 5);

        // Assert
        Assert.Equal(5, result.Posts.Count);
    }

    [Fact]
    public async Task TestBlogServicePagination_GetBlogsByCategory_FailsBlogs()
    {
        var blogPosts = BlogEntityExtensions.GetBlogPostEntities(10, "en");
        var blogService = SetupBlogService(blogPosts);

        // Act
        var result = await blogService.GetPagedPosts(10, 5);

        // Assert
        Assert.Empty(result.Posts);
    }
```

## In Conclusion
This is a simple start to our Unit Testing. In the next post we'll add testing for more services and endpoints. We'll also look at how we can test our endpoints using Integration Testing.