using Htmx;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Mostlylucid.Blog.ViewServices;
using Mostlylucid.Controllers;
using Mostlylucid.Helpers;
using Mostlylucid.Mapper;
using Mostlylucid.Models.Blog;
using Mostlylucid.Models.Comments;
using Mostlylucid.Services;
using Mostlylucid.Services.Markdown;
using Mostlylucid.Shared.Config.Markdown;
using MarkdownBaseService = Mostlylucid.Services.Markdown.MarkdownBaseService;

namespace Mostlylucidblog.Controllers;

[Route("blog")]
public class BlogController(
    BaseControllerService baseControllerService,
    MarkdownRenderingService markdownRenderingService,
    MarkdownConfig markdownConfig,
    ILogger<BlogController> logger,
    CommentViewService? commentViewService = null) : BaseController(baseControllerService, logger)
{
    [ResponseCache(Duration = 300, VaryByHeader = "hx-request", VaryByQueryKeys = new[] { "page", "pageSize", nameof(startDate), nameof(endDate), nameof(language), nameof(orderBy), nameof(orderDir), "order", nameof(category) },
        Location = ResponseCacheLocation.Client)]
    [OutputCache(PolicyName = "BlogList", VaryByHeaderNames = new[] { "hx-request" })]
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 20, DateTime? startDate = null, DateTime? endDate = null,
        string language = MarkdownBaseService.EnglishLanguage, string orderBy = "date", string orderDir = "desc", string? order = null, string? category = null)
    {
        // Parse combined order param (e.g., "date_asc") if provided
        if (!string.IsNullOrEmpty(order) && order.Contains('_'))
        {
            var parts = order.Split('_');
            orderBy = parts[0];
            orderDir = parts[1];
        }

        var request = new BlogIndexRequest(page, pageSize, startDate, endDate, language, orderBy, orderDir, order, category);
        var result = await BlogViewService.GetIndexDataAsync(request);

        // Handle single post redirect
        if (result.ShouldRedirectToSinglePost && result.SinglePostSlug != null)
        {
            var postUrl = BlogUrlHelper.GetBlogUrl(result.SinglePostSlug, result.SinglePostLanguage);

            if (Request.IsHtmx())
            {
                Response.Headers["HX-Redirect"] = postUrl;
                return Content("");
            }
            return RedirectPermanent(postUrl);
        }

        result.Posts.LinkUrl = Url.Action("Index", "Blog", new { startDate, endDate, language, order = order ?? $"{orderBy}_{orderDir}", category });

        if (Request.IsHtmx()) return PartialView("_BlogSummaryList", result.Posts);
        return View("Index", result.Posts);
    }

    [HttpGet("categories")]
    [ResponseCache(Duration = 300, VaryByHeader = "hx-request", VaryByQueryKeys = new[] { nameof(language) }, Location = ResponseCacheLocation.Client)]
    [OutputCache(Duration = 1800, VaryByHeaderNames = new[] { "hx-request" }, VaryByQueryKeys = new[] { nameof(language) })]
    public async Task<IActionResult> Categories(string language = MarkdownBaseService.EnglishLanguage)
    {
        var categories = await BlogViewService.GetCategoriesWithCount(language);
        return Json(categories);
    }

    [HttpGet("calendar-days")]
    [ResponseCache(Duration = 300, VaryByHeader = "hx-request", VaryByQueryKeys = new[] { nameof(year), nameof(month), nameof(language) }, Location = ResponseCacheLocation.Client)]
    [OutputCache(Duration = 1800, VaryByHeaderNames = new[] { "hx-request" }, VaryByQueryKeys = new[] { nameof(year), nameof(month), nameof(language) })]
    public async Task<IActionResult> CalendarDays(int year, int month, string language = MarkdownBaseService.EnglishLanguage)
    {
        if (year < 2000 || month < 1 || month > 12) return BadRequest("Invalid year or month");

        var dates = await BlogViewService.GetCalendarDaysAsync(year, month, language);
        return Json(new { dates });
    }

    [HttpGet("date-range")]
    [ResponseCache(Duration = 3600, VaryByHeader = "hx-request", VaryByQueryKeys = new[] { nameof(language) }, Location = ResponseCacheLocation.Client)]
    [OutputCache(Duration = 7200, VaryByHeaderNames = new[] { "hx-request" }, VaryByQueryKeys = new[] { nameof(language) })]
    public async Task<IActionResult> DateRange(string language = MarkdownBaseService.EnglishLanguage)
    {
        var result = await BlogViewService.GetDateRangeAsync(language);
        return Json(new { minDate = result.MinDate, maxDate = result.MaxDate });
    }

    [Route("{slug}")]
    [HttpGet]
    [ResponseCache(Duration = 300, VaryByHeader = "hx-request",
        VaryByQueryKeys = new[] { nameof(slug), nameof(language) }, Location = ResponseCacheLocation.Client)]
    [OutputCache(PolicyName = "BlogPost", VaryByHeaderNames = new[] { "hx-request" },
        VaryByQueryKeys = new[] { nameof(slug), nameof(language) })]
    public async Task<IActionResult> Show(string slug, string language = MarkdownBaseService.EnglishLanguage)
    {
        slug = BlogViewService.NormalizeSlug(slug);

        // If non-English language is passed via query string, redirect to canonical path format
        if (!BlogUrlHelper.IsEnglish(language) && Request.Query.ContainsKey("language"))
        {
            var canonicalUrl = BlogUrlHelper.GetBlogUrl(slug, language);
            return RedirectPermanent(canonicalUrl);
        }

        var userInfo = await GetUserInfo();
        var post = await GetPostWithDetailsAsync(slug, language, userInfo);
        if (post == null) return NotFound();

        if (Request.IsHtmx()) return PartialView("_PostPartial", post);
        return View("Post", post);
    }

    [Route("category/{category}")]
    [HttpGet]
    [ResponseCache(Duration = 300, VaryByHeader = "hx-request",
        VaryByQueryKeys = new[] { nameof(category), nameof(page), nameof(pageSize) },
        Location = ResponseCacheLocation.Client)]
    [OutputCache(PolicyName = "BlogCategory", VaryByHeaderNames = new[] { "hx-request" })]
    public async Task<IActionResult> Category(string category, int page = 1, int pageSize = 10)
    {
        var userInfo = await GetUserInfo();
        var posts = await BlogViewService.GetPostsByCategory(category, page, pageSize);

        posts.Authenticated = userInfo.LoggedIn;
        posts.Name = userInfo.Name;
        posts.AvatarUrl = userInfo.AvatarUrl;
        posts.LinkUrl = Url.Action("Category", "Blog");

        ViewBag.Category = category;
        ViewBag.Title = category + " - Blog";

        if (Request.IsHtmx()) return PartialView("_BlogSummaryList", posts);
        return View("Index", posts);
    }

    /// <summary>
    /// Redirect old path format /blog/language/{slug}/{language} to canonical /blog/{language}/{slug}.
    /// Returns 301 Permanent Redirect for SEO.
    /// </summary>
    [Route("language/{slug}/{language}")]
    [HttpGet]
    public IActionResult Compat(string slug, string language)
    {
        var canonicalUrl = BlogUrlHelper.GetBlogUrl(slug, language);
        return RedirectPermanent(canonicalUrl);
    }

    /// <summary>
    /// Redirect legacy /posts/{id}.aspx URLs to /blog/{id}.
    /// Returns 301 Permanent Redirect for SEO, or HX-Redirect for HTMX requests.
    /// Returns 404 if the post doesn't exist.
    /// </summary>
    [Route("/posts/{id}.aspx")]
    [HttpGet]
    public async Task<IActionResult> LegacyPostsAspx(string id)
    {
        // Check if the post actually exists
        var post = await BlogViewService.GetPost(id);
        if (post == null)
        {
            return NotFound();
        }

        var canonicalUrl = $"/blog/{id}";

        if (Request.IsHtmx())
        {
            Response.Headers["HX-Redirect"] = canonicalUrl;
            return Content("");
        }

        return RedirectPermanent(canonicalUrl);
    }

    [Route("{language}/{slug}")]
    [HttpGet]
    [ResponseCache(Duration = 300, VaryByHeader = "hx-request",
        VaryByQueryKeys = new[] { nameof(slug), nameof(language) }, Location = ResponseCacheLocation.Client)]
    [OutputCache(PolicyName = "BlogPost", VaryByHeaderNames = new[] { "hx-request" },
        VaryByQueryKeys = new[] { nameof(slug), nameof(language) })]
    public async Task<IActionResult> Language(string slug, string language)
    {
        return await Show(slug, language);
    }

    /// <summary>
    /// View a draft post - NOT indexed, NOT cached, NOT listed anywhere.
    /// For author preview only.
    /// </summary>
    [Route("drafts/{slug}")]
    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Draft(string slug)
    {
        var post = await GetDraftAsync(slug);
        if (post == null) return NotFound($"Draft '{slug}' not found");

        ViewBag.IsDraft = true;

        if (Request.IsHtmx()) return PartialView("_PostPartial", post);
        return View("Post", post);
    }

    private async Task<BlogPostViewModel?> GetPostWithDetailsAsync(string slug, string language, LoginData userInfo)
    {
        var post = await BlogViewService.GetPost(slug, language);
        if (post == null) return null;

        // Populate user info
        post.Authenticated = userInfo.LoggedIn;
        post.Name = userInfo.Name;
        post.Email = userInfo.Email;
        post.AvatarUrl = userInfo.AvatarUrl;

        // Populate comments
        var commentViewList = new CommentViewList
        {
            PostId = int.TryParse(post.Id, out var postId) ? postId : 0,
            IsAdmin = userInfo.IsAdmin
        };

        // Markdown-backed posts have no database comment thread or numeric database ID.
        if (commentViewService != null && commentViewList.PostId > 0)
        {
            commentViewList.Comments = userInfo.IsAdmin
                ? await commentViewService.GetAllComments(commentViewList.PostId)
                : await commentViewService.GetApprovedComments(commentViewList.PostId);
        }

        commentViewList.Comments.ForEach(x => x.IsAdmin = userInfo.IsAdmin);
        post.Comments = commentViewList;

        // Determine previous (newer) and next (older) posts within the same language
        var allInLanguage = await BlogViewService.GetPostsForLanguage(language: post.Language);
        if (allInLanguage?.Any() == true)
        {
            var index = allInLanguage.FindIndex(p => p.Slug.Equals(post.Slug, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
            {
                // previous in time (newer) would be at index - 1 because list is ordered DESC (newest first)
                if (index - 1 >= 0)
                    post.PreviousPost = allInLanguage[index - 1];
                // next in time (older) would be at index + 1
                if (index + 1 < allInLanguage.Count)
                    post.NextPost = allInLanguage[index + 1];
            }
        }

        return post;
    }

    private async Task<BlogPostViewModel?> GetDraftAsync(string slug)
    {
        slug = BlogViewService.NormalizeSlug(slug);

        var draftsPath = Path.Combine(markdownConfig.MarkdownPath, "drafts");
        var filePath = Path.Combine(draftsPath, $"{slug}.md");

        if (!System.IO.File.Exists(filePath))
        {
            logger.LogWarning("Draft not found: {Slug}", slug);
            return null;
        }

        try
        {
            var markdown = await System.IO.File.ReadAllTextAsync(filePath);
            var fileInfo = new FileInfo(filePath);
            var blogPost = markdownRenderingService.GetPageFromMarkdown(markdown, fileInfo.LastWriteTimeUtc, filePath);

            if (blogPost == null)
            {
                logger.LogWarning("Invalid draft file (no valid title): {FilePath}", filePath);
                return null;
            }

            var post = blogPost.ToViewModel();
            post.Categories = blogPost.Categories.ToArray();
            return post;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reading draft {Slug}", slug);
            return null;
        }
    }
}
