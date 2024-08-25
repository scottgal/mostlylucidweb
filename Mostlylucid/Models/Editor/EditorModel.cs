using Mostlylucid.MarkdownTranslator.Models;
using Mostlylucid.Models.Blog;

namespace Mostlylucid.Models.Editor;

public class EditorModel : BaseViewModel
{
    public string Markdown { get; set; } = string.Empty;
    public BlogPostViewModel PostViewModel { get; set; } = new();
    
    public bool IsNew { get; set; }
    public List<string> Languages { get; set; } = new();
    
    public List<TranslateResultTask> TranslationTasks { get; set; } = new();
    
    public string UserSessionId { get; set; } = string.Empty;
}