using System.ComponentModel.DataAnnotations;
using Mostlylucid.Shared;

namespace Mostlylucid.Models.Comments;

    public  class CommentInputModel() : BaseViewModel
    {
        
        public int Id { get; set; }
        
        public DateTime Date { get; init; }= DateTime.Now;
        [Required(ErrorMessage = "Please enter your name")]
        public new string? Name { get; set; }
        
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public new string? Email { get; set; } = "";
        public CommentStatus Status { get; set; } = CommentStatus.Pending;
        
        [Required(ErrorMessage = "Comment is required")]
        public string? Content { get; set; } 
        [Required]
        public int  BlogPostId { get; set; }
        public int? ParentId { get; set; } 

    }