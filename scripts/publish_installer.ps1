$ErrorActionPreference = "Stop"

Write-Host "Starting Vantus File Indexer Build & Publish..."

# Ensure we are in the repo root
if (-not (Test-Path "Vantus.FileIndexer.sln")) {
    Write-Error "Please run this script from the repository root."
    exit 1
}

# Restore dependencies
Write-Host "Restoring dependencies..."
dotnet restore Vantus.FileIndexer.sln

# Build the packaging project
Write-Host "Building Vantus.Packaging (Release|x64)..."

# Try to find MSBuild (heuristic) or use dotnet build
# Note: wapproj often requires msbuild.exe.
# If running in VS Developer Command Prompt, msbuild is available.
# We attempt to use msbuild first.

if (Get-Command msbuild -ErrorAction SilentlyContinue) {
    msbuild Vantus.Packaging\Vantus.Packaging.wapproj /p:Configuration=Release /p:Platform=x64 /p:AppxBundle=Always /p:UapAppxPackageBuildMode=SideloadOnly /p:AppxPackageSigningEnabled=false
} else {
    Write-Warning "MSBuild not found in PATH. Attempting 'dotnet build' (this may fail for .wapproj)..."
    dotnet build Vantus.Packaging\Vantus.Packaging.wapproj -c Release /p:Platform=x64 /p:AppxBundle=Always /p:UapAppxPackageBuildMode=SideloadOnly /p:AppxPackageSigningEnabled=false
}

# Define the output directory pattern
# MSBuild usually outputs to Vantus.Packaging\AppPackages\<ProjectName>_<Version>_Test\
$packageBaseDir = "Vantus.Packaging\AppPackages"

if (-not (Test-Path $packageBaseDir)) {
    Write-Error "AppPackages directory not found at $packageBaseDir. Build may have failed."
    exit 1
}

# Find the latest msixbundle
$bundle = Get-ChildItem -Path $packageBaseDir -Filter "*.msixbundle" -Recurse | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if ($bundle) {
    Write-Host "Found bundle: $($bundle.FullName)"
    $dest = ".\VantusInstaller.msixbundle"
    Copy-Item $bundle.FullName -Destination $dest -Force
    Write-Host "SUCCESS: Installer copied to $dest"
} else {
    Write-Error "Could not find generated .msixbundle in $packageBaseDir"
}

# Check for setup.exe (Bootstrapper)
# This is usually generated if "Enable automatic updates" or specific sideloading options are on.
$setupExe = Get-ChildItem -Path $packageBaseDir -Filter "setup.exe" -Recurse | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if ($setupExe) {
     Copy-Item $setupExe.FullName -Destination ".\VantusSetup.exe" -Force
     Write-Host "SUCCESS: Bootstrapper copied to .\VantusSetup.exe"
} else {
    Write-Host "Note: setup.exe not found. This is expected if 'Generate App Installer' is disabled or signing is off. The .msixbundle is the primary installer."
}

Write-Host "Done."
