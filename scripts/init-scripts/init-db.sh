#!/bin/bash
set -e

# Wait for Cassandra to be ready
until cqlsh -e "describe keyspaces"; do
  echo "Cassandra is unavailable - sleeping"
  sleep 2
done

echo "Cassandra is up - executing schema"

# Execute the initialization script
cqlsh -f /docker-entrypoint-initdb.d/init-cassandra.cql

echo "Schema initialization completed" 