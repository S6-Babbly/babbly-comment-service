# Babbly Comment Service

The comment management microservice for the Babbly platform, providing RESTful API endpoints for creating, reading, updating, and deleting comments on posts.

## Tech Stack

- **Backend**: ASP.NET Core 9.0
- **Database**: Apache Cassandra
- **Message Broker**: Kafka (for event publishing)
- **API Documentation**: Swagger/OpenAPI

## Features

- Comment creation and management (CRUD operations)
- Query comments by post or user
- Event publishing to Kafka for comment operations
- High-performance distributed storage with Cassandra

## Local Development Setup

### Prerequisites

- .NET SDK 9.0 or later
- Docker and Docker Compose
- Apache Cassandra

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/babbly-comment-service.git
   cd babbly-comment-service
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Configure Cassandra connection:
   ```bash
   # Using user secrets for development
   dotnet user-secrets set "CassandraHosts" "localhost"
   dotnet user-secrets set "CassandraKeyspace" "babbly_comments"
   dotnet user-secrets set "CassandraUsername" "cassandra"
   dotnet user-secrets set "CassandraPassword" "cassandra"
   ```

4. Run the service:
   ```bash
   dotnet run
   ```

The API will be available at `http://localhost:8082`.

### Database Initialization

The Cassandra keyspace and tables are automatically created on startup.

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `CassandraHosts` | Comma-separated Cassandra hosts | `localhost` |
| `CassandraKeyspace` | Keyspace name | `babbly_comments` |
| `CassandraUsername` | Database username | `cassandra` |
| `CassandraPassword` | Database password | `cassandra` |

## API Endpoints

### Comments
- `GET /api/Comment` - Get all comments
- `GET /api/Comment/{id}` - Get a specific comment by ID
- `GET /api/Comment/post/{postId}` - Get all comments for a post
- `GET /api/Comment/user/{userId}` - Get all comments by a user
- `POST /api/Comment` - Create a new comment
- `PUT /api/Comment/{id}` - Update an existing comment
- `DELETE /api/Comment/{id}` - Delete a comment

### Health Check
- `GET /api/Health` - Service health check

## Database Schema

### Comments Table
```cql
CREATE TABLE comments (
    id uuid PRIMARY KEY,
    content text,
    post_id uuid,
    user_id text,
    created_at timestamp
);
```

### Indices
- `post_id` index for querying comments by post
- `user_id` index for querying comments by user

## Docker Support

Run the service with Docker Compose:

```bash
# From the root of the Babbly organization
docker-compose up -d comment-service
```

Or run with its own Docker Compose (includes Cassandra):

```bash
# From the babbly-comment-service directory
docker-compose up -d
```

The service will be available at `http://localhost:8082`.

## Architecture Notes

### Why Cassandra?

Cassandra was chosen for the Comment Service because:
- **High write throughput**: Ideal for comment-heavy discussions
- **Efficient querying**: Fast retrieval of comments by post or user
- **Horizontal scalability**: Easy to scale with growing user engagement
- **Distributed by design**: Reliable and fault-tolerant

### Kafka Integration

The service publishes events to the `comment-events` Kafka topic:
- `CommentCreated` - When a new comment is added
- `CommentUpdated` - When a comment is modified
- `CommentDeleted` - When a comment is removed

These events can be consumed by other services for notifications or analytics.

### Integration with Babbly Ecosystem

- **API Gateway**: Routes comment-related requests to this service
- **Post Service**: Comments are associated with posts via post ID
- **User Service**: Comments are associated with users via user ID
- **Frontend**: Creates and displays comments through the API Gateway

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
