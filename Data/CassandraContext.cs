using Cassandra;
using Cassandra.Mapping;
using Microsoft.Extensions.Logging;
using babbly_comment_service.Models;
using System;
using System.Threading;

namespace babbly_comment_service.Data
{
    public class CassandraContext : IDisposable
    {
        private readonly Cluster _cluster;
        private readonly Cassandra.ISession _session;
        private readonly IMapper _mapper;
        private readonly ILogger<CassandraContext> _logger;
        private readonly IConfiguration _configuration;
        private const int MAX_RETRIES = 5;
        private const int RETRY_DELAY_MS = 5000;
        private bool _disposed = false;

        public CassandraContext(IConfiguration configuration, ILogger<CassandraContext> logger)
        {
            _logger = logger;
            _configuration = configuration;
            
            try
            {
                var hosts = configuration["CassandraHosts"]?.Split(',') 
                    ?? throw new ArgumentNullException("CassandraHosts configuration missing");
                var keyspace = configuration["CassandraKeyspace"] 
                    ?? throw new ArgumentNullException("CassandraKeyspace configuration missing");
                var username = configuration["CassandraUsername"];
                var password = configuration["CassandraPassword"];

                var clusterBuilder = Cluster.Builder()
                    .AddContactPoints(hosts)
                    .WithReconnectionPolicy(new ExponentialReconnectionPolicy(1000, 60000))
                    .WithQueryOptions(new QueryOptions().SetConsistencyLevel(ConsistencyLevel.LocalQuorum));

                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    clusterBuilder.WithCredentials(username, password);
                }

                _cluster = clusterBuilder.Build();
                
                // First connect without keyspace to check/create it
                var tempSession = _cluster.Connect();
                CheckAndCreateKeyspaceIfNeeded(tempSession, keyspace);
                tempSession.Dispose();

                // Now connect with retry logic
                _session = ConnectWithRetry(keyspace);
                
                // Configure mappings
                MappingConfiguration mappingConfig = new MappingConfiguration();
                mappingConfig.Define(
                    new Map<Comment>()
                        .TableName("comments")
                        .PartitionKey(c => c.Id)
                        .Column(c => c.Id, cm => cm.WithName("id").WithDbType<Guid>())
                        .Column(c => c.Content, cm => cm.WithName("content"))
                        .Column(c => c.PostId, cm => cm.WithName("post_id").WithDbType<Guid>())
                        .Column(c => c.UserId, cm => cm.WithName("user_id"))
                        .Column(c => c.CreatedAt, cm => cm.WithName("created_at"))
                );

                _mapper = new Mapper(_session, mappingConfig);
                _logger.LogInformation("Successfully connected to Cassandra cluster");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Cassandra connection");
                throw;
            }
        }

        private void CheckAndCreateKeyspaceIfNeeded(Cassandra.ISession session, string keyspace)
        {
            try
            {
                var keyspaceMetadata = session.Cluster.Metadata.GetKeyspace(keyspace);
                if (keyspaceMetadata == null)
                {
                    _logger.LogInformation("Keyspace {Keyspace} does not exist, creating it...", keyspace);
                    session.Execute($"CREATE KEYSPACE {keyspace} WITH REPLICATION = {{ 'class' : 'SimpleStrategy', 'replication_factor' : 1 }}");
                    _logger.LogInformation("Keyspace {Keyspace} created successfully", keyspace);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking/creating keyspace {Keyspace}", keyspace);
                throw;
            }
        }

        private Cassandra.ISession ConnectWithRetry(string keyspace)
        {
            int retryCount = 0;
            while (retryCount < MAX_RETRIES)
            {
                try
                {
                    var session = _cluster.Connect(keyspace);
                    EnsureTablesExist(session);
                    return session;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    if (retryCount >= MAX_RETRIES)
                    {
                        _logger.LogError(ex, "Failed to connect to keyspace {Keyspace} after {MaxRetries} attempts", keyspace, MAX_RETRIES);
                        throw;
                    }
                    _logger.LogWarning(ex, "Failed to connect to keyspace {Keyspace}, retrying in {Delay}ms (attempt {Retry}/{MaxRetries})", 
                        keyspace, RETRY_DELAY_MS, retryCount, MAX_RETRIES);
                    Thread.Sleep(RETRY_DELAY_MS);
                }
            }
            throw new Exception($"Failed to connect to keyspace {keyspace} after {MAX_RETRIES} attempts");
        }

        private void EnsureTablesExist(Cassandra.ISession session)
        {
            try
            {
                // Create comments table
                session.Execute(@"
                    CREATE TABLE IF NOT EXISTS comments (
                        id uuid PRIMARY KEY,
                        content text,
                        post_id uuid,
                        user_id text,
                        created_at timestamp
                    )");

                // Create indices for common queries
                session.Execute("CREATE INDEX IF NOT EXISTS ON comments (post_id)");
                session.Execute("CREATE INDEX IF NOT EXISTS ON comments (user_id)");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring tables exist");
                throw;
            }
        }

        public IMapper Mapper => _mapper;
        public Cassandra.ISession Session => _session;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _session?.Dispose();
                    _cluster?.Dispose();
                }

                _disposed = true;
            }
        }

        ~CassandraContext()
        {
            Dispose(false);
        }
    }
} 