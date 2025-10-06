-- PostgreSQL Database Setup Script for MCP Gateway
-- Run this script to create the database and user

-- Create database
CREATE DATABASE mcpgateway;

-- Create user
CREATE USER agentic_user WITH PASSWORD 'agentic_password';

-- Grant privileges
GRANT ALL PRIVILEGES ON DATABASE mcpgateway TO agentic_user;

-- Connect to the database
\c mcpgateway;

-- Grant schema privileges
GRANT ALL ON SCHEMA public TO agentic_user;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO agentic_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO agentic_user;

-- Set default privileges for future tables
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO agentic_user;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO agentic_user;




