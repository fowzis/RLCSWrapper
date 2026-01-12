# RLTrajectoryPlanner Build Guide

This document provides detailed step-by-step instructions for building all components of the RLTrajectoryPlanner project from source.

## Table of Contents

1. [Project Overview](#project-overview)
2. [Prerequisites](#prerequisites)
3. [Build Order](#build-order)
4. [Step 1: Build RL Library](#step-1-build-rl-library)
5. [Step 2: Build RLWrapper Native Library](#step-2-build-rlwrapper-native-library)
6. [Step 3: Copy Native Libraries](#step-3-copy-native-libraries)
7. [Step 4: Build C# Projects](#step-4-build-c-projects)
8. [Step 5: Run Tests](#step-5-run-tests)
9. [Troubleshooting](#troubleshooting)

---

## Project Overview

The RLTrajectoryPlanner project consists of three main components:

1. **RL Library** (External Dependency)
   - C++ robotics library providing kinematics, dynamics, and planning algorithms
   - Must be built separately from the `rl` repository
   - Produces native libraries: `rlplan.dll`, `rlkin.dll`, `rlsg.dll`, `rlmdl.dll`, `rlhal.dll`, etc.

2. **RLWrapper** (C++ Native Wrapper)
   - C-compatible wrapper DLL around the RL library
   - Provides simplified C API for trajectory planning
   - Built using CMake
   - Produces: `RLWrapper.dll` (Windows), `libRLWrapper.so` (Linux), `libRLWrapper.dylib` (macOS)

3. **RLTrajectoryPlanner.Core** (C# .NET Library)
   - .NET 10 wrapper library providing managed API
   - Uses P/Invoke to call RLWrapper functions
   - Produces: `RLTrajectoryPlanner.Core.dll`

4. **RLTrajectoryPlanner.Test** (C# Test Application)
   - Console application for testing the wrapper
   - Produces: `RLTrajectoryPlanner.Test.exe`

---

## Prerequisites

### Required Software

#### Windows

1. **Visual Studio 2022** (or Visual Studio Build Tools 2022)
   - **Required Workloads:**
     - Desktop development with C++
   - **Required Components:**
     - MSVC v143 - VS 2022 C++ x64/x86 build tools
     - Windows 10/11 SDK (latest version)
     - CMake tools for Visual Studio

2. **CMake** (3.10 or higher)
   - Download from: https://cmake.org/download/
   - Ensure CMake is added to system PATH
   - Verify installation: `cmake --version`

3. **.NET 10 SDK**
   - Download from: https://dotnet.microsoft.com/download
   - Verify installation: `dotnet --version`

4. **Git** (for cloning repositories)
   - Download from: https://git-scm.com/download/win

#### Linux

1. **Build Tools**
   ```bash
   sudo apt-get update
   sudo apt-get install -y build-essential cmake git
   ```

2. **.NET 10 SDK**
   - Follow instructions at: https://dotnet.microsoft.com/download/linux

3. **RL Library Dependencies**
   - See RL library build guide for complete list
   - Typically includes: Boost, Eigen3, libxml2, collision detection libraries

#### macOS

1. **Xcode Command Line Tools**
   ```bash
   xcode-select --install
   ```

2. **CMake**
   ```bash
   brew install cmake
   ```

3. **.NET 10 SDK**
   - Follow instructions at: https://dotnet.microsoft.com/download/macos

---

## Build Order

The components must be built in the following order:

1. **RL Library** (if not already built)
2. **RLWrapper** (depends on RL Library)
3. **Copy Native Libraries** (organize DLLs/so files)
4. **RLTrajectoryPlanner.Core** (depends on RLWrapper)
5. **RLTrajectoryPlanner.Test** (depends on Core)

---

## Step 1: Build RL Library

The RL Library must be built first as it is a dependency for RLWrapper.

### Windows

1. **Clone or locate the RL repository:**
   ```powershell
   # If cloning:
   git clone <rl-repository-url>
   cd rl
   ```

2. **Create build directory:**
   ```powershell
   mkdir build
   cd build
   ```

3. **Configure with CMake:**
   ```powershell
   cmake .. -DCMAKE_BUILD_TYPE=Release -DCMAKE_INSTALL_PREFIX=../install
   ```

   **Important CMake Options:**
   - `-DCMAKE_BUILD_TYPE=Release` - Build optimized release version
   - `-DCMAKE_INSTALL_PREFIX=../install` - Set install directory
   - `-DRL_SG_BULLET=ON` - Enable Bullet collision detection (optional)
   - `-DRL_SG_FCL=ON` - Enable FCL collision detection (optional)
   - `-DRL_SG_PQP=ON` - Enable PQP collision detection (optional)
   - `-DRL_SG_ODE=ON` - Enable ODE collision detection (optional)

4. **Build:**
   ```powershell
   cmake --build . --config Release
   ```

5. **Install:**
   ```powershell
   cmake --install . --config Release
   ```

   This will create an `install` directory with:
   - `install/bin/` - Contains all RL DLLs
   - `install/lib/` - Contains import libraries
   - `install/include/` - Contains headers

### Linux/macOS

1. **Create build directory:**
   ```bash
   mkdir build
   cd build
   ```

2. **Configure with CMake:**
   ```bash
   cmake .. -DCMAKE_BUILD_TYPE=Release -DCMAKE_INSTALL_PREFIX=../install
   ```

3. **Build:**
   ```bash
   cmake --build . -j$(nproc)
   ```

4. **Install:**
   ```bash
   cmake --install .
   ```

**Note:** For detailed RL library build instructions, refer to the RL library's BUILD_GUIDE.md or documentation.

---

## Step 2: Build RLWrapper Native Library

The RLWrapper is a C++ wrapper that provides a C-compatible API for the RL library.

### Windows

1. **Navigate to RLWrapper directory:**
   ```powershell
   cd C:\Tools\RoboticLibrary\GitHub\RLTrajectoryPlanner\RLWrapper
   ```

2. **Create build directory:**
   ```powershell
   mkdir build
   cd build
   ```

3. **Configure with CMake:**
   
   You need to tell CMake where to find the RL library. If RL was installed to a custom location:
   ```powershell
   cmake .. -DCMAKE_BUILD_TYPE=Release -Drl_DIR="C:\path\to\rl\install\lib\cmake\rl" -DCMAKE_PREFIX_PATH="C:\path\to\rl\install"
   ```

   If RL is installed in a standard location or CMake can find it:
   ```powershell
   cmake .. -DCMAKE_BUILD_TYPE=Release
   ```

   **CMake will automatically:**
   - Find the RL library using `find_package(rl REQUIRED)`
   - Detect available collision detection engines (Bullet, FCL, ODE, PQP, SOLID3)
   - Configure the build accordingly

4. **Build:**
   ```powershell
   cmake --build . --config Release
   ```

5. **Locate the output:**
   - **Release build:** `build/Release/RLWrapper.dll`
   - **Debug build:** `build/Debug/RLWrapper.dll`

### Linux

1. **Navigate to RLWrapper directory:**
   ```bash
   cd RLTrajectoryPlanner/RLWrapper
   ```

2. **Create build directory:**
   ```bash
   mkdir build
   cd build
   ```

3. **Configure with CMake:**
   ```bash
   cmake .. -DCMAKE_BUILD_TYPE=Release -Drl_DIR=/path/to/rl/install/lib/cmake/rl
   ```

4. **Build:**
   ```bash
   cmake --build . -j$(nproc)
   ```

5. **Locate the output:**
   - `build/libRLWrapper.so`

### macOS

1. **Navigate to RLWrapper directory:**
   ```bash
   cd RLTrajectoryPlanner/RLWrapper
   ```

2. **Create build directory:**
   ```bash
   mkdir build
   cd build
   ```

3. **Configure with CMake:**
   ```bash
   cmake .. -DCMAKE_BUILD_TYPE=Release -Drl_DIR=/path/to/rl/install/lib/cmake/rl
   ```

4. **Build:**
   ```bash
   cmake --build . -j$(sysctl -n hw.ncpu)
   ```

5. **Locate the output:**
   - `build/libRLWrapper.dylib`

### Troubleshooting RLWrapper Build

**Problem: CMake cannot find RL library**

**Solution:**
- Set `rl_DIR` to point to the RL CMake config directory:
  ```powershell
  cmake .. -Drl_DIR="C:\path\to\rl\install\lib\cmake\rl"
  ```
- Or set `CMAKE_PREFIX_PATH`:
  ```powershell
  cmake .. -DCMAKE_PREFIX_PATH="C:\path\to\rl\install"
  ```

**Problem: Missing collision detection engine**

**Solution:**
- The wrapper will use the first available engine (FCL > ODE > PQP > Bullet > SOLID3)
- Ensure at least one collision detection library is built with RL
- Check RL build configuration for enabled engines

---

## Step 3: Copy Native Libraries

After building RLWrapper, you need to copy all native libraries to the `RLlib` folder structure for the C# projects to find them.

### Windows

1. **Copy RLWrapper DLL:**
   ```powershell
   # From RLWrapper build directory
   Copy-Item "build\Release\RLWrapper.dll" -Destination "RLlib\Windows\RLWrapper.dll"
   ```

2. **Copy RL libraries using the provided script:**
   
   **Option A: From project root**
   ```powershell
   cd C:\Tools\RoboticLibrary\GitHub\RLTrajectoryPlanner
   .\copy_rl_libraries.ps1 -RLInstallPath "C:\path\to\rl\install" -IncludeDependencies
   ```

   **Option B: From RLlib directory**
   ```powershell
   cd RLlib
   .\copy_rl_libraries.ps1 -RLInstallPath "C:\path\to\rl\install" -IncludeDependencies
   ```

   The script will:
   - Copy all `rl*.dll` files from `install/bin` to `RLlib/Windows/`
   - Optionally copy dependencies (VC++ runtime, Boost DLLs, etc.) with `-IncludeDependencies`

3. **Verify files are present:**
   ```powershell
   ls RLlib\Windows\
   ```
   
   You should see:
   - `RLWrapper.dll`
   - `rlplan.dll`
   - `rlkin.dll`
   - `rlsg.dll`
   - `rlmdl.dll`
   - `rlhal.dll`
   - `vcruntime140.dll` (if dependencies included)
   - `msvcp140.dll` (if dependencies included)
   - Other dependency DLLs

### Linux

1. **Copy RLWrapper shared library:**
   ```bash
   cp RLWrapper/build/libRLWrapper.so RLlib/Linux/
   ```

2. **Copy RL libraries:**
   ```bash
   cd RLlib
   chmod +x copy_rl_libraries.sh
   ./copy_rl_libraries.sh /path/to/rl/install --include-dependencies
   ```

3. **Verify files:**
   ```bash
   ls -la RLlib/Linux/
   ```

### macOS

1. **Copy RLWrapper dynamic library:**
   ```bash
   cp RLWrapper/build/libRLWrapper.dylib RLlib/macOS/
   ```

2. **Copy RL libraries:**
   ```bash
   cd RLlib
   chmod +x copy_rl_libraries.sh
   ./copy_rl_libraries.sh /path/to/rl/install --include-dependencies
   ```

3. **Verify files:**
   ```bash
   ls -la RLlib/macOS/
   ```

---

## Step 4: Build C# Projects

Once all native libraries are in place, build the C# solution.

### Windows/Linux/macOS

1. **Navigate to project root:**
   ```powershell
   # Windows PowerShell
   cd C:\Tools\RoboticLibrary\GitHub\RLTrajectoryPlanner
   ```
   ```bash
   # Linux/macOS
   cd RLTrajectoryPlanner
   ```

2. **Restore NuGet packages (if any):**
   ```bash
   dotnet restore RLTrajectoryPlanner.sln
   ```

3. **Build the solution:**
   ```bash
   dotnet build RLTrajectoryPlanner.sln --configuration Release
   ```

   **Build options:**
   - `--configuration Release` - Build optimized release version
   - `--configuration Debug` - Build debug version with symbols
   - `-p:Platform=x64` - Build for x64 platform (Windows)

4. **Verify build output:**
   
   **Windows:**
   ```powershell
   ls RLTrajectoryPlanner.Core\bin\Release\net10.0\
   ls RLTrajectoryPlanner.Test\bin\Release\net10.0\
   ```

   **Linux/macOS:**
   ```bash
   ls RLTrajectoryPlanner.Core/bin/Release/net10.0/
   ls RLTrajectoryPlanner.Test/bin/Release/net10.0/
   ```

   You should see:
   - `RLTrajectoryPlanner.Core.dll`
   - `RLTrajectoryPlanner.Core.pdb` (debug symbols)
   - `RLTrajectoryPlanner.Test.dll` or `.exe`
   - `RLTrajectoryPlanner.Test.pdb`

### Build Output Structure

After building, the output structure should be:

```
RLTrajectoryPlanner/
├── RLlib/
│   ├── Windows/          # Native DLLs for Windows
│   ├── Linux/            # Native .so files for Linux
│   └── macOS/           # Native .dylib files for macOS
├── RLTrajectoryPlanner.Core/
│   └── bin/
│       └── Release/
│           └── net10.0/
│               └── RLTrajectoryPlanner.Core.dll
└── RLTrajectoryPlanner.Test/
    └── bin/
        └── Release/
            └── net10.0/
                └── RLTrajectoryPlanner.Test.exe
```

---

## Step 5: Run Tests

Test the built wrapper with the test application.

### Prerequisites

You need robot XML files for testing:
- **Kinematics XML:** Defines robot structure (joints, links, transformations)
- **Scene XML:** Defines robot model and obstacles in the workspace

### Running Tests

1. **Navigate to test output directory:**
   ```powershell
   # Windows
   cd RLTrajectoryPlanner.Test\bin\Release\net10.0
   ```
   ```bash
   # Linux/macOS
   cd RLTrajectoryPlanner.Test/bin/Release/net10.0
   ```

2. **Ensure native libraries are accessible:**
   
   The test application looks for native libraries relative to the executable. Ensure the `RLlib` folder structure is accessible:
   
   **Windows:**
   - Copy `RLlib/Windows/*.dll` to the test output directory, OR
   - Ensure `RLlib/Windows/` is in the PATH, OR
   - Place DLLs in the same directory as the executable

   **Linux/macOS:**
   - Set `LD_LIBRARY_PATH` (Linux) or `DYLD_LIBRARY_PATH` (macOS):
     ```bash
     export LD_LIBRARY_PATH=$PWD/../../../../RLlib/Linux:$LD_LIBRARY_PATH
     ```

3. **Run the test:**
   ```bash
   # With XML files in current directory
   dotnet RLTrajectoryPlanner.Test.dll scara_robot.xml workspace.xml
   
   # Or specify full paths
   dotnet RLTrajectoryPlanner.Test.dll "C:\path\to\scara_robot.xml" "C:\path\to\workspace.xml"
   ```

### Expected Output

The test should:
1. Initialize the planner successfully
2. Display robot DOF
3. Test configuration validation
4. Plan trajectories (2D and 3D)
5. Test different planner algorithms
6. Demonstrate scene reuse

---

## Troubleshooting

### Build Issues

#### CMake Cannot Find RL Library

**Symptoms:**
```
CMake Error: Could not find a package configuration file provided by "rl"
```

**Solutions:**
1. Set `rl_DIR` explicitly:
   ```powershell
   cmake .. -Drl_DIR="C:\path\to\rl\install\lib\cmake\rl"
   ```
2. Set `CMAKE_PREFIX_PATH`:
   ```powershell
   cmake .. -DCMAKE_PREFIX_PATH="C:\path\to\rl\install"
   ```
3. Verify RL was installed correctly:
   ```powershell
   Test-Path "C:\path\to\rl\install\lib\cmake\rl\rlConfig.cmake"
   ```

#### Missing Collision Detection Engine

**Symptoms:**
```
No collision detection engine available
```

**Solutions:**
1. Rebuild RL library with at least one collision detection backend enabled
2. Common backends: FCL, ODE, PQP, Bullet, SOLID3
3. Check RL CMake configuration for enabled engines

#### C# Build Fails - Native Library Not Found

**Symptoms:**
```
DllNotFoundException: Failed to load native library 'RLWrapper.dll'
```

**Solutions:**
1. Verify `RLWrapper.dll` exists in `RLlib/Windows/`
2. Verify all RL DLLs are present in `RLlib/Windows/`
3. Check that DLLs match the build architecture (x64/x86)
4. Ensure DLLs are accessible from the application directory

#### Runtime Errors - Missing Dependencies

**Symptoms:**
```
The program can't start because VCRUNTIME140.dll is missing
```

**Solutions:**
1. Use `-IncludeDependencies` flag when copying libraries:
   ```powershell
   .\copy_rl_libraries.ps1 -RLInstallPath "C:\path\to\rl\install" -IncludeDependencies
   ```
2. Install Visual C++ Redistributable:
   - Download from Microsoft
   - Install both x64 and x86 versions if needed
3. Copy missing DLLs manually from RL install directory

### Runtime Issues

#### Planning Always Fails

**Possible Causes:**
1. Invalid start/goal configurations (outside joint limits)
2. Scene obstacles blocking all paths
3. Planner parameters too restrictive (delta, epsilon, timeout)
4. Invalid XML files

**Solutions:**
1. Verify configurations are within joint limits:
   ```csharp
   bool isValid = planner.IsValidConfiguration(startConfig);
   ```
2. Check XML file validity
3. Try different planner algorithms
4. Increase timeout and adjust delta/epsilon parameters

#### Library Loading Fails at Runtime

**Symptoms:**
```
PlanningException: Failed to load native library 'RLWrapper.dll'
```

**Solutions:**
1. **Windows:**
   - Ensure DLLs are in the same directory as the executable
   - Or add `RLlib/Windows/` to PATH
   - Check for architecture mismatch (x64 vs x86)

2. **Linux:**
   - Set `LD_LIBRARY_PATH`:
     ```bash
     export LD_LIBRARY_PATH=/path/to/RLlib/Linux:$LD_LIBRARY_PATH
     ```
   - Or use `rpath` when building

3. **macOS:**
   - Set `DYLD_LIBRARY_PATH`:
     ```bash
     export DYLD_LIBRARY_PATH=/path/to/RLlib/macOS:$DYLD_LIBRARY_PATH
     ```

### Architecture Mismatch

**Symptoms:**
```
BadImageFormatException: An attempt was made to load a program with an incorrect format
```

**Solutions:**
1. Ensure all components are built for the same architecture (x64 or x86)
2. Check .NET application target platform matches native libraries
3. Rebuild all components for the same architecture

---

## Quick Build Checklist

Use this checklist to ensure all steps are completed:

- [ ] RL Library built and installed
- [ ] RLWrapper configured with CMake (found RL library)
- [ ] RLWrapper built successfully
- [ ] RLWrapper DLL/so copied to `RLlib/[Platform]/`
- [ ] RL libraries copied to `RLlib/[Platform]/` using script
- [ ] Dependencies copied (if needed)
- [ ] C# solution restored (`dotnet restore`)
- [ ] C# solution built successfully
- [ ] Test application runs without errors
- [ ] Native libraries accessible at runtime

---

## Additional Resources

- **RL Library Documentation:** See `rl` repository documentation
- **RL Library Build Guide:** See `rl-3rdparty/BUILD_GUIDE.md`
- **API Documentation:** See `Documentation/API.md`
- **Interoperability Guide:** See `Documentation/INTEROPERABILITY.md`

---

## Build Summary

After completing all steps, you should have:

1. ✅ RL Library installed (native DLLs/so files)
2. ✅ RLWrapper built (RLWrapper.dll/libRLWrapper.so)
3. ✅ All native libraries organized in `RLlib/[Platform]/`
4. ✅ `RLTrajectoryPlanner.Core.dll` built
5. ✅ `RLTrajectoryPlanner.Test.exe` built and tested

The project is now ready to use or integrate into other .NET solutions!
