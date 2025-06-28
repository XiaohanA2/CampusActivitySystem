# Campus Activity System - Release Script
param(
    [string]$Version = "1.0.0",
    [string]$OutputPath = ".\releases",
    [switch]$Help
)

if ($Help) {
    Write-Host "Campus Activity System - Release Script" -ForegroundColor Cyan
    Write-Host "Usage: .\release.ps1 [-Version 1.0.0] [-OutputPath .\releases]" -ForegroundColor Yellow
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host "  .\release.ps1" -ForegroundColor White
    Write-Host "  .\release.ps1 -Version 2.0.0" -ForegroundColor White
    exit 0
}

$ErrorActionPreference = "Stop"
$ReleaseName = "CampusActivitySystem-v$Version"

Write-Host "Creating release package: $ReleaseName" -ForegroundColor Green
Write-Host "Output path: $OutputPath" -ForegroundColor Cyan

# Check .NET SDK
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET SDK: $dotnetVersion" -ForegroundColor Green
}
catch {
    Write-Host "❌ .NET SDK not found" -ForegroundColor Red
    exit 1
}

# Check solution file
if (!(Test-Path "CampusActivitySystem.sln")) {
    Write-Host "❌ Solution file not found" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Solution file found" -ForegroundColor Green

# Create output directory
if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

$releaseDir = Join-Path $OutputPath $ReleaseName

# Clean old release
if (Test-Path $releaseDir) {
    Remove-Item $releaseDir -Recurse -Force
}

# Create directories
Write-Host "🔄 Creating directories..." -ForegroundColor Blue
New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $releaseDir "WebAPI") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $releaseDir "BlazorWeb") -Force | Out-Null

# Clean and restore
Write-Host "🔄 Cleaning solution..." -ForegroundColor Blue
dotnet clean CampusActivitySystem.sln --configuration Release --verbosity quiet

Write-Host "🔄 Restoring packages..." -ForegroundColor Blue
dotnet restore CampusActivitySystem.sln --verbosity quiet

# Publish WebAPI
Write-Host "🔄 Publishing WebAPI..." -ForegroundColor Blue
$webApiOutput = Join-Path $releaseDir "WebAPI"
dotnet publish "src/CampusActivity.WebAPI/CampusActivity.WebAPI.csproj" --configuration Release --output $webApiOutput --no-restore --verbosity quiet

# Publish BlazorWeb
Write-Host "🔄 Publishing BlazorWeb..." -ForegroundColor Blue
$blazorOutput = Join-Path $releaseDir "BlazorWeb"
dotnet publish "src/CampusActivity.BlazorWeb/CampusActivity.BlazorWeb.csproj" --configuration Release --output $blazorOutput --no-restore --verbosity quiet

# Create startup scripts
Write-Host "🔄 Creating startup scripts..." -ForegroundColor Blue

# Create start.bat
$startBat = @()
$startBat += "@echo off"
$startBat += "echo Starting Campus Activity System..."
$startBat += "echo Starting WebAPI..."
$startBat += "start `"WebAPI`" /D WebAPI dotnet CampusActivity.WebAPI.dll"
$startBat += "timeout /t 3 /nobreak >nul"
$startBat += "echo Starting BlazorWeb..."
$startBat += "start `"BlazorWeb`" /D BlazorWeb dotnet CampusActivity.BlazorWeb.dll"
$startBat += "echo."
$startBat += "echo System started!"
$startBat += "echo WebAPI: http://localhost:7186"
$startBat += "echo Web UI: http://localhost:7150"
$startBat += "echo."
$startBat += "pause"

$startBat | Out-File -FilePath (Join-Path $releaseDir "start.bat") -Encoding ASCII

# Create stop.bat
$stopBat = @()
$stopBat += "@echo off"
$stopBat += "echo Stopping Campus Activity System..."
$stopBat += "taskkill /f /im dotnet.exe 2>nul"
$stopBat += "echo System stopped."
$stopBat += "pause"

$stopBat | Out-File -FilePath (Join-Path $releaseDir "stop.bat") -Encoding ASCII

