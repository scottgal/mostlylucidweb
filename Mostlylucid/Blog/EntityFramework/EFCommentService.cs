using Mostlylucid.EntityFramework.Models;

namespace Mostlylucid.Blog.EntityFramework;

public class EFCommentService(IMostlylucidDBContext context,  ILogger<EFCommentService> logger) : ICommentService
{
    
    
  public async Task<string> Add(int postId, int? parentCommentId, string author, string content)
  {
      using var activity = Log.Logger.StartActivity("AddComment {PostId}, {ParentCommentId},{Author}, {Content}", 
          new {postId, parentCommentId, author, content});
      await using var transaction = await context.Database.BeginTransactionAsync();
      try
      {
         
         var html = Markdig.Markdown.ToHtml(content);
         
          // Create the new comment
          var newComment = new CommentEntity()
          {
              HtmlContent = html,
              Content = content,
              CreatedAt = DateTime.UtcNow,
              PostId = postId,
              Author = author,
              Status = CommentStatus.Pending,
              ParentCommentId = parentCommentId
          };
            
          context.Comments.Add(newComment);
          await context.SaveChangesAsync();
          activity.AddProperty("CommentId", newComment.Id);
          logger.LogInformation("Saved comment to DB");// Save to generate the new comment's Id

          // Insert into CommentClosure table
          var commentClosures = new List<CommentClosure>();

          // Self-referencing closure entry
          commentClosures.Add(new CommentClosure
          {
              AncestorId =  newComment.Id,
              DescendantId = newComment.Id,
              Depth = 0
          });

          // If there is a parent comment, insert the ancestor relationships
          if (parentCommentId.HasValue)
          {
              // Fetch all ancestors of the parent comment
              var parentAncestors = await context.CommentClosures
                  .Where(cc => cc.DescendantId == parentCommentId.Value)
                  .ToListAsync();

              // Add ancestor relationships for the new comment
              foreach (var ancestor in parentAncestors)
              {
                  commentClosures.Add(new CommentClosure
                  {
                      AncestorId = ancestor.AncestorId,
                      DescendantId = newComment.Id,
                      Depth = ancestor.Depth + 1
                  });
              }

// Add a direct parent-child relationship only if it is not already added
              if (parentAncestors.All(a => a.AncestorId != parentCommentId.Value))
              {
                  commentClosures.Add(new CommentClosure
                  {
                      AncestorId = parentCommentId.Value,
                      DescendantId = newComment.Id,
                      Depth = 1
                  });
              }

          }

          context.CommentClosures.AddRange(commentClosures);
          await context.SaveChangesAsync();
          activity.Complete();
          logger.LogInformation("Saved comment closure to DB");

          // Commit transaction
          await transaction.CommitAsync();
          return html;
      }
      catch (Exception e)
      {
          // Rollback transaction in case of failure
          await transaction.RollbackAsync();
          logger.LogError(e, "Failed to save comment to DB");
      }

      return string.Empty;
  }

  public async Task<List<CommentEntity>> GetForPost(int blogPostId, int page = 1, int pageSize = 10,
      int? maxDepth = null, CommentStatus? status = null)
  {
      // Step 1: Query the top-level comments for the specified blog post
      var query = context.Comments
          .Where(c => c.PostId == blogPostId)
          .OrderByDescending(c => c.CreatedAt)
          .Skip((page - 1) * pageSize)
          .Take(pageSize);


// Step 2: Filter by status if provided
      if (status.HasValue)
      {
          query = query.Where(c => c.Status == status.Value);
      }
      return await query.ToListAsync();
// Step 3: Include related entities
      query = query
          .Include(c => c.ParentComment)
          .Include(c => c.Descendants);

      var comments = await query.ToListAsync();
// Filter out the current comment from its own descendants in memory
      foreach (var comment in comments)
      {
          comment.Descendants = comment.Descendants.Where(d => d.DescendantId != comment.Id).ToList();
      }



      List<CommentClosure> descendants = new();
      foreach (var comment in comments)
      {
         descendants = comment.Descendants.ToList();
         foreach(var descendant in descendants)
         {
             CommentEntity commentDescendant = descendant.Descendant;
             while (commentDescendant != null)
             {
                var currentComment = comments.FirstOrDefault(x => x.Id == commentDescendant.Id);
                currentComment.CurrentDepth = descendant.Depth;
                commentDescendant = descendant.Descendant;
             }
         }
      }

      return comments;
  }

  public async Task<CommentEntity?> Get(int commentId)
  {
      return await context.Comments.FindAsync(commentId);
  }
  
  public async Task<List<CommentEntity>> GetDescendants(int commentId, int maxDepth = 0)
  {
      var descendants = await context.CommentClosures
          .Where(cc => cc.AncestorId == commentId && cc.Depth > 0 && (maxDepth == 0 || cc.Depth <= maxDepth))
          .Select(cc => cc.Descendant)
          .ToListAsync();

      return descendants;
  }
  
  public async Task<List<CommentEntity>> GetAncestors(int commentId)
  {
      var ancestors = await context.CommentClosures
          .Where(cc => cc.DescendantId == commentId && cc.Depth > 0)
          .Select(cc => cc.Ancestor)
          .ToListAsync();

      return ancestors;
  }
  
  public async Task Delete(int commentId)
  {
      await using var transaction = await context.Database.BeginTransactionAsync();
      try
      {
          // Find all descendants of the comment to be deleted
          var descendants = await context.CommentClosures
              .Where(cc => cc.AncestorId == commentId)
              .Select(cc => cc.DescendantId)
              .Distinct()
              .ToListAsync();

          // Delete all closure records for the descendants
          context.CommentClosures.RemoveRange(
              context.CommentClosures.Where(cc => descendants.Contains(cc.DescendantId))
          );

          // Delete the comments themselves
          context.Comments.RemoveRange(
              context.Comments.Where(c => descendants.Contains(c.Id))
          );

          await context.SaveChangesAsync();
          await transaction.CommitAsync();
      }
      catch (Exception)
      {
          await transaction.RollbackAsync();
          throw;
      }
  }
  
  private async Task ChangeStatus(int commentId, CommentStatus newStatus)
  {
      var comment = await context.Comments.FindAsync(commentId);
      if (comment == null)
      {
          throw new ArgumentException("Comment not found");
      }

      comment.Status = newStatus;
      await context.SaveChangesAsync();
  }
 


    public async Task Reject(int commentId)=> await ChangeStatus(commentId, CommentStatus.Rejected);
    
    public async Task Approve(int commentId) => await ChangeStatus(commentId, CommentStatus.Approved);

}