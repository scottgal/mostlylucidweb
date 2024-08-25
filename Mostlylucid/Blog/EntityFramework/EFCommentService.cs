using Microsoft.EntityFrameworkCore;
using Mostlylucid.Blog.Models;
using Mostlylucid.Controllers;
using Mostlylucid.EntityFramework;
using Mostlylucid.EntityFramework.Models;

namespace Mostlylucid.Blog.EntityFramework;

public class EFCommentService(IMostlylucidDBContext context,  ILogger<EFCommentService> logger)
{
    public async Task AddComment(string slug, BaseController.LoginData userInformation, string markdown)
    {
        var blogPost = await context.BlogPosts.FirstOrDefaultAsync(x => x.Slug == slug);
        if (blogPost == null)
        {
            return;
        }
        var comment = new CommentEntity() {BlogPostId = blogPost.Id, Content = markdown, Date = DateTimeOffset.Now, Name = userInformation.Name, Email = userInformation.Email, Avatar = userInformation.AvatarUrl, Slug = slug};
   
        await context.Comments.AddAsync(comment);
        await context.SaveChangesAsync();
    }

    public async Task<List<Comment>> GetComments(string slug, bool moderated = true)
    {
        var comments =await  context.Comments.Where(x => x.Slug == slug && x.Moderated == moderated).ToListAsync();
        return comments.Select(x=>new Comment(x.Date.DateTime,x.Name,x.Avatar,x.Content)).OrderBy(x=>x.Date).ToList();
        
    }
    
}