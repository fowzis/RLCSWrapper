# PowerShell script to build RLWrapper native library
# Usage: .\build_rlwrapper.ps1 -RLInstallPath "C:\path\to\rl\install"
#        .\build_rlwrapper.ps1 -RLInstallPath "C:\path\to\rl\install" -BuildType Release
#        .\build_rlwrapper.ps1 -RLInstallPath "C:\path\to\rl\install" -BuildType Release -CopyToRLlib
#        .\build_rlwrapper.ps1 -RLInstallPath "C:\path\to\rl\install" -BoostRoot "C:\path\to\boost"

param(
    [Parameter(Mandatory=$false)]
    [string]$RLInstallPath = "",
    
    [Parameter(Mandatory=$false)]
    [ValidateSet("Debug", "Release", "RelWithDebInfo", "MinSizeRel")]
    [string]$BuildType = "Release",
    
    [Parameter(Mandatory=$false)]
    [switch]$CopyToRLlib = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$Clean = $false,
    
    [Parameter(Mandatory=$false)]
    [string]$BoostRoot = ""
)

$ErrorActionPreference = "Stop"

# Get script directory (RLWrapper folder)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$buildDir = Join-Path $scriptDir "build"
$projectRoot = Split-Path -Parent $scriptDir
$rlLibPath = Join-Path $projectRoot "RLlib"
# Path to rl-3rdparty install (where Boost and other dependencies are)
$thirdPartyRoot = Split-Path -Parent $projectRoot
$thirdPartyInstallPath = Join-Path $thirdPartyRoot "rl-3rdparty\install"

Write-Host "RLWrapper Build Script" -ForegroundColor Cyan
Write-Host "=====================" -ForegroundColor Cyan
Write-Host ""

# Determine RL install path
if ([string]::IsNullOrEmpty($RLInstallPath))
{
    # Try to find RL install directory relative to this script
    # Assuming RLTrajectoryPlanner is sibling to rl project
    $RLInstallPath = Join-Path (Split-Path -Parent $projectRoot) "rl\install"
    
    if (-not (Test-Path $RLInstallPath))
    {
        Write-Host "Error: RL install directory not found at: $RLInstallPath" -ForegroundColor Red
        Write-Host ""
        Write-Host "Usage: .\build_rlwrapper.ps1 -RLInstallPath 'C:\path\to\rl\install'" -ForegroundColor Yellow
        Write-Host "       .\build_rlwrapper.ps1 -RLInstallPath 'C:\path\to\rl\install' -BuildType Release" -ForegroundColor Yellow
        Write-Host "       .\build_rlwrapper.ps1 -RLInstallPath 'C:\path\to\rl\install' -CopyToRLlib" -ForegroundColor Yellow
        exit 1
    }
}

# Verify RL install path exists
if (-not (Test-Path $RLInstallPath))
{
    Write-Host "Error: RL install directory not found at: $RLInstallPath" -ForegroundColor Red
    exit 1
}

# Check for CMake
$cmakePath = Get-Command cmake -ErrorAction SilentlyContinue
if (-not $cmakePath)
{
    Write-Host "Error: CMake not found in PATH. Please install CMake and ensure it's in your PATH." -ForegroundColor Red
    exit 1
}

Write-Host "Configuration:" -ForegroundColor Green
Write-Host "  Script directory:      $scriptDir"
Write-Host "  Build directory:       $buildDir"
Write-Host "  RL install path:       $RLInstallPath"
Write-Host "  Third-party install:    $thirdPartyInstallPath"
Write-Host "  Build type:             $BuildType"
Write-Host "  Copy to RLlib:          $CopyToRLlib"
if (-not [string]::IsNullOrEmpty($BoostRoot))
{
    Write-Host "  Boost root (manual):   $BoostRoot"
}
Write-Host ""

# Clean build directory if requested
if ($Clean -and (Test-Path $buildDir))
{
    Write-Host "Cleaning build directory..." -ForegroundColor Yellow
    Remove-Item -Path $buildDir -Recurse -Force
    Write-Host "  Build directory cleaned" -ForegroundColor Green
}

# Create build directory
if (-not (Test-Path $buildDir))
{
    Write-Host "Creating build directory..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Path $buildDir | Out-Null
    Write-Host "  Build directory created" -ForegroundColor Green
}

