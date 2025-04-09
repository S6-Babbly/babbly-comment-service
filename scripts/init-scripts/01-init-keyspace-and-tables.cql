-- Create keyspace
CREATE KEYSPACE IF NOT EXISTS babbly_comments
WITH REPLICATION = { 
    'class' : 'SimpleStrategy', 
    'replication_factor' : 1 
};

-- Use the keyspace
USE babbly_comments;

-- Create comments table
CREATE TABLE IF NOT EXISTS comments (
    id uuid PRIMARY KEY,
    content text,
    post_id uuid,
    user_id text,
    created_at timestamp
);

-- Create indices for common queries
CREATE INDEX IF NOT EXISTS ON comments (post_id);
CREATE INDEX IF NOT EXISTS ON comments (user_id);

-- Create user credentials if not using default Cassandra auth
-- CREATE USER IF NOT EXISTS babbly_user WITH PASSWORD 'babbly_password' SUPERUSER;
-- GRANT ALL PERMISSIONS ON KEYSPACE babbly_comments TO babbly_user; 