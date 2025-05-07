using Cassandra;
using Microsoft.AspNetCore.Mvc;
using babbly_comment_service.Data;
using System;

namespace babbly_comment_service.Controllers
{
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        private readonly Cassandra.ISession _session;
        private readonly ILogger<HealthController> _logger;

        public HealthController(CassandraContext context, ILogger<HealthController> logger)
        {
            _session = context.Session;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                _logger.LogInformation("Health check requested");
                // Check Cassandra connection
                _session.Execute("SELECT release_version FROM system.local");
                return Ok(new { status = "Healthy", service = "babbly-comment-service" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(500, new { status = "Unhealthy", service = "babbly-comment-service", message = ex.Message });
            }
        }
    }
} 