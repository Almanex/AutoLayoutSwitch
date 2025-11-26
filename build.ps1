# Build script for creating a release

Write-Host "Building AutoLayoutSwitch Release..." -ForegroundColor Cyan

# Clean previous builds
if (Test-Path ".\src\bin") {
    Remove-Item ".\src\bin" -Recurse -Force
    Write-Host "Cleaned bin directory" -ForegroundColor Green
}

if (Test-Path ".\src\obj") {
    Remove-Item ".\src\obj" -Recurse -Force
    Write-Host "Cleaned obj directory" -ForegroundColor Green
}

# Build Release
Write-Host "`nBuilding Release..." -ForegroundColor Cyan
dotnet publish .\src\AutoLayoutSwitch.csproj -c Release -r win-x64

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nBuild successful!" -ForegroundColor Green
    
    # Copy icon
    Copy-Item ".\src\icon.ico" ".\src\bin\Release\net8.0-windows\win-x64\publish\icon.ico"
    
    # Create release directory
    $releaseDir = ".\release"
    if (-not (Test-Path $releaseDir)) {
        New-Item -ItemType Directory -Path $releaseDir | Out-Null
    }
    
    # Copy executable
    Copy-Item ".\src\bin\Release\net8.0-windows\win-x64\publish\AutoLayoutSwitch.exe" "$releaseDir\AutoLayoutSwitch.exe"
    Copy-Item ".\src\icon.ico" "$releaseDir\icon.ico"
    Copy-Item ".\README.md" "$releaseDir\README.md"
    Copy-Item ".\LICENSE" "$releaseDir\LICENSE"
    
    Write-Host "`nRelease files created in: $releaseDir" -ForegroundColor Green
    Write-Host "Executable: $releaseDir\AutoLayoutSwitch.exe" -ForegroundColor Yellow
    
    # Get file size
    $fileSize = (Get-Item "$releaseDir\AutoLayoutSwitch.exe").Length / 1MB
    Write-Host ("File size: {0:N2} MB" -f $fileSize) -ForegroundColor Yellow
} else {
    Write-Host "`nBuild failed!" -ForegroundColor Red
    exit 1
}
