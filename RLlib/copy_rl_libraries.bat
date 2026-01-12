@echo off
REM Batch file wrapper to run copy_rl_libraries.ps1 with execution policy bypass
REM Copies Robotics Library (RL) libraries from RL install directory to platform-specific folders
REM Usage: copy_rl_libraries.bat [parameters]
REM   Parameters are passed through to copy_rl_libraries.ps1:
REM     -RLInstallPath "C:\path\to\rl\install"
REM     -IncludeDependencies

set SCRIPT_DIR=%~dp0
set LOG_DIR=%SCRIPT_DIR%logs
if not exist "%LOG_DIR%" mkdir "%LOG_DIR%"

REM Redirect output to log file while also displaying on console using PowerShell Tee-Object
REM Generate timestamp and log file path entirely within PowerShell for reliability
REM Pass all batch file arguments through to the PowerShell script
PowerShell -ExecutionPolicy Bypass -Command "& { $ErrorActionPreference='Continue'; $logDir = '%LOG_DIR%'; $timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'; $logFile = Join-Path $logDir \"copy-rl-libraries-batch-$timestamp.log\"; Write-Host \"Batch log: $logFile\"; $scriptArgs = $args; & '%SCRIPT_DIR%copy_rl_libraries.ps1' @scriptArgs *>&1 | Tee-Object -FilePath $logFile; $exitCode = $LASTEXITCODE; exit $exitCode }" %*

set EXIT_CODE=%ERRORLEVEL%

if %EXIT_CODE% NEQ 0 (
    echo.
    echo Script failed with error code %EXIT_CODE%
    echo Copy log saved to: %LOG_DIR%
    pause
    exit /b %EXIT_CODE%
)

echo.
echo Copy log saved to: %LOG_DIR%
pause
