#!/usr/bin/env bash
# build.sh — Build TileTool for Linux
set -euo pipefail

PROJECT="TileTool/TileTool.csproj"
RUNTIME="linux-x64"
OUTPUT="publish/linux-x64"

echo "=== TileTool Linux Build ==="
echo "Restoring packages..."
dotnet restore "$PROJECT"

echo "Building release..."
dotnet publish "$PROJECT" \
    --configuration Release \
    --runtime "$RUNTIME" \
    --self-contained true \
    --output "$OUTPUT" \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true

echo ""
echo "Done! Binary available at: $OUTPUT/TileTool"
