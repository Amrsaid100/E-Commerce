#!/bin/bash
set -e

echo "Building E-Commerce API..."
dotnet restore
dotnet build -c Release
dotnet publish -c Release -o out

echo "Build completed successfully!"
