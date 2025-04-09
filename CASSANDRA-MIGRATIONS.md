# Cassandra Migrations

This document explains how we handle database schema migrations in the Babbly Comment Service.

## Initial Schema Setup

The initial database schema is created when the Cassandra container starts for the first time. The CQL scripts in the `scripts/init-scripts` directory are executed in alphabetical order.

Current initialization scripts:
- `01-init-keyspace-and-tables.cql`: Creates the keyspace and initial tables with indexes

## Manual Migrations

For now, we handle schema changes manually. Here's the process:

1. Create a new CQL script in the `scripts/init-scripts` directory with a sequential number prefix
2. The script should include idempotent commands (using `IF NOT EXISTS` or similar)
3. Test the migration locally
4. When deploying, the new scripts will run automatically when the container starts

## Future Enhancements

In the future, we may implement an automated migration system that:
- Tracks which migrations have been applied
- Only applies new migrations
- Handles rollbacks if needed 