# Create start.ps1
$startPs1 = @()
$startPs1 += "# Start Campus Activity System"
$startPs1 += "Write-Host `"Starting Campus Activity System...`" -ForegroundColor Green"
$startPs1 += "Write-Host `"Starting WebAPI...`" -ForegroundColor Yellow"
$startPs1 += "Start-Process powershell -ArgumentList `"-NoExit`", `"-Command`", `"cd WebAPI; dotnet CampusActivity.WebAPI.dll`""
$startPs1 += "Start-Sleep -Seconds 3"
$startPs1 += "Write-Host `"Starting BlazorWeb...`" -ForegroundColor Yellow"  
$startPs1 += "Start-Process powershell -ArgumentList `"-NoExit`", `"-Command`", `"cd BlazorWeb; dotnet CampusActivity.BlazorWeb.dll`""
$startPs1 += "Write-Host `"System started!`" -ForegroundColor Green"
$startPs1 += "Write-Host `"WebAPI: http://localhost:7186`" -ForegroundColor Cyan"
$startPs1 += "Write-Host `"Web UI: http://localhost:7150`" -ForegroundColor Cyan"

$startPs1 | Out-File -FilePath (Join-Path $releaseDir "start.ps1") -Encoding UTF8

# Create README.txt
Write-Host "🔄 Creating README..." -ForegroundColor Blue
$readme = @()
$readme += "=== Campus Activity Management System ==="
$readme += ""
$readme += "Version: $Version"
$readme += "Build Date: $(Get-Date -Format 'yyyy-MM-dd')"
$readme += ""
$readme += "CONFIGURATION REQUIRED:"
$readme += ""
$readme += "Before running, configure database connection in:"
$readme += "- WebAPI\appsettings.json"
$readme += "- BlazorWeb\appsettings.json"
$readme += ""
$readme += "Update ConnectionStrings section:"
$readme += "{"
$readme += "  `"ConnectionStrings`": {"
$readme += "    `"DefaultConnection`": `"Server=YOUR_SERVER;Database=CampusActivityDB;User=YOUR_USER;Password=YOUR_PASSWORD;`""
$readme += "  }"
$readme += "}"
$readme += ""
$readme += "Default accounts:"
$readme += "- Admin: admin / admin123"
$readme += "- Student: student1 / 123456"
$readme += "- Teacher: teacher1 / 123456"
$readme += ""
$readme += "System URLs:"
$readme += "- Web UI: http://localhost:7150"
$readme += "- API: http://localhost:7186"
$readme += "- API Docs: http://localhost:7186/swagger"
$readme += ""
$readme += "Requirements:"
$readme += "- .NET 8.0 Runtime"
$readme += "- MySQL 8.0+"
$readme += ""
$readme += "To start: Run start.bat or start.ps1"
$readme += "To stop: Run stop.bat"

$readme | Out-File -FilePath (Join-Path $releaseDir "README.txt") -Encoding UTF8

# Create ZIP archive
Write-Host "🔄 Creating ZIP archive..." -ForegroundColor Blue
try {
    $archivePath = Join-Path $OutputPath "$ReleaseName.zip"
    if (Test-Path $archivePath) {
        Remove-Item $archivePath -Force
    }
    
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::CreateFromDirectory($releaseDir, $archivePath)
    
    $archiveSize = [math]::Round((Get-Item $archivePath).Length / 1MB, 2)
    Write-Host "✅ Archive created: $archivePath" -ForegroundColor Green
    Write-Host "✅ Archive size: $archiveSize MB" -ForegroundColor Green
}
catch {
    Write-Host "⚠️ Failed to create archive: $($_.Exception.Message)" -ForegroundColor Yellow
}

# Summary
Write-Host ""
Write-Host "🎉 Release completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Release information:" -ForegroundColor Cyan
Write-Host "  Version: $Version" -ForegroundColor White
Write-Host "  Location: $releaseDir" -ForegroundColor White
Write-Host "  Archive: $ReleaseName.zip" -ForegroundColor White
Write-Host ""
Write-Host "Package contents:" -ForegroundColor Cyan
Write-Host "  📦 WebAPI application" -ForegroundColor White
Write-Host "  🎨 BlazorWeb application" -ForegroundColor White
Write-Host "  🚀 start.bat, start.ps1" -ForegroundColor White
Write-Host "  🛑 stop.bat" -ForegroundColor White
Write-Host "  📋 README.txt" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Extract to target server" -ForegroundColor White
Write-Host "  2. Configure database in appsettings.json" -ForegroundColor White
Write-Host "  3. Run start.bat or start.ps1" -ForegroundColor White
Write-Host ""

exit 0 