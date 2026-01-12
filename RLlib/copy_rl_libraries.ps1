# PowerShell script to copy RL libraries from rl project install/bin to RLlib folders
# Usage: .\copy_rl_libraries.ps1 -RLInstallPath "C:\path\to\rl\install"
#        .\copy_rl_libraries.ps1 -RLInstallPath "C:\path\to\rl\install" -IncludeDependencies

param(
    [Parameter(Mandatory=$false)]
    [string]$RLInstallPath = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$IncludeDependencies = $false
)

$ErrorActionPreference = "Stop"

# Get script directory (RLlib folder)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$windowsPath = Join-Path $scriptDir "Windows"
$linuxPath = Join-Path $scriptDir "Linux"
$macosPath = Join-Path $scriptDir "macOS"

# Create target directories
Write-Host "Creating target directories..."
New-Item -ItemType Directory -Force -Path $windowsPath | Out-Null
New-Item -ItemType Directory -Force -Path $linuxPath | Out-Null
New-Item -ItemType Directory -Force -Path $macosPath | Out-Null

# Determine source path
if ([string]::IsNullOrEmpty($RLInstallPath))
{
    # Try to find RL install directory relative to this script
    # Assuming RLTrajectoryPlanner is sibling to rl project
    $projectRoot = Split-Path -Parent (Split-Path -Parent $scriptDir)
    $RLInstallPath = Join-Path $projectRoot "rl\install"
    
    if (-not (Test-Path $RLInstallPath))
    {
        Write-Host "Error: RL install directory not found at: $RLInstallPath" -ForegroundColor Red
        Write-Host ""
        Write-Host "Usage: .\copy_rl_libraries.ps1 -RLInstallPath 'C:\path\to\rl\install'" -ForegroundColor Yellow
        Write-Host "       .\copy_rl_libraries.ps1 -RLInstallPath 'C:\path\to\rl\install' -IncludeDependencies" -ForegroundColor Yellow
        exit 1
    }
}

$binPath = Join-Path $RLInstallPath "bin"

if (-not (Test-Path $binPath))
{
    Write-Host "Error: RL install/bin directory not found at: $binPath" -ForegroundColor Red
    exit 1
}

Write-Host "Source directory: $binPath" -ForegroundColor Green
Write-Host "Target directories:" -ForegroundColor Green
Write-Host "  Windows: $windowsPath"
Write-Host "  Linux:   $linuxPath"
Write-Host "  macOS:   $macosPath"
Write-Host ""

# Copy Windows DLLs (rl*.dll)
Write-Host "Copying Windows libraries (rl*.dll)..." -ForegroundColor Cyan
$windowsFiles = Get-ChildItem -Path $binPath -Filter "rl*.dll" -ErrorAction SilentlyContinue
if ($windowsFiles)
{
    foreach ($file in $windowsFiles)
    {
        $destPath = Join-Path $windowsPath $file.Name
        Copy-Item $file.FullName -Destination $destPath -Force
        Write-Host "  Copied $($file.Name)" -ForegroundColor Gray
    }
    Write-Host "  Copied $($windowsFiles.Count) RL library file(s) to Windows folder" -ForegroundColor Green
}
else
{
    Write-Host "  No rl*.dll files found in $binPath" -ForegroundColor Yellow
}

# Copy dependency DLLs if requested (Visual C++ runtime, Boost, etc.)
if ($IncludeDependencies)
{
    Write-Host ""
    Write-Host "Copying Windows dependency libraries..." -ForegroundColor Cyan
    
    # Visual C++ Runtime DLLs
    $vcRuntimePatterns = @("msvcp*.dll", "vcruntime*.dll", "concrt*.dll")
    foreach ($pattern in $vcRuntimePatterns)
    {
        $depFiles = Get-ChildItem -Path $binPath -Filter $pattern -ErrorAction SilentlyContinue
        foreach ($file in $depFiles)
        {
            $destPath = Join-Path $windowsPath $file.Name
            Copy-Item $file.FullName -Destination $destPath -Force
            Write-Host "  Copied dependency: $($file.Name)" -ForegroundColor Gray
        }
    }
    
    # Look for Boost DLLs (boost_*.dll)
    $boostFiles = Get-ChildItem -Path $binPath -Filter "boost_*.dll" -ErrorAction SilentlyContinue
    foreach ($file in $boostFiles)
    {
        $destPath = Join-Path $windowsPath $file.Name
        Copy-Item $file.FullName -Destination $destPath -Force
        Write-Host "  Copied dependency: $($file.Name)" -ForegroundColor Gray
    }
    
    # Look for libxml2 DLLs
    $libxmlFiles = Get-ChildItem -Path $binPath -Filter "*xml*.dll" -ErrorAction SilentlyContinue
    foreach ($file in $libxmlFiles)
    {
        $destPath = Join-Path $windowsPath $file.Name
        Copy-Item $file.FullName -Destination $destPath -Force
        Write-Host "  Copied dependency: $($file.Name)" -ForegroundColor Gray
    }
}

# Copy Linux .so files (librl*.so)
Write-Host ""
Write-Host "Copying Linux libraries (librl*.so)..." -ForegroundColor Cyan
$linuxFiles = Get-ChildItem -Path $binPath -Filter "librl*.so*" -ErrorAction SilentlyContinue
if ($linuxFiles)
{
    foreach ($file in $linuxFiles)
    {
        $destPath = Join-Path $linuxPath $file.Name
        Copy-Item $file.FullName -Destination $destPath -Force
        Write-Host "  Copied $($file.Name)" -ForegroundColor Gray
    }
    Write-Host "  Copied $($linuxFiles.Count) RL library file(s) to Linux folder" -ForegroundColor Green
}
else
{
    Write-Host "  No librl*.so files found in $binPath" -ForegroundColor Yellow
    Write-Host "  (This is normal if building on Windows)" -ForegroundColor Gray
}

# Copy macOS .dylib files (librl*.dylib)
Write-Host ""
Write-Host "Copying macOS libraries (librl*.dylib)..." -ForegroundColor Cyan
$macosFiles = Get-ChildItem -Path $binPath -Filter "librl*.dylib*" -ErrorAction SilentlyContinue
if ($macosFiles)
{
    foreach ($file in $macosFiles)
    {
        $destPath = Join-Path $macosPath $file.Name
        Copy-Item $file.FullName -Destination $destPath -Force
        Write-Host "  Copied $($file.Name)" -ForegroundColor Gray
    }
    Write-Host "  Copied $($macosFiles.Count) RL library file(s) to macOS folder" -ForegroundColor Green
}
else
{
    Write-Host "  No librl*.dylib files found in $binPath" -ForegroundColor Yellow
    Write-Host "  (This is normal if building on Windows)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Done! Libraries copied successfully." -ForegroundColor Green
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Windows: $windowsPath"
if ($windowsFiles) { Write-Host "    - $($windowsFiles.Count) RL library file(s)" }
Write-Host "  Linux:   $linuxPath"
if ($linuxFiles) { Write-Host "    - $($linuxFiles.Count) RL library file(s)" }
Write-Host "  macOS:   $macosPath"
if ($macosFiles) { Write-Host "    - $($macosFiles.Count) RL library file(s)" }
Write-Host ""
Write-Host "Note: If you need dependency libraries (Boost, libxml2, etc.)," -ForegroundColor Yellow
Write-Host "      run with -IncludeDependencies flag or copy them manually." -ForegroundColor Yellow
