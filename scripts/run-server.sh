#!/bin/bash
# Run PandoDeploy server

cd "$(dirname "$0")/.."

echo "Starting PandoDeploy Server..."
echo ""

# Create data directories if they don't exist
mkdir -p ./data/images

# Run the server
dotnet run --project src/PandoDeploy.Server/PandoDeploy.Server.csproj

