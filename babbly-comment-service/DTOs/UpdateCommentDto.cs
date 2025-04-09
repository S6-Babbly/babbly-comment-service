using System.ComponentModel.DataAnnotations;

namespace babbly_comment_service.DTOs
{
    public class UpdateCommentDto
    {
        [Required]
        public string Content { get; set; } = string.Empty;
    }
} 