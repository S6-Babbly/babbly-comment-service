using babbly_comment_service.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace babbly_comment_service.Data
{
    public class CommentRepository : CassandraRepository<Comment>
    {
        public CommentRepository(
            CassandraContext context,
            ILogger<CommentRepository> logger) 
            : base(context, logger, "comments")
        {
        }

        public async Task<IEnumerable<Comment>> GetByPostIdAsync(Guid postId)
        {
            return await QueryAsync("WHERE post_id = ? ALLOW FILTERING", postId);
        }

        public async Task<IEnumerable<Comment>> GetByUserIdAsync(string userId)
        {
            return await QueryAsync("WHERE user_id = ? ALLOW FILTERING", userId);
        }

        public async Task<IEnumerable<Comment>> GetLatestCommentsAsync(int limit = 20)
        {
            return await QueryAsync("ORDER BY created_at DESC LIMIT ?", limit);
        }
    }
} 