using System;
using System.ComponentModel.DataAnnotations;

namespace babbly_comment_service.Models
{
    public class Comment
    {
        public Guid Id { get; set; }
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        public Guid PostId { get; set; }
        
        public string UserId { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
    }
} 