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
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetCommentsByPost(Guid postId)
        {
            try
            {
                _logger.LogInformation("Getting comments for post with ID: {PostId}", postId);
                var comments = await _mapper.FetchAsync<Comment>("WHERE post_id = ?", postId);
                return Ok(comments.Select(c => MapCommentToDto(c)));
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
                var comment = new Comment
                {
                    Id = Guid.NewGuid(),
                    Content = createCommentDto.Content,
                    PostId = createCommentDto.PostId,
                    UserId = createCommentDto.UserId,
                    CreatedAt = DateTime.UtcNow
                };

                await _mapper.InsertAsync(comment);
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
                var existingComment = await _mapper.SingleOrDefaultAsync<Comment>("WHERE id = ?", id);
                if (existingComment == null)
                {
                    return NotFound();
                }

                existingComment.Content = updateCommentDto.Content;
                await _mapper.UpdateAsync(existingComment);
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
                var comment = await _mapper.SingleOrDefaultAsync<Comment>("WHERE id = ?", id);
                if (comment == null)
                {
                    return NotFound();
                }

                await _mapper.DeleteAsync<Comment>("WHERE id = ?", id);
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
    }
} 