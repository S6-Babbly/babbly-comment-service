# Babbly Comment Service

A microservice for managing comments in the Babbly social media platform.

## Overview

The Babbly Comment Service is a RESTful API that provides endpoints for creating, reading, updating, and deleting comments. It uses Apache Cassandra as its database and is designed to be deployed as a Docker container.

## API Endpoints

### Comments

- `GET /api/Comment`: Get all comments
- `GET /api/Comment/{id}`: Get a specific comment by ID
- `GET /api/Comment/post/{postId}`: Get all comments for a specific post
- `GET /api/Comment/user/{userId}`: Get all comments by a specific user
- `POST /api/Comment`: Create a new comment
- `PUT /api/Comment/{id}`: Update an existing comment
- `DELETE /api/Comment/{id}`: Delete a comment

### Health Check

- `GET /api/Health`: Check service health

## Technical Stack

- ASP.NET Core
- C# 10
- Apache Cassandra
- Docker

## Setup and Installation

### Prerequisites

- .NET 9.0 SDK
- Docker & Docker Compose
- Apache Cassandra (for local development without Docker)

### Environment Variables

The following environment variables can be configured:

- `ASPNETCORE_ENVIRONMENT`: Development, Staging, or Production
- `CassandraHosts`: Comma-separated list of Cassandra hosts
- `CassandraKeyspace`: Cassandra keyspace name
- `CassandraUsername`: Cassandra username (optional)
- `CassandraPassword`: Cassandra password (optional)

### Running with Docker Compose

1. Clone the repository
2. Navigate to the root directory
3. Run `docker-compose up -d`
4. Access the API at `http://localhost:5004/swagger`

### Running Locally

1. Start a local Cassandra instance
2. Update `appsettings.Development.json` with your Cassandra configuration
3. Run `dotnet run` from the project directory
4. Access the API at `http://localhost:5000/swagger`

## Data Model

### Comment

```
{
  "id": "guid",
  "content": "string",
  "postId": "guid",
  "userId": "string",
  "createdAt": "timestamp"
}
```

## Database Schema

The service uses a Cassandra keyspace named `babbly_comments` with the following tables:

### Table: comments

```cql
CREATE TABLE IF NOT EXISTS comments (
    id uuid PRIMARY KEY,
    content text,
    post_id uuid,
    user_id text,
    created_at timestamp
);
```

### Indices

```cql
CREATE INDEX IF NOT EXISTS ON comments (post_id);
CREATE INDEX IF NOT EXISTS ON comments (user_id);
```

## Development

### Building

```sh
dotnet build
```

### Testing

```sh
dotnet test
```

## License

This project is licensed under the MIT License - see the LICENSE file for details. 