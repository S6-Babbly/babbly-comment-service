#!/bin/bash
set -e

# Get the container's own IP address - more reliable than hardcoding 0.0.0.0
CASSANDRA_HOST=$(hostname -i)

# Wait for Cassandra to be ready
until cqlsh $CASSANDRA_HOST -e "describe keyspaces"; do
  echo "Cassandra is unavailable - sleeping"
  sleep 2
done

echo "Cassandra is up - executing schema"

# Execute the initialization script
cqlsh $CASSANDRA_HOST -f /docker-entrypoint-initdb.d/01-init-keyspace-and-tables.cql

echo "Schema initialization completed" 