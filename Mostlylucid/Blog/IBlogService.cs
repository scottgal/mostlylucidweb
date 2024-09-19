﻿using Mostlylucid.Models.Blog;

namespace Mostlylucid.Blog;

public interface IBlogService : IMarkdownFileBlogService
{
   Task<List<string>> GetCategories(bool noTracking = false);
   
   Task<List<BlogPostViewModel>> GetAllPosts();
    Task<List<BlogPostViewModel>> GetPosts(DateTime? startDate = null, string category = "");
    Task<PostListViewModel> GetPostsByCategory(string category, int page = 1, int pageSize = 10, string language = MarkdownBaseService.EnglishLanguage);
    Task<BlogPostViewModel?> GetPost(string slug, string language = "");
    Task<PostListViewModel> GetPagedPosts(int page = 1, int pageSize = 10, string language = MarkdownBaseService.EnglishLanguage);
    Task<List<PostListModel>> GetPostsForLanguage(DateTime? startDate = null, string category = "", string language = MarkdownBaseService.EnglishLanguage);
    Task<BlogPostViewModel> SavePost(BlogPostViewModel model);
    
    Task<bool> Delete(string slug, string language);
    Task<string> GetSlug(int id);

}

public interface IMarkdownFileBlogService
{
    Task<bool> EntryChanged(string slug, string language, string hash);
    Task<bool> EntryExists(string slug, string language);
    Task<BlogPostViewModel> SavePost(string slug, string language,  string markdown);
      
}

public interface IMarkdownBlogService
{
    Task<BlogPostViewModel> GetPage(string filePath);
    Task<List<BlogPostViewModel>> GetPages();
    
    
    Dictionary<string, List<String>> LanguageList();
    
}