# Configure CMake
Write-Host ""
Write-Host "Configuring CMake..." -ForegroundColor Cyan

$cmakeArgs = @(
    ".."
    "-DCMAKE_BUILD_TYPE=$BuildType"
)

# Add RL library path if provided
if (-not [string]::IsNullOrEmpty($RLInstallPath))
{
    # Try different possible RL config paths (check for versioned folder first)
    $rlConfigPathAlt = Join-Path $RLInstallPath "lib\cmake\rl-0.7.0"
    $rlConfigPath = Join-Path $RLInstallPath "lib\cmake\rl"
    
    if (Test-Path $rlConfigPathAlt)
    {
        $cmakeArgs += "-Drl_DIR=`"$rlConfigPathAlt`""
        Write-Host "  Found RL config at: $rlConfigPathAlt" -ForegroundColor Gray
    }
    elseif (Test-Path $rlConfigPath)
    {
        $cmakeArgs += "-Drl_DIR=`"$rlConfigPath`""
        Write-Host "  Found RL config at: $rlConfigPath" -ForegroundColor Gray
    }
    else
    {
        Write-Host "  Warning: RL config not found at expected paths" -ForegroundColor Yellow
    }
    
    # Add CMAKE_PREFIX_PATH to help find dependencies (Boost, Bullet, etc.)
    # Include both RL install and third-party install paths
    $prefixPaths = @($RLInstallPath)
    if (Test-Path $thirdPartyInstallPath)
    {
        $prefixPaths += $thirdPartyInstallPath
    }
    $cmakeArgs += "-DCMAKE_PREFIX_PATH=`"$($prefixPaths -join ';')`""
    
    # Try to help CMake find Boost
    # Boost might be in RL install or system-installed
        if (-not [string]::IsNullOrEmpty($BoostRoot))
        {
            # User specified Boost path
            if (Test-Path $BoostRoot)
            {
                # Use BOOST_ROOT (all caps) for CMake 4.2+ compatibility
                $cmakeArgs += "-DBOOST_ROOT=`"$BoostRoot`""
                Write-Host "  Using Boost at: $BoostRoot" -ForegroundColor Gray
            }
            else
            {
                Write-Host "  Warning: Specified Boost path not found: $BoostRoot" -ForegroundColor Yellow
            }
        }
    else
    {
        # Try to auto-detect Boost
        # Priority 1: Check rl-3rdparty/install (relative to solution)
        $boostFound = $false
        
        # Check for Boost in rl-3rdparty/install (may be versioned like boost-1_88)
        $thirdPartyIncludePath = Join-Path $thirdPartyInstallPath "include"
        if (Test-Path $thirdPartyIncludePath)
        {
            # Look for boost or boost-* directories
            $boostDirs = Get-ChildItem -Path $thirdPartyIncludePath -Directory -Filter "boost*" -ErrorAction SilentlyContinue
            if ($boostDirs)
            {
                # Use the first boost directory found (usually boost-1_88 or boost)
                $boostDir = $boostDirs[0]
                $boostIncludePath = Join-Path $boostDir.FullName "boost"
                if (Test-Path $boostIncludePath)
                {
                    # Boost found in versioned folder
                    # Use BOOST_ROOT (all caps) for CMake 4.2+ compatibility
                    $cmakeArgs += "-DBOOST_ROOT=`"$thirdPartyInstallPath`""
                    $cmakeArgs += "-DBoost_INCLUDE_DIR=`"$boostIncludePath`""
                    Write-Host "  Found Boost in rl-3rdparty/install ($($boostDir.Name))" -ForegroundColor Green
                    Write-Host "    Boost include: $boostIncludePath" -ForegroundColor Gray
                    $boostFound = $true
                }
            }
        }
        
        if (-not $boostFound)
        {
            # Priority 2: Check if Boost is bundled with RL install
            $boostIncludePath = Join-Path $RLInstallPath "include\boost"
            if (Test-Path $boostIncludePath)
            {
                # Use BOOST_ROOT (all caps) for CMake 4.2+ compatibility
                $cmakeArgs += "-DBOOST_ROOT=`"$RLInstallPath`""
                Write-Host "  Found Boost in RL install" -ForegroundColor Gray
                $boostFound = $true
            }
            else
            {
                # Priority 3: Try common Boost installation locations
                $commonBoostPaths = @(
                    "C:\local\boost",
                    "C:\boost",
                    "$env:ProgramFiles\boost"
                )
                foreach ($boostPath in $commonBoostPaths)
                {
                    if (Test-Path $boostPath)
                    {
                        # Use BOOST_ROOT (all caps) for CMake 4.2+ compatibility
                        $cmakeArgs += "-DBOOST_ROOT=`"$boostPath`""
                        Write-Host "  Found Boost at: $boostPath" -ForegroundColor Gray
                        $boostFound = $true
                        break
                    }
                }
            }
        }
        
        if (-not $boostFound)
        {
            Write-Host "  Note: Boost not auto-detected. CMake will try to find it." -ForegroundColor Yellow
            Write-Host "        Expected location: $thirdPartyInstallPath" -ForegroundColor Yellow
            Write-Host "        If build fails, specify Boost path with -BoostRoot parameter" -ForegroundColor Yellow
        Write-Host "        Note: CMake 4.2+ requires BOOST_ROOT (all caps) instead of Boost_ROOT" -ForegroundColor Yellow
        }
    }
}

