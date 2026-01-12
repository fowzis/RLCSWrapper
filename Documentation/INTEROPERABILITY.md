# Interoperability Guide

This document describes the P/Invoke implementation and native library interoperability details.

## Architecture Overview

The wrapper uses a three-layer architecture:

1. **C++ Wrapper DLL** (`RLWrapper`): Exports C-compatible functions wrapping RL library calls
2. **C# P/Invoke Layer** (`RLWrapper.cs`): Declares native functions and handles marshalling
3. **C# API Layer** (`TrajectoryPlanner.cs`): Provides high-level managed API

## Native Library Structure

### RLWrapper DLL

The C++ wrapper (`RLWrapper.dll` / `libRLWrapper.so` / `libRLWrapper.dylib`) exports the following C-compatible functions:

```cpp
void* CreatePlanner();
int LoadKinematics(void* planner, const char* xmlPath);
int LoadScene(void* planner, const char* xmlPath, int robotModelIndex);
int PlanTrajectory(void* planner, double* start, int startSize, ...);
int IsValidConfiguration(void* planner, double* config, int configSize);
int GetDof(void* planner);
void DestroyPlanner(void* planner);
```

### Error Codes

The wrapper uses integer error codes:

- `0` (RL_SUCCESS): Success
- `-1` (RL_ERROR_INVALID_POINTER): Invalid pointer parameter
- `-2` (RL_ERROR_INVALID_PARAMETER): Invalid parameter
- `-3` (RL_ERROR_LOAD_FAILED): Failed to load kinematics/scene
- `-4` (RL_ERROR_PLANNING_FAILED): Planning failed
- `-5` (RL_ERROR_NOT_INITIALIZED): Planner not initialized
- `-6` (RL_ERROR_EXCEPTION): Exception in native code

## P/Invoke Declarations

### Function Signatures

```csharp
[DllImport("RLWrapper", CallingConvention = CallingConvention.Cdecl)]
private static extern IntPtr CreatePlannerNative();

[DllImport("RLWrapper", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
private static extern int LoadKinematicsNative(IntPtr planner, [MarshalAs(UnmanagedType.LPStr)] string xmlPath);

[DllImport("RLWrapper", CallingConvention = CallingConvention.Cdecl)]
private static extern int PlanTrajectoryNative(
    IntPtr planner,
    [MarshalAs(UnmanagedType.LPArray)] double[] start, int startSize,
    [MarshalAs(UnmanagedType.LPArray)] double[] goal, int goalSize,
    int useZAxis, [MarshalAs(UnmanagedType.LPStr)] string plannerType,
    double delta, double epsilon, int timeoutMs,
    [MarshalAs(UnmanagedType.LPArray)] double[] waypoints, int maxWaypoints, out int waypointCount);
```

### Key P/Invoke Attributes

- **`CallingConvention.Cdecl`**: C calling convention (required for C-compatible exports)
- **`CharSet.Ansi`**: ANSI string marshalling for `const char*` parameters
- **`UnmanagedType.LPStr`**: Marshals strings as ANSI null-terminated strings
- **`UnmanagedType.LPArray`**: Marshals arrays as pointers to unmanaged arrays

## Platform-Specific Library Loading

### Windows

- Library name: `RLWrapper.dll`
- Search paths:
  1. `RLlib/Windows/RLWrapper.dll`
  2. Application directory
  3. System PATH

### Linux

- Library name: `libRLWrapper.so`
- Search paths:
  1. `RLlib/Linux/libRLWrapper.so`
  2. `LD_LIBRARY_PATH`
  3. System library paths (`/usr/lib`, `/lib`)

### macOS

- Library name: `libRLWrapper.dylib`
- Search paths:
  1. `RLlib/macOS/libRLWrapper.dylib`
  2. `DYLD_LIBRARY_PATH`
  3. System library paths

### Implementation

The wrapper uses `NativeLibrary.Load()` (.NET 5+) for cross-platform library loading:

```csharp
string libraryName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "RLWrapper.dll" :
                    RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "libRLWrapper.so" :
                    "libRLWrapper.dylib";

string libraryPath = Path.Combine("RLlib", platformFolder, libraryName);
NativeLibrary.Load(libraryPath);
```

## Memory Management

### Opaque Pointers

The C++ wrapper uses opaque pointers (`void*`) to hide internal state:

```cpp
struct PlannerState {
    std::shared_ptr<rl::sg::Scene> scene;
    std::shared_ptr<rl::kin::Kinematics> kinematics;
    // ...
};

void* CreatePlanner() {
    return new PlannerState();
}
```

In C#, these are represented as `IntPtr`:

