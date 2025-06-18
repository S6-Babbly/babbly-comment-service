using System;
using System.ComponentModel.DataAnnotations;

namespace babbly_comment_service.DTOs
{
    public class CreateCommentDto
    {
        [Required]
        [StringLength(500, MinimumLength = 1, ErrorMessage = "Comment content must be between 1 and 500 characters")]
        public string Content { get; set; } = string.Empty;
        
        [Required]
        public Guid PostId { get; set; }
    }
} 