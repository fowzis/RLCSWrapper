# RLlib - Native Library Management

This directory contains platform-specific folders for RL library native binaries and the scripts to copy them from the RL project's `install/bin` directory.

## Directory Structure

```
RLlib/
├── Windows/          # Windows DLLs (rl*.dll)
├── Linux/            # Linux shared libraries (librl*.so)
├── macOS/            # macOS dynamic libraries (librl*.dylib)
├── copy_rl_libraries.ps1  # PowerShell script (Windows)
├── copy_rl_libraries.bat  # Batch file wrapper (Windows)
├── copy_rl_libraries.sh   # Bash script (Linux/macOS)
└── README.md         # This file
```

## Usage

### Windows (Batch File)

The batch file wrapper automatically handles PowerShell execution policy and logs output:

**From Command Prompt (cmd.exe):**
```batch
REM Copy RL libraries from default location (../rl/install)
copy_rl_libraries.bat

REM Copy RL libraries from specific path
copy_rl_libraries.bat -RLInstallPath "C:\path\to\rl\install"

REM Copy RL libraries and dependencies (VC++ runtime, Boost, etc.)
copy_rl_libraries.bat -RLInstallPath "C:\path\to\rl\install" -IncludeDependencies

REM Just include dependencies (uses auto-detected path)
copy_rl_libraries.bat -IncludeDependencies
```

**From PowerShell:**
```powershell
# Use .\ prefix or cmd /c to run batch files from PowerShell
.\copy_rl_libraries.bat
.\copy_rl_libraries.bat -RLInstallPath "C:\path\to\rl\install"
.\copy_rl_libraries.bat -IncludeDependencies

# Alternative: use cmd /c
cmd /c copy_rl_libraries.bat -IncludeDependencies
```

The batch file creates timestamped log files in the `logs` directory for troubleshooting.

### Windows (PowerShell)

You can also run the PowerShell script directly:

```powershell
# Copy RL libraries from default location (../rl/install)
.\copy_rl_libraries.ps1

# Copy RL libraries from specific path
.\copy_rl_libraries.ps1 -RLInstallPath "C:\path\to\rl\install"

# Copy RL libraries and dependencies (VC++ runtime, Boost, etc.)
.\copy_rl_libraries.ps1 -RLInstallPath "C:\path\to\rl\install" -IncludeDependencies
```

### Linux/macOS (Bash)

```bash
# Make script executable (first time only)
chmod +x copy_rl_libraries.sh

# Copy RL libraries from default location (../rl/install)
./copy_rl_libraries.sh

# Copy RL libraries from specific path
./copy_rl_libraries.sh /path/to/rl/install

# Copy RL libraries and dependencies
./copy_rl_libraries.sh /path/to/rl/install --include-dependencies
```

## What Gets Copied

### RL Library Files

The scripts automatically copy all RL library files matching these patterns:

- **Windows**: `rl*.dll` files (e.g., `rlplan.dll`, `rlkin.dll`, `rlsg.dll`, `rlhal.dll`, `rlmdl.dll`)
- **Linux**: `librl*.so*` files (e.g., `librlplan.so`, `librlkin.so`, `librlsg.so`)
- **macOS**: `librl*.dylib*` files (e.g., `librlplan.dylib`, `librlkin.dylib`, `librlsg.dylib`)

### Dependencies (Optional)

When using the `--include-dependencies` flag, the scripts also copy:

- **Visual C++ Runtime**: `msvcp*.dll`, `vcruntime*.dll`, `concrt*.dll`
- **Boost Libraries**: `boost_*.dll` (if present)
- **libxml2**: `*xml*.dll` (if present)

## Default Path Detection

If no path is specified, the scripts attempt to find the RL install directory relative to the project:

- Assumes `RLCSWrapper` and `rl` are sibling directories
- Looks for `../rl/install/bin` from the script location

## Example Output

```
Source directory: C:\Tools\RoboticLibrary\GitHub\rl\install\bin
Target directories:
  Windows: C:\Tools\RoboticLibrary\GitHub\RLCSWrapper\RLlib\Windows
  Linux:   C:\Tools\RoboticLibrary\GitHub\RLCSWrapper\RLlib\Linux
  macOS:   C:\Tools\RoboticLibrary\GitHub\RLCSWrapper\RLlib\macOS

Copying Windows libraries (rl*.dll)...
  Copied rlhal.dll
  Copied rlkin.dll
  Copied rlmdl.dll
  Copied rlplan.dll
  Copied rlsg.dll
  Copied 5 RL library file(s) to Windows folder

Done! Libraries copied successfully.
```

## Notes

- The scripts create the platform directories if they don't exist
- Existing files are overwritten (use `-Force` in PowerShell, default in bash)
- Linux/macOS files may not be present if building on Windows (this is normal)
- You may need to manually copy additional dependencies depending on your RL build configuration
- The batch file wrapper (`copy_rl_libraries.bat`) automatically handles PowerShell execution policy and creates log files in the `logs` directory

## Troubleshooting

### Script Not Found

If you get a "script not found" error:
- Ensure you're running the script from the `RLlib` directory
- Check that the script has execute permissions (Linux/macOS): `chmod +x copy_rl_libraries.sh`

### No Files Copied

- Verify the RL project has been built and installed
- Check that `install/bin` directory exists and contains the expected files
- On Windows, ensure you're looking in `install/bin`, not `build/bin` or `build/lib`

### Missing Dependencies

- Use the `--include-dependencies` flag to copy runtime dependencies
- You may need to manually copy additional DLLs/so files from your RL build's dependency directories
- Check the RL project's documentation for required dependencies
