using System;
using System.ComponentModel.DataAnnotations;

namespace babbly_comment_service.DTOs
{
    public class CreateCommentDto
    {
        [Required]
        public string Content { get; set; } = string.Empty;
        
        [Required]
        public Guid PostId { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
    }
} 