using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Mostlylucid.Config.Markdown;
using Mostlylucid.Controllers;

namespace Mostlylucid.Services.Markdown;

public class CommentService(MarkdownConfig markdownConfig) : BaseService
{
    private static readonly Regex CommentNameRegex = new("<!--name\\s+(.*?)\\s+-->\n",
        RegexOptions.Compiled | RegexOptions.Singleline);
    
    private static readonly Regex CommentAvaterRegex = new("<!--avatar\\s+(.*?)\\s+-->\n",
        RegexOptions.Compiled | RegexOptions.Singleline);
    
    public async Task AddComment(string slug,BaseController.LoginData userInformation, string markdown)
    {
        markdown = $"<!--name {userInformation.Name} --><!--avatar {userInformation.AvatarUrl} -->{ markdown}";
        var path = Path.Combine(markdownConfig.MarkdownNotModeratedCommentsPath,$"{DateTime.Now.ToFileTimeUtc()}_{userInformation.Identifier}_{slug}.md");
        var comment =markdown;
        await File.WriteAllTextAsync(path, comment);
    }

    public string ProcessComment(string commentMarkdown)
    {
        var html = Markdig.Markdown.ToHtml(commentMarkdown);
        return html;
    }
 
    
    public  record Comment(DateTime Date, string Name, string Avatar, string Content);
    
    public List<Comment> GetComments(string slug)
    {
        var comments = new List<Comment>();
        var path = Directory.GetFiles(markdownConfig.MarkdownCommentsPath, $"*{slug}.md");
        foreach (var file in path)
        {
            var dateString = file.Substring(0, file.IndexOf("_", StringComparison.Ordinal));
            var date =  DateTime.FromFileTimeUtc(Int64.Parse( dateString));
            
            var id = file.Substring(file.IndexOf("_", StringComparison.Ordinal) + 1, file.LastIndexOf("_", StringComparison.Ordinal) - file.IndexOf("_", StringComparison.Ordinal) - 1);
       
            var comment = File.ReadAllText(file);
            var name = CommentNameRegex.Match(comment).Groups[1].Value;
            var avatar = CommentAvaterRegex.Match(comment).Groups[1].Value;
            var html = Markdig.Markdown.ToHtml(comment, _pipeline);
            var commentModel = new Comment(date, name, avatar, html);
            comments.Add(commentModel);
        }

        comments =  comments.OrderByDescending(x=>x.Date).ToList();
        return comments;

    }
}