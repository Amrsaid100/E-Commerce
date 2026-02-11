#!/bin/bash
set -e

echo "🔨 Building E-Commerce API for Production..."

# Restore
echo "📦 Restoring dependencies..."
dotnet restore

# Build
echo "🛠️ Building project..."
dotnet build -c Release --no-restore

# Publish
echo "📤 Publishing project..."
dotnet publish -c Release -o out --no-build

echo "✅ Build completed successfully!"
ls -la out/
