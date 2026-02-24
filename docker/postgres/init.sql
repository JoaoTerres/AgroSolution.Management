-- Creates application databases inside the single PostgreSQL container.
-- This script runs automatically on first container startup (via docker-entrypoint-initdb.d).
-- Add more CREATE DATABASE statements here if new microservices need their own DB.

SELECT 'CREATE DATABASE agrosolution_management'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'agrosolution_management') \gexec

SELECT 'CREATE DATABASE agrosolution_identity'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'agrosolution_identity') \gexec
