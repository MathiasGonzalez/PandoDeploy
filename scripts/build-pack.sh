#!/bin/bash
# Build and pack PandoDeploy for local testing

VERSION=${1:-"1.0.0-local"}

echo "Building PandoDeploy version $VERSION"

# Clean previous builds
echo "Cleaning..."
dotnet clean --configuration Release

# Restore dependencies
echo "Restoring dependencies..."
dotnet restore

# Build
echo "Building..."
dotnet build --configuration Release --no-restore /p:Version=$VERSION

# Run tests
echo "Running tests..."
dotnet test --configuration Release --no-build --verbosity minimal

# Pack CLI tool
echo "Packing CLI tool..."
PACK_OUTPUT="./nupkgs"
rm -rf $PACK_OUTPUT
mkdir -p $PACK_OUTPUT

dotnet pack src/PandoDeploy.CLI/PandoDeploy.CLI.csproj --configuration Release --no-build --output $PACK_OUTPUT /p:Version=$VERSION

echo ""
echo "Package created successfully!"
echo "To install locally, run:"
echo "  dotnet tool uninstall -g PandoDeploy"
echo "  dotnet tool install -g PandoDeploy --add-source $PACK_OUTPUT --version $VERSION"

