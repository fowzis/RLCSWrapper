---
name: C# .NET 10 RL Library Wrapper
overview: Create a C# .NET 10 solution that wraps the RL (Robotic Library) C++ library using P/Invoke, providing a singleton trajectory planning service for SCARA robots with optional Z-axis planning and configurable planner algorithms.
todos:
  - id: setup_project_structure
    content: Create solution structure with Core library and Test project folders
    status: completed
  - id: create_cpp_wrapper
    content: Create C++ wrapper DLL with C-compatible exports for RL library functions
    status: completed
  - id: implement_pinvoke
    content: Implement C# P/Invoke declarations in RLWrapper.cs
    status: completed
    dependencies:
      - create_cpp_wrapper
  - id: create_models
    content: Create PlanningRequest, PlanningResult, and PlannerType models
    status: completed
  - id: implement_singleton
    content: Implement TrajectoryPlanner singleton class with one-time initialization (load kinematics and scene), lifetime management, and cleanup
    status: completed
    dependencies:
      - implement_pinvoke
      - create_models
  - id: implement_initialization
    content: Implement Initialize() method that loads kinematics and scene XML files once and stores them for lifetime reuse
    status: completed
    dependencies:
      - implement_singleton
  - id: implement_zaxis_logic
    content: Implement optional Z-axis handling for 2D/3D SCARA planning
    status: completed
    dependencies:
      - implement_singleton
  - id: implement_planner_selection
    content: Implement planner algorithm selection (RRT, RRT-Connect, PRM, etc.)
    status: completed
    dependencies:
      - implement_singleton
  - id: implement_planning_method
    content: Implement PlanTrajectory method that uses pre-loaded scene/kinematics, ensures collision-free trajectories, and returns waypoints
    status: completed
    dependencies:
      - implement_zaxis_logic
      - implement_planner_selection
      - implement_initialization
  - id: copy_rl_libraries
    content: Create cross-platform build scripts/targets to copy RL native libraries (DLL/SO/dylib) to platform-specific RLlib folders
    status: completed
  - id: implement_platform_detection
    content: Implement platform detection and native library loading for Windows/Linux/macOS
    status: completed
    dependencies:
      - implement_pinvoke
      - copy_rl_libraries
  - id: create_test_program
    content: Create test console application demonstrating all features
    status: completed
    dependencies:
      - implement_planning_method
  - id: write_documentation
    content: Write comprehensive documentation (README, API, interoperability guide)
    status: completed
    dependencies:
      - create_test_program
---

# C# .NET 10 RL Library Wrapper for Trajectory Planning

## Overview

This solution provides a C# .NET 10 wrapper around the RL (Robotic Library) C++ library for collision-free trajectory planning. The wrapper exposes a singleton service class that loads robot kinematics and scene files (with obstacles) once during initialization, and reuses them for all subsequent planning requests. All trajectories are automatically checked against scene obstacles to ensure collision-free paths.

## Project Structure

```javascript
RLTrajectoryPlanner/
├── RLlib/                          # RL library native libraries and dependencies
│   ├── Windows/
│   │   ├── rlplan.dll              # RL planning library
│   │   ├── rlkin.dll               # RL kinematics library
│   │   ├── rlsg.dll                # RL scene graph library
│   │   └── [other DLLs]            # RL math, XML, util, Boost, libxml2, etc.
│   ├── Linux/
│   │   ├── librlplan.so            # RL planning library
│   │   ├── librlkin.so             # RL kinematics library
│   │   ├── librlsg.so               # RL scene graph library
│   │   └── [other .so files]       # RL math, XML, util, Boost, libxml2, etc.
│   └── macOS/
│       ├── librlplan.dylib         # RL planning library
│       ├── librlkin.dylib          # RL kinematics library
│       ├── librlsg.dylib            # RL scene graph library
│       └── [other .dylib files]    # RL math, XML, util, Boost, libxml2, etc.
├── RLTrajectoryPlanner.Core/       # Core wrapper library
│   ├── RLTrajectoryPlanner.Core.csproj
│   ├── RLWrapper.cs                # P/Invoke declarations
│   ├── TrajectoryPlanner.cs        # Singleton planner service
│   ├── Models/
│   │   ├── PlanningRequest.cs      # Request model
│   │   ├── PlanningResult.cs       # Result model
│   │   └── PlannerType.cs          # Planner algorithm enum
│   └── Exceptions/
│       └── PlanningException.cs    # Custom exceptions
├── RLTrajectoryPlanner.Test/       # Test console application
│   ├── RLTrajectoryPlanner.Test.csproj
│   └── Program.cs                  # Test program
└── Documentation/
    ├── README.md                    # Usage documentation
    ├── INTEROPERABILITY.md         # P/Invoke details
    └── API.md                      # API reference
```

