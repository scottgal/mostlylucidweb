﻿using Mostlylucid.Models.Blog;

namespace Mostlylucid.Blog;

public interface IBlogService
{
   Task<List<string>> GetCategories();
    Task<List<BlogPostViewModel>> GetPosts(DateTime? startDate = null, string category = "");
    Task<PostListViewModel> GetPostsByCategory(string category, int page = 1, int pageSize = 10, string language = MarkdownBaseService.EnglishLanguage);
    Task<BlogPostViewModel?> GetPost(string slug, string language = "");
    Task<PostListViewModel> GetPagedPosts(int page = 1, int pageSize = 10, string language = MarkdownBaseService.EnglishLanguage);
    
    Task<List<PostListModel>> GetPostsForLanguage(DateTime? startDate = null, string category = "", string language = MarkdownBaseService.EnglishLanguage);
}

public interface IMarkdownBlogService
{
    BlogPostViewModel GetPageFromMarkdown(string markdownLines, DateTime publishedDate, string filePath);
    Task<List<BlogPostViewModel>> GetPages();
    Task<BlogPostViewModel?> GetPageFromSlug(string slug, string language = "");
    
    Dictionary<string, List<String>> LanguageList();
    
}