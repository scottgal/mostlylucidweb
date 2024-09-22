using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mostlylucid.Blog.ViewServices;
using Mostlylucid.Email.Models;
using Mostlylucid.Models.Comments;
using Mostlylucid.Services;
using Mostlylucid.Services.Interfaces;
using Mostlylucid.Shared;

namespace Mostlylucid.Controllers;

[Route("comment")]
public class CommentController(
    ICommentService commentService,
    IEmailSenderHostedService sender,
    CommentViewService commentViewService,
    BaseControllerService baseControllerService,

    ILogger<CommentController> logger)
    : BaseController(baseControllerService, logger)
{
    [HttpGet]
    [Route("get-commentform")]
    public async Task<IActionResult> GetCommentForm(int postId, int? parentCommentId)
    {
        var model = new CommentInputModel
        {
            BlogPostId = postId,
            ParentId = parentCommentId ?? 0
        };
        var user = await GetUserInfo();
        model.Authenticated = user.LoggedIn;
        model.Name = user.Name ?? string.Empty;
        model.Email = user.Email ?? string.Empty;
        model.AvatarUrl = user.AvatarUrl;
        model.ParentId = parentCommentId;


        return PartialView("_CommentForm", model);
    }


    [HttpPost]
    [Route("save-comment")]
    public async Task<IActionResult> Comment([Bind(Prefix = "")] CommentInputModel model)
    {
        if (!ModelState.IsValid) return PartialView("_CommentForm", model);
        var postId = model.BlogPostId;
        ;
        var name = model.Name ?? "Anonymous";
        var email = model.Email ?? "Anonymous";
        var comment = model.Content;

        var parentCommentId = model.ParentId;

        var htmlContent = await commentService.Add(postId, parentCommentId, name, comment);
        if (string.IsNullOrEmpty(htmlContent))
        {
            ModelState.AddModelError("Content", "Comment could not be saved");
            return PartialView("_CommentForm", model);
        }

        var slug = await BlogViewService.GetSlug(postId);
        var url = Url.Action("Show", "Blog", new { slug }, Request.Scheme);
        var commentModel = new CommentEmailModel
        {
            SenderEmail = email ?? "",
            Comment = htmlContent,
            PostUrl = url ?? string.Empty
        };
        await sender.SendEmailAsync(commentModel);
        model.Content = htmlContent;
        return PartialView("_CommentResponse", model);
    }

    [HttpGet]
    [Route("get-commentlist/{postId}")]
    public async Task<IActionResult> GetCommentList(int postId)
    {
        var user = await GetUserInfo();
        var commentViewList = new CommentViewList();
        commentViewList.PostId = postId;
        commentViewList.IsAdmin = user.IsAdmin;

        if (user.IsAdmin)
            commentViewList.Comments = await commentViewService.GetAllComments(postId);
        else
            commentViewList.Comments = await commentViewService.GetApprovedComments(postId);


        return PartialView("_ListComments", commentViewList);
    }

    [HttpPost]
    [Authorize]
    [Route("change-status")]
    public async Task<IActionResult> ChangeStatus(int commentId, CommentStatus status)
    {
        var user = await GetUserInfo();
        if (!user.IsAdmin) return Unauthorized();
        switch (status)
        {
            case CommentStatus.Approved:
                await commentService.Approve(commentId);
                break;
            case CommentStatus.Rejected:
                await commentService.Reject(commentId);
                break;
            case CommentStatus.Deleted:
                await commentService.Delete(commentId);
                break;
        }

        var comment = await commentService.Get(commentId);
        var postId = comment.PostId;
        return RedirectToAction("GetCommentList", new { postId });
    }

    [HttpPost]
    [Route("reply")]
    public async Task<IActionResult> Reply(CommentInputModel model)
    {
        var postId = model.BlogPostId;
        var name = model.Name ?? "Anonymous";
        var email = model.Email ?? "Anonymous";
        var htmlContent = await commentService.Add(postId, model.ParentId, name, model.Content);
        if (string.IsNullOrEmpty(htmlContent)) return BadRequest();
        var slug = await BlogViewService.GetSlug(postId);
        var url = Url.Action("Show", "Blog", new { slug }, Request.Scheme);
        var commentModel = new CommentEmailModel
        {
            SenderEmail = email ?? "",
            Comment = htmlContent,
            PostUrl = url ?? string.Empty
        };
        await sender.SendEmailAsync(commentModel);
        model.Content = htmlContent;
        return PartialView("_CommentResponse", model);
    }

    [HttpGet]
    [Route("list-comments")]
    public async Task<IActionResult> ListComments(string slug)
    {
        return View();
    }
}