## Implementation Details

### 1. C++ Export Library (`RLWrapper.cpp` / `RLWrapper.h`)

Create a C++ wrapper DLL that exports C-compatible functions for:

- Loading kinematics XML file
- Loading scene XML file
- Setting start/goal configurations
- Planning trajectory with optional Z-axis constraint
- Getting trajectory waypoints
- Cleanup and resource management

**Key Functions:**

```cpp
extern "C" {
    // Create planner instance - maintains scene and kinematics for lifetime
    RL_PLANNER_API void* CreatePlanner();
    
    // Load kinematics ONCE - stored in planner instance
    RL_PLANNER_API int LoadKinematics(void* planner, const char* xmlPath);
    
    // Load scene with obstacles ONCE - stored in planner instance
    // Scene includes robot model and all obstacles for collision checking
    RL_PLANNER_API int LoadScene(void* planner, const char* xmlPath, int robotModelIndex);
    
    // Plan trajectory - uses pre-loaded scene and kinematics
    // Automatically checks collisions against scene obstacles
    RL_PLANNER_API int PlanTrajectory(void* planner, 
        double* start, int startSize,
        double* goal, int goalSize,
        int useZAxis, const char* plannerType,
        double delta, double epsilon, int timeoutMs,
        double* waypoints, int* maxWaypoints, int* waypointCount);
    
    // Check if configuration is collision-free (uses loaded scene)
    RL_PLANNER_API int IsValidConfiguration(void* planner, double* config, int configSize);
    
    // Cleanup - destroys scene and kinematics
    RL_PLANNER_API void DestroyPlanner(void* planner);
}
```

**C++ Implementation Notes:**

- Planner instance maintains: `std::shared_ptr<rl::sg::Scene>`, `std::shared_ptr<rl::kin::Kinematics>`, `std::shared_ptr<rl::plan::SimpleModel>`
- Scene contains robot model + obstacles - loaded once, used for all planning
- Model connects kinematics to scene for collision checking
- All planning calls reuse the same scene/kinematics objects

### 2. C# P/Invoke Wrapper (`RLWrapper.cs`)

P/Invoke declarations mapping C++ functions to C#:

- Use `DllImport` with `CallingConvention.Cdecl`
- Handle pointer marshalling for arrays
- Convert C error codes to exceptions
- **Cross-platform support**: Use `RuntimeInformation` to detect OS and load appropriate library names:
- Windows: `rlplan.dll`
- Linux: `librlplan.so` or `rlplan`
- macOS: `librlplan.dylib`
- Use platform-specific library loading helpers

### 3. Singleton Trajectory Planner (`TrajectoryPlanner.cs`)

**Key Features:**

- Singleton pattern implementation
- Thread-safe initialization
- **One-time initialization**: Load kinematics and scene XML files ONCE during initialization
- **Lifetime management**: Robot model and scene objects persist for the singleton's lifetime
- **Collision-free planning**: All trajectories are automatically checked against scene obstacles
- **API Methods:**
- `Initialize(InitializationRequest)` - Load kinematics and scene XML files once
- `PlanTrajectory(PlanningRequest)` - Plan collision-free trajectory using loaded models
- `IsInitialized` - Check if planner has been initialized
- `IsValidConfiguration(double[] config)` - Check if configuration is collision-free
- Plan trajectory method with:
- Start configuration (array of joint angles)
- Goal configuration (array of joint angles)
- Optional Z-axis flag (if false, fixes Z at current height for 2D planning)
- Planner algorithm selection (RRT-Connect default)
- Return trajectory as list of waypoints
- **Reuse loaded models**: All planning requests reuse the same loaded kinematics and scene
- **Collision checking**: Automatic via RL library's `model->isColliding()` against scene obstacles

**Planner Algorithm Support:**

- `RRT` - Rapidly-exploring Random Tree
- `RRTConnect` (default) - Bidirectional RRT
- `RRTGoalBias` - RRT with goal biasing
- `PRM` - Probabilistic Roadmap Method

### 4. SCARA Robot Z-Axis Handling

For SCARA robots (2 rotational joints + 1 linear Z-axis):

- **When `useZAxis = false`**: 
- Fix Z-axis at current height from start configuration
- Plan only in X-Y plane (2D planning)
- Z-axis value remains constant throughout trajectory
- **When `useZAxis = true`**:
- Allow Z-axis to vary (3D planning)
- All 3 DOF can change during planning