Write-Host "  CMake arguments: $($cmakeArgs -join ' ')" -ForegroundColor Gray

Push-Location $buildDir
try
{
    # Temporarily change error action to continue to avoid exceptions on warnings
    $oldErrorAction = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    
    try
    {
        $cmakeOutput = & cmake @cmakeArgs 2>&1 | Out-String
        $cmakeExitCode = $LASTEXITCODE
    }
    finally
    {
        $ErrorActionPreference = $oldErrorAction
    }
    
    # Check if configuration actually succeeded by looking for success indicators
    $configSuccess = $false
    if ($cmakeOutput -match "Configuring done" -or $cmakeOutput -match "Generating done")
    {
        $configSuccess = $true
    }
    elseif (Test-Path "CMakeCache.txt")
    {
        # CMakeCache.txt exists, configuration likely succeeded
        $configSuccess = $true
    }
    
    if (-not $configSuccess)
    {
        Write-Host "  CMake configuration failed!" -ForegroundColor Red
        Write-Host ""
        Write-Host "CMake Output:" -ForegroundColor Yellow
        Write-Host $cmakeOutput
        Write-Host ""
        Write-Host "Troubleshooting:" -ForegroundColor Cyan
        Write-Host "  1. Ensure RL library is installed at: $RLInstallPath"
        Write-Host "  2. Check that Boost is available (required by RL library)"
        Write-Host "  3. Try setting BOOST_ROOT manually: -DBOOST_ROOT=`"C:\path\to\boost`""
        Write-Host "  4. Verify CMake can find RL config: $rlConfigPathAlt or $rlConfigPath"
        exit 1
    }
    
    # Show warnings if any, but don't fail
    $warnings = $cmakeOutput | Select-String -Pattern "CMake Warning" -Context 0,1
    if ($warnings)
    {
        Write-Host "  CMake configuration completed with warnings (non-fatal):" -ForegroundColor Yellow
        $warnings | ForEach-Object { Write-Host "    $($_.Line)" -ForegroundColor Gray }
    }
    
    Write-Host "  CMake configuration successful" -ForegroundColor Green
}
finally
{
    Pop-Location
}

# Detect OS (do this before building)
$isWindows = $false
$isLinux = $false
$isMacOS = $false

if ($env:OS -eq "Windows_NT" -or $PSVersionTable.Platform -eq "Win32NT")
{
    $isWindows = $true
}
else
{
    # Try to detect Linux/macOS using uname if available
    try
    {
        $unameOutput = & uname 2>$null
        if ($unameOutput -eq "Linux") { $isLinux = $true }
        elseif ($unameOutput -eq "Darwin") { $isMacOS = $true }
    }
    catch
    {
        # If uname not available, assume Windows-like environment
        $isWindows = $true
    }
}

# Build the project
Write-Host ""
Write-Host "Building RLWrapper..." -ForegroundColor Cyan

Push-Location $buildDir
try
{
    if ($isWindows)
    {
        # Windows: use --config flag
        $buildOutput = & cmake --build . --config $BuildType 2>&1
    }
    else
    {
        # Linux/macOS: use -j for parallel build
        $cpuCount = 4
        if (Get-Command nproc -ErrorAction SilentlyContinue)
        {
            $cpuCount = (nproc)
        }
        elseif (Get-Command sysctl -ErrorAction SilentlyContinue)
        {
            $cpuCount = (sysctl -n hw.ncpu)
        }
        $buildOutput = & cmake --build . -j $cpuCount 2>&1
    }
    
    $buildExitCode = $LASTEXITCODE
    
    if ($buildExitCode -ne 0)
    {
        Write-Host "  Build failed!" -ForegroundColor Red
        Write-Host $buildOutput
        exit $buildExitCode
    }
    
    Write-Host "  Build successful" -ForegroundColor Green
}
finally
{
    Pop-Location
}

# Locate the output file
Write-Host ""
Write-Host "Locating output file..." -ForegroundColor Cyan

$outputFile = $null
if ($isWindows)
{
    # Windows: DLL in Release or Debug subdirectory
    $outputFile = Join-Path $buildDir "$BuildType\RLWrapper.dll"
}
elseif ($isLinux)
{
    # Linux: .so file
    $outputFile = Join-Path $buildDir "libRLWrapper.so"
}
elseif ($isMacOS)
{
    # macOS: .dylib file
    $outputFile = Join-Path $buildDir "libRLWrapper.dylib"
}

if (-not $outputFile -or -not (Test-Path $outputFile))
{
    Write-Host "  Warning: Output file not found at expected location: $outputFile" -ForegroundColor Yellow
    Write-Host "  Searching build directory..." -ForegroundColor Yellow
    
    # Try to find the file
    if ($isWindows)
    {
        $foundFiles = Get-ChildItem -Path $buildDir -Filter "RLWrapper.dll" -Recurse -ErrorAction SilentlyContinue
    }
    elseif ($isLinux)
    {
        $foundFiles = Get-ChildItem -Path $buildDir -Filter "libRLWrapper.so" -Recurse -ErrorAction SilentlyContinue
    }
    elseif ($isMacOS)
    {
        $foundFiles = Get-ChildItem -Path $buildDir -Filter "libRLWrapper.dylib" -Recurse -ErrorAction SilentlyContinue
    }
    
    if ($foundFiles -and $foundFiles.Count -gt 0)
    {
        $outputFile = $foundFiles[0].FullName
        Write-Host "  Found: $outputFile" -ForegroundColor Green
    }
    else
    {
        Write-Host "  Error: Could not locate output file" -ForegroundColor Red
        exit 1
    }
}
else
{
    Write-Host "  Output file: $outputFile" -ForegroundColor Green
}

# Copy to RLlib if requested
if ($CopyToRLlib)
{
    Write-Host ""
    Write-Host "Copying to RLlib folder..." -ForegroundColor Cyan
    
    # Determine platform folder
    $platformFolder = ""
    if ($isWindows)
    {
        $platformFolder = "Windows"
    }
    elseif ($isLinux)
    {
        $platformFolder = "Linux"
    }
    elseif ($isMacOS)
    {
        $platformFolder = "macOS"
    }
    else
    {
        Write-Host "  Warning: Unknown platform, skipping copy" -ForegroundColor Yellow
        $CopyToRLlib = $false
    }
    
    if ($CopyToRLlib)
    {
        $targetDir = Join-Path $rlLibPath $platformFolder
        if (-not (Test-Path $targetDir))
        {
            Write-Host "  Creating target directory: $targetDir" -ForegroundColor Gray
            New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
        }
        
        $targetFile = Join-Path $targetDir (Split-Path -Leaf $outputFile)
        Copy-Item -Path $outputFile -Destination $targetFile -Force
        Write-Host "  Copied to: $targetFile" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "Build Summary" -ForegroundColor Cyan
Write-Host "============" -ForegroundColor Cyan
Write-Host "  Build type:    $BuildType"
Write-Host "  Output file:   $outputFile"
if ($CopyToRLlib)
{
    Write-Host "  Copied to:     $targetFile"
}
Write-Host ""
Write-Host "Done! RLWrapper built successfully." -ForegroundColor Green
Write-Host ""
