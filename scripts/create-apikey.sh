#!/bin/bash
# Create an API key in the PandoDeploy database

DB_PATH=${1:-"./data/pandodeploy.db"}
KEY_NAME=${2:-"default"}

if [ ! -f "$DB_PATH" ]; then
    echo "Database not found at $DB_PATH"
    echo "Please start the server first or specify the correct path"
    exit 1
fi

API_KEY=$(uuidgen | tr -d '-' | tr '[:upper:]' '[:lower:]')

sqlite3 "$DB_PATH" "INSERT INTO ApiKeys (Key, Name, IsActive, CreatedAt) VALUES ('$API_KEY', '$KEY_NAME', 1, datetime('now'));"

if [ $? -eq 0 ]; then
    echo "API Key created successfully!"
    echo ""
    echo "Name: $KEY_NAME"
    echo "Key: $API_KEY"
    echo ""
    echo "Export it as environment variable:"
    echo "  export PANDODEPLOY_API_KEY=$API_KEY"
    echo ""
    echo "Or use it directly in commands:"
    echo "  pandodeploy deploy --image myapp:latest --server http://localhost:5000 --api-key $API_KEY"
else
    echo "Failed to create API key"
    exit 1
fi