```csharp
IntPtr plannerHandle = RLWrapper.CreatePlanner();
// ... use plannerHandle ...
RLWrapper.DestroyPlanner(plannerHandle);
```

### Array Marshalling

Arrays are marshalled as pointers:

```cpp
// C++: double* waypoints, int maxWaypoints, int* waypointCount
int PlanTrajectory(..., double* waypoints, int maxWaypoints, int* waypointCount);
```

```csharp
// C#: double[] waypoints, int maxWaypoints, out int waypointCount
int PlanTrajectoryNative(..., [MarshalAs(UnmanagedType.LPArray)] double[] waypoints, 
                         int maxWaypoints, out int waypointCount);
```

The P/Invoke runtime automatically:
- Pins the managed array
- Passes a pointer to the native code
- Unpins after the call returns

### Lifetime Management

- **Creation**: `CreatePlanner()` allocates state on the heap
- **Usage**: State persists between calls
- **Destruction**: `DestroyPlanner()` deallocates state

The C# wrapper implements `IDisposable` to ensure cleanup:

```csharp
public void Dispose()
{
    if (_plannerHandle != IntPtr.Zero)
    {
        RLWrapper.DestroyPlanner(_plannerHandle);
        _plannerHandle = IntPtr.Zero;
    }
}
```

## Exception Handling

### C++ Exceptions

C++ exceptions are caught and converted to error codes:

```cpp
try {
    // ... operation ...
    return RL_SUCCESS;
}
catch (const std::exception&) {
    return RL_ERROR_EXCEPTION;
}
```

### C# Exception Conversion

Error codes are converted to C# exceptions:

```csharp
private static void ThrowOnError(int errorCode, string operation)
{
    if (errorCode == RL_SUCCESS) return;
    
    string errorMessage = errorCode switch
    {
        RL_ERROR_INVALID_POINTER => "Invalid pointer parameter",
        RL_ERROR_PLANNING_FAILED => "Trajectory planning failed",
        // ...
    };
    
    throw new PlanningException($"{operation} failed: {errorMessage}");
}
```

## Thread Safety

### Native Code

The C++ wrapper is **not** thread-safe. Each planner instance should be used from a single thread, or external synchronization is required.

### Managed Code

The C# `TrajectoryPlanner` singleton uses locks to ensure thread-safe access:

```csharp
private readonly object _lockObject = new object();

public PlanningResult PlanTrajectory(PlanningRequest request)
{
    lock (_lockObject)
    {
        // ... thread-safe operations ...
    }
}
```

## Building the Native Wrapper

### Windows (Visual Studio)

```bash
cd RLWrapper
mkdir build
cd build
cmake .. -G "Visual Studio 17 2022" -A x64
cmake --build . --config Release
```

### Linux

```bash
cd RLWrapper
mkdir build
cd build
cmake .. -DCMAKE_BUILD_TYPE=Release
make
```

### macOS

```bash
cd RLWrapper
mkdir build
cd build
cmake .. -DCMAKE_BUILD_TYPE=Release
make
```

## Dependencies

### Required RL Libraries

- `rlplan`: Planning algorithms
- `rlkin`: Kinematics
- `rlsg`: Scene graph (collision detection)
- `rlmath`: Math utilities
- `rlxml`: XML parsing
- `rlutil`: Utilities

### Native Dependencies

- **Boost**: C++ libraries
- **libxml2**: XML parsing
- **Collision Detection**: PQP, ODE, Bullet, FCL, or SOLID3

## Troubleshooting

### DllNotFoundException

**Problem**: Native library not found.

**Solutions**:
1. Ensure library is in correct RLlib platform folder
2. Check library name matches platform convention
3. Verify all dependencies are available
4. Check architecture (x64/x86) matches

### AccessViolationException

**Problem**: Invalid pointer access.

**Solutions**:
1. Ensure planner is initialized before use
2. Verify pointer is not null before passing to native code
3. Check array sizes match expected DOF

### Planning Always Fails

**Problem**: Planning returns error code -4.

**Solutions**:
1. Verify start/goal configurations are valid
2. Check scene contains obstacles correctly
3. Ensure robot model index is correct
4. Try different planner algorithms
5. Increase timeout

## Performance Considerations

### Library Loading

- Library is loaded lazily on first use
- Loading is cached (subsequent calls reuse loaded library)
- Consider preloading if startup time is critical

### Memory Allocation

- Waypoints are allocated in managed memory
- Large trajectories may require significant memory
- Consider streaming for very large paths

### Planning Performance

- RRT-Connect is typically fastest
- PRM requires precomputation but faster queries
- Delta parameter affects planning time vs. path quality

