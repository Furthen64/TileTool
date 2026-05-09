# winbuild.ps1 — Build TileTool for Windows
param(
    [string]$Runtime = "win-x64",
    [string]$Output  = "publish\win-x64"
)

$ErrorActionPreference = "Stop"
$Project = "TileTool\TileTool.csproj"

Write-Host "=== TileTool Windows Build ===" -ForegroundColor Cyan
Write-Host "Restoring packages..."
dotnet restore $Project

Write-Host "Building release..."
dotnet publish $Project `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    --output $Output `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true

Write-Host ""
Write-Host "Done! Binary available at: $Output\TileTool.exe" -ForegroundColor Green
