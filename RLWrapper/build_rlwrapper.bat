@echo off
REM Batch file wrapper to run build_rlwrapper.ps1 with execution policy bypass
REM Builds the RLWrapper native library using CMake
REM Usage: build_rlwrapper.bat [parameters]
REM   Parameters are passed through to build_rlwrapper.ps1:
REM     -RLInstallPath "C:\path\to\rl\install"
REM     -BuildType Release
REM     -CopyToRLlib
REM     -Clean
REM     -BoostRoot "C:\path\to\boost"

set SCRIPT_DIR=%~dp0
set LOG_DIR=%SCRIPT_DIR%logs
if not exist "%LOG_DIR%" mkdir "%LOG_DIR%"

REM Redirect output to log file while also displaying on console using PowerShell Tee-Object
REM Generate timestamp and log file path entirely within PowerShell for reliability
REM Pass all batch file arguments through to the PowerShell script
PowerShell -ExecutionPolicy Bypass -Command "& { $ErrorActionPreference='Continue'; $logDir = '%LOG_DIR%'; $timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'; $logFile = Join-Path $logDir \"build-rlwrapper-batch-$timestamp.log\"; Write-Host \"Batch log: $logFile\"; $scriptArgs = $args; & '%SCRIPT_DIR%build_rlwrapper.ps1' @scriptArgs *>&1 | Tee-Object -FilePath $logFile; $exitCode = $LASTEXITCODE; exit $exitCode }" %*

set EXIT_CODE=%ERRORLEVEL%

if %EXIT_CODE% NEQ 0 (
    echo.
    echo Script failed with error code %EXIT_CODE%
    echo Build log saved to: %LOG_DIR%
    pause
    exit /b %EXIT_CODE%
)

echo.
echo Build log saved to: %LOG_DIR%
pause