**Implementation:**

- Extract Z-axis value from start configuration
- If `useZAxis = false`, constrain Z-axis in planner or modify goal to match start Z
- Use RL library's joint limit constraints or custom constraint handling

### 5. Models

**`PlanningRequest.cs`:**

```csharp
public class PlanningRequest
{
    public double[] StartConfiguration { get; set; }
    public double[] GoalConfiguration { get; set; }
    public bool UseZAxis { get; set; } = false;
    public PlannerType Algorithm { get; set; } = PlannerType.RRTConnect;
    public double Delta { get; set; } = 0.1;  // Step size
    public double Epsilon { get; set; } = 0.001;  // Goal tolerance
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    // Note: Kinematics and Scene files are loaded during Initialize(), not per request
}
```

**`InitializationRequest.cs` (new):**

```csharp
public class InitializationRequest
{
    public string KinematicsXmlPath { get; set; }  // Robot kinematics XML file
    public string SceneXmlPath { get; set; }       // Scene XML with obstacles
    public int RobotModelIndex { get; set; } = 0;   // Index of robot in scene
}
```

**`PlanningResult.cs`:**

```csharp
public class PlanningResult
{
    public bool Success { get; set; }
    public List<double[]> Waypoints { get; set; }  // Each waypoint is array of joint angles
    public string ErrorMessage { get; set; }
    public TimeSpan PlanningTime { get; set; }
}
```

**`PlannerType.cs`:**

```csharp
public enum PlannerType
{
    RRT,
    RRTConnect,
    RRTGoalBias,
    PRM
}
```

### 6. Library Copying and Platform Detection

Create build scripts/targets to copy required RL native libraries:

- **Windows**: Copy `.dll` files to `RLlib/Windows/`
- **Linux**: Copy `.so` files to `RLlib/Linux/`
- **macOS**: Copy `.dylib` files to `RLlib/macOS/`
- Dependencies: Boost libraries, libxml2, collision detection libraries (PQP/ODE/Bullet/FCL)
- Use `RuntimeInformation.IsOSPlatform()` to detect platform at runtime
- Set library search path:
- Windows: `SetDllDirectory` or `AddDllDirectory`
- Linux: `LD_LIBRARY_PATH` or `dlopen` with `RTLD_GLOBAL`
- macOS: `DYLD_LIBRARY_PATH` or `dlopen`

### 7. Collision-Free Trajectory Planning

**Architecture:**

- **Scene Loading**: Scene XML contains robot model + obstacles (boxes, meshes, etc.)
- **Collision Detection**: RL library uses collision detection engine (PQP/ODE/Bullet/FCL) to check collisions
- **Automatic Checking**: Planner automatically verifies each configuration and path segment against scene obstacles
- **Verifier**: Uses `RecursiveVerifier` or `SequentialVerifier` to check intermediate points along path segments
- **Model Integration**: `SimpleModel` connects kinematics to scene - `model->isColliding()` checks robot vs obstacles

**Collision Checking Flow:**

1. Each configuration during planning: `model->setPosition(q)`, `model->updateFrames()`, `model->isColliding()`
2. Path segments verified: Verifier checks intermediate points between waypoints
3. Only collision-free paths are returned
4. If collision detected, planner tries alternative paths

### 8. Usage Pattern

**Initialization (Once):**

```csharp
var planner = TrajectoryPlanner.Instance;
planner.Initialize(new InitializationRequest 
{
    KinematicsXmlPath = "scara_robot.xml",
    SceneXmlPath = "workspace_with_obstacles.xml",
    RobotModelIndex = 0
});
```

**Planning (Multiple times, reusing loaded scene):**

```csharp
// All planning requests use the same loaded scene and kinematics
var result1 = planner.PlanTrajectory(new PlanningRequest { ... });
var result2 = planner.PlanTrajectory(new PlanningRequest { ... });
var result3 = planner.PlanTrajectory(new PlanningRequest { ... });
// Scene and kinematics persist - no reloading needed
```

**Key Points:**

- Scene and kinematics loaded ONCE during `Initialize()`
- All `PlanTrajectory()` calls reuse the same loaded models
- Collision checking is automatic - planner checks against scene obstacles
- Scene contains robot model + obstacles - all checked during planning
- No need to reload scene/kinematics for each planning request

### 9. Test Program

Simple console application demonstrating:

- Initialize singleton planner with kinematics and scene XML files (loaded once)
- Verify collision checking works (test invalid configurations)
- Plan trajectory in 2D (Z-axis fixed) - collision-free
- Plan trajectory in 3D (Z-axis variable) - collision-free
- Plan multiple trajectories reusing same loaded scene (no reloading)
- Display waypoints
- Test different planner algorithms
- Test scenarios with obstacles blocking direct paths
- Verify that trajectories avoid obstacles automatically

## Files to Create

### Core Library

- `RLTrajectoryPlanner.Core/RLTrajectoryPlanner.Core.csproj` - .NET 10 project file
- `RLTrajectoryPlanner.Core/RLWrapper.cs` - P/Invoke declarations
- `RLTrajectoryPlanner.Core/TrajectoryPlanner.cs` - Singleton service class
- `RLTrajectoryPlanner.Core/Models/PlanningRequest.cs`
- `RLTrajectoryPlanner.Core/Models/PlanningResult.cs`
- `RLTrajectoryPlanner.Core/Models/PlannerType.cs`
- `RLTrajectoryPlanner.Core/Exceptions/PlanningException.cs`

### C++ Wrapper (if needed)

- `RLWrapper/RLWrapper.h` - C++ header with C exports
- `RLWrapper/RLWrapper.cpp` - C++ implementation
- `RLWrapper/CMakeLists.txt` - CMake build file (cross-platform)
- `RLWrapper/RLWrapper.vcxproj` - Visual Studio project (Windows)
- Build scripts for Linux/macOS compilation

### Test Application

- `RLTrajectoryPlanner.Test/RLTrajectoryPlanner.Test.csproj`
- `RLTrajectoryPlanner.Test/Program.cs`

### Documentation

- `Documentation/README.md` - Main usage guide
- `Documentation/INTEROPERABILITY.md` - P/Invoke details, marshalling
- `Documentation/API.md` - API reference

### Build Configuration

- `RLTrajectoryPlanner.sln` - Solution file
- `RLTrajectoryPlanner.Core/Properties/AssemblyInfo.cs`
- Build scripts for copying DLLs

## Key Implementation Considerations

1. **Memory Management**: 

- C++ objects managed via opaque pointers
- Scene and kinematics loaded once, maintained for singleton lifetime
- Proper cleanup in Dispose pattern
- Handle DLL unloading
- Scene and kinematics persist between planning calls

2. **Thread Safety**: 

- Singleton initialization thread-safe
- Consider if planning can be parallelized or needs locking

3. **Error Handling**:

- Convert C++ exceptions to C# exceptions
- Validate input arrays match DOF count
- Handle planning failures gracefully

4. **Z-Axis Constraint**:

- For 2D planning, extract Z from start config
- Modify goal config Z to match start Z
- Or use RL's constraint mechanism if available

5. **Planner Configuration**:

- Map `PlannerType` enum to RL planner creation
- Set planner parameters (delta, epsilon, timeout)
- Configure sampler and verifier
- Planner uses pre-loaded model (kinematics + scene) for collision checking
- Verifier checks path segments against scene obstacles automatically

6. **Collision-Free Planning**:

- Scene loaded once contains robot model + obstacles
- Model connects kinematics to scene for collision queries
- Planner automatically checks `model->isColliding()` for each configuration
- Verifier checks intermediate points along path segments
- Only collision-free trajectories are returned
- Failed planning attempts indicate collision or unreachable goal

7. **Native Library Loading** (Cross-platform):

- **Windows**: Use `SetDllDirectory` or `AddDllDirectory` for RLlib/Windows folder
- **Linux**: Use `dlopen` or set `LD_LIBRARY_PATH` for RLlib/Linux folder
- **macOS**: Use `dlopen` or set `DYLD_LIBRARY_PATH` for RLlib/macOS folder
- Detect platform using `RuntimeInformation.IsOSPlatform(OSPlatform.Windows/Linux/OSX)`
- Handle missing library errors gracefully
- Consider architecture (x64/x86/arm64)
- Use `NativeLibrary.Load()` (.NET 5+) for better cross-platform support

## Dependencies

- **RL Library native libraries** (from rl-0.7.0 build):
- Windows: `.dll` files
- Linux: `.so` files  
- macOS: `.dylib` files
- **.NET 10 SDK** (cross-platform)
- **C++ Runtime** (platform-specific, if wrapper library needed)
- **Native dependencies**: Boost, libxml2, collision detection libraries (PQP/ODE/Bullet/FCL)
- **.NET packages**: `System.Runtime.InteropServices` for P/Invoke

## Testing Strategy