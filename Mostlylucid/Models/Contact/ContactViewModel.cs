using System.ComponentModel.DataAnnotations;

namespace Mostlylucid.Models.Contact;

public class ContactViewModel : BaseViewModel
{
    [EmailAddress(ErrorMessage = "Invalid Email Address")]
    public string? Email { get; set; }
    
    [Required(ErrorMessage = "Comment is required")]
    public string? Comment { get; set; }
    
}