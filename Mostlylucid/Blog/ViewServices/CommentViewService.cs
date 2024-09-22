using Mostlylucid.Models.Comments;
using Mostlylucid.Services.Interfaces;
using Mostlylucid.Shared;

namespace Mostlylucid.Blog.ViewServices;

public class CommentViewService(ICommentService commentService)
{
    private async Task<List<CommentViewModel>> GetComments(int postId,int page=1, int pageSize=10, int? maxDepth = null, CommentStatus? status = CommentStatus.Approved)
    {
        var comments = await commentService.GetForPost(postId, page, pageSize:pageSize, maxDepth, status);
        return comments.Select(c => new CommentViewModel(c.Id, c.CreatedAt, c.Author, c.Status, c.HtmlContent ?? c.Content,c.PostId, c.ParentCommentId ?? 0, c.CurrentDepth)).ToList();
    }
    
    public async Task<List<CommentViewModel>> GetAllComments(int postId,int page=1, int pageSize=100, int? maxDepth = null)
    {
    
        return await GetComments(postId, page, pageSize, maxDepth, null);
    }
    public async Task<List<CommentViewModel>> GetPendingComments(int postId,int page=1, int pageSize=10, int? maxDepth = null)
    {
        var status = CommentStatus.Pending;
        return await GetComments(postId, page, pageSize, maxDepth, status);
    }
    
    public async Task<List<CommentViewModel>> GetApprovedComments(int postId,int page=1, int pageSize=10, int? maxDepth = null)
    {
        var status = CommentStatus.Approved;
        return await GetComments(postId, page, pageSize, maxDepth, status);
    }

    
    public async Task AddComment(int postId, int? parentCommentId, string author, string content)
    {
        await commentService.Add(postId, parentCommentId, author, content);
    }
}