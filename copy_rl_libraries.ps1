# PowerShell script to copy RL libraries from rl project install/bin to RLlib folders
# This script calls the RLlib/copy_rl_libraries.ps1 script for convenience
# Usage: .\copy_rl_libraries.ps1 -RLInstallPath "C:\path\to\rl\install"
#        .\copy_rl_libraries.ps1 -RLInstallPath "C:\path\to\rl\install" -IncludeDependencies

param(
    [Parameter(Mandatory=$false)]
    [string]$RLInstallPath = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$IncludeDependencies = $false
)

$ErrorActionPreference = "Stop"

# Get script directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rlLibScript = Join-Path $scriptPath "RLlib\copy_rl_libraries.ps1"

if (-not (Test-Path $rlLibScript))
{
    Write-Host "Error: RLlib copy script not found at: $rlLibScript" -ForegroundColor Red
    exit 1
}

# Build arguments for the RLlib script
$arguments = @()
if (-not [string]::IsNullOrEmpty($RLInstallPath))
{
    $arguments += "-RLInstallPath"
    $arguments += $RLInstallPath
}
if ($IncludeDependencies)
{
    $arguments += "-IncludeDependencies"
}

# Call the RLlib script
& $rlLibScript @arguments

