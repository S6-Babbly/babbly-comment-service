using babbly_comment_service.Data;
using babbly_comment_service.DTOs;
using babbly_comment_service.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra.Mapping;
using Cassandra;

namespace babbly_comment_service.Controllers
{
    [ApiController]
    [Route("api/comments")]
    public class CommentController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly Cassandra.ISession _session;
        private readonly ILogger<CommentController> _logger;

        public CommentController(CassandraContext context, ILogger<CommentController> logger)
        {
            _mapper = context.Mapper;
            _session = context.Session;
            _logger = logger;
        }

        // GET: api/Comment
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Comment>>> GetComments()
        {
            try
            {
                var comments = await _mapper.FetchAsync<Comment>("SELECT * FROM comments");
                return Ok(comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/Comment/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Comment>> GetComment(Guid id)
        {
            try
            {
                var comment = await _mapper.SingleOrDefaultAsync<Comment>("WHERE id = ?", id);
                if (comment == null)
                {
                    return NotFound();
                }
                return Ok(comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comment {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/Comment/post/{postId}
        [HttpGet("post/{postId}")]
        public async Task<ActionResult> GetCommentsByPost(Guid postId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                _logger.LogInformation("Getting comments for post with ID: {PostId}, page: {Page}, pageSize: {PageSize}", postId, page, pageSize);
                
                // Get all comments for the post
                var allComments = await _mapper.FetchAsync<Comment>("WHERE post_id = ?", postId);
                
                // Sort by creation date descending (newest first) in C# since Cassandra table doesn't have clustering columns
                var commentList = allComments.OrderByDescending(c => c.CreatedAt).ToList();
                
                // Calculate pagination
                var total = commentList.Count;
                var skip = (page - 1) * pageSize;
                var paginatedComments = commentList.Skip(skip).Take(pageSize).ToList();
                
                // Map to DTOs
                var commentDtos = paginatedComments.Select(c => MapCommentToDto(c)).ToList();
                
                // Return paginated response format expected by frontend
                var response = new
                {
                    items = commentDtos,
                    total = total,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)total / pageSize)
                };
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments for post {PostId}", postId);
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/Comment/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetCommentsByUser(string userId)
        {
            try
            {
                _logger.LogInformation("Getting comments for user with ID: {UserId}", userId);
                var comments = await _mapper.FetchAsync<Comment>("WHERE user_id = ?", userId);
                return Ok(comments.Select(c => MapCommentToDto(c)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/Comment
        [HttpPost]
        public async Task<ActionResult<Comment>> CreateComment(CreateCommentDto createCommentDto)
        {
            try
            {
                // Get authenticated user ID from JWT headers (forwarded by API Gateway)
                var userId = Request.Headers["X-User-Id"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized(new { error = "Authentication required. User ID not found in token." });
                }

                // Validate content
                if (string.IsNullOrWhiteSpace(createCommentDto.Content))
                {
                    return BadRequest(new { error = "Comment content cannot be empty" });
                }

                if (createCommentDto.Content.Length > 500)
                {
                    return BadRequest(new { error = "Comment content cannot exceed 500 characters" });
                }

                var comment = new Comment
                {
                    Id = Guid.NewGuid(),
                    Content = createCommentDto.Content.Trim(),
                    PostId = createCommentDto.PostId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                await _mapper.InsertAsync(comment);
                _logger.LogInformation("User {UserId} created comment {CommentId} on post {PostId}", userId, comment.Id, createCommentDto.PostId);
                return CreatedAtAction(nameof(GetComment), new { id = comment.Id }, comment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comment");
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT: api/Comment/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComment(Guid id, UpdateCommentDto updateCommentDto)
        {
            try
            {
                // Get authenticated user ID from JWT headers
                var userId = Request.Headers["X-User-Id"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized(new { error = "Authentication required. User ID not found in token." });
                }

                var existingComment = await _mapper.SingleOrDefaultAsync<Comment>("WHERE id = ?", id);
                if (existingComment == null)
                {
                    return NotFound(new { error = "Comment not found" });
                }

                // Ensure user can only edit their own comments
                if (existingComment.UserId != userId)
                {
                    return Forbid("You can only edit your own comments");
                }

                // Validate content
                if (string.IsNullOrWhiteSpace(updateCommentDto.Content))
                {
                    return BadRequest(new { error = "Comment content cannot be empty" });
                }

                if (updateCommentDto.Content.Length > 500)
                {
                    return BadRequest(new { error = "Comment content cannot exceed 500 characters" });
                }

                existingComment.Content = updateCommentDto.Content.Trim();
                await _mapper.UpdateAsync(existingComment);
                
                _logger.LogInformation("User {UserId} updated comment {CommentId}", userId, id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating comment {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // DELETE: api/Comment/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(Guid id)
        {
            try
            {
                // Get authenticated user ID from JWT headers
                var userId = Request.Headers["X-User-Id"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized(new { error = "Authentication required. User ID not found in token." });
                }

                var comment = await _mapper.SingleOrDefaultAsync<Comment>("WHERE id = ?", id);
                if (comment == null)
                {
                    return NotFound(new { error = "Comment not found" });
                }

                // Ensure user can only delete their own comments
                if (comment.UserId != userId)
                {
                    return Forbid("You can only delete your own comments");
                }

                await _mapper.DeleteAsync<Comment>("WHERE id = ?", id);
                
                _logger.LogInformation("User {UserId} deleted comment {CommentId}", userId, id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // Helper method to map Comment entity to CommentDto
        private static CommentDto MapCommentToDto(Comment comment)
        {
            return new CommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                PostId = comment.PostId,
                UserId = comment.UserId,
                CreatedAt = comment.CreatedAt,
                TimeAgo = FormatTimeAgo(comment.CreatedAt)
            };
        }

        // Helper method to format time ago string
        private static string FormatTimeAgo(DateTime dateTime)
        {
            var span = DateTime.UtcNow - dateTime;

            if (span.TotalDays > 365)
            {
                var years = (int)(span.TotalDays / 365);
                return $"{years}y ago";
            }
            if (span.TotalDays > 30)
            {
                var months = (int)(span.TotalDays / 30);
                return $"{months}mo ago";
            }
            if (span.TotalDays > 1)
            {
                return $"{(int)span.TotalDays}d ago";
            }
            if (span.TotalHours > 1)
            {
                return $"{(int)span.TotalHours}h ago";
            }
            if (span.TotalMinutes > 1)
            {
                return $"{(int)span.TotalMinutes}m ago";
            }

            return "Just now";
        }

        // DELETE: api/Comment/user/{userId}
        [HttpDelete("user/{userId}")]
        public async Task<IActionResult> DeleteAllCommentsByUser(string userId)
        {
            try
            {
                _logger.LogInformation("Deleting all comments for user {UserId}", userId);
                
                // Get all comments by the user
                var userComments = await _mapper.FetchAsync<Comment>("WHERE user_id = ?", userId);
                var commentsList = userComments.ToList();
                
                _logger.LogInformation("Found {Count} comments to delete for user {UserId}", commentsList.Count, userId);
                
                // Delete each comment
                foreach (var comment in commentsList)
                {
                    await _mapper.DeleteAsync<Comment>("WHERE id = ?", comment.Id);
                }
                
                _logger.LogInformation("Successfully deleted {Count} comments for user {UserId}", commentsList.Count, userId);
                return Ok(new { message = $"Deleted {commentsList.Count} comments for user {userId}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comments for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }
    }
} 