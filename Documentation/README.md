# RL Trajectory Planner - C# .NET 10 Wrapper

A C# .NET 10 wrapper around the RL (Robotic Library) C++ library for collision-free trajectory planning. This wrapper provides a singleton service class that loads robot kinematics and scene files (with obstacles) once during initialization and reuses them for all subsequent planning requests.

## Features

- **Collision-Free Planning**: All trajectories are automatically checked against scene obstacles
- **One-Time Initialization**: Robot model and scene loaded once, reused for lifetime
- **SCARA Robot Support**: Optional Z-axis handling for 2D/3D planning
- **Multiple Planner Algorithms**: RRT, RRT-Connect, RRT-GoalBias, PRM
- **Cross-Platform**: Supports Windows, Linux, and macOS
- **Thread-Safe**: Singleton implementation with thread-safe initialization

## Requirements

- .NET 10 SDK
- RL Library native libraries (built from rl-0.7.0)
- RLWrapper native library (built from RLWrapper project)
- Native dependencies: Boost, libxml2, collision detection libraries (PQP/ODE/Bullet/FCL)

## Project Structure

```
RLTrajectoryPlanner/
├── RLlib/                          # Native libraries (platform-specific)
│   ├── Windows/                    # Windows DLLs
│   ├── Linux/                      # Linux .so files
│   └── macOS/                      # macOS .dylib files
├── RLTrajectoryPlanner.Core/       # Core wrapper library
├── RLTrajectoryPlanner.Test/       # Test console application
├── RLWrapper/                      # C++ wrapper DLL source
└── Documentation/                  # This documentation
```

## Quick Start

### 1. Build the RL Library

First, build the RL library from source. See the RL library documentation for build instructions.

### 2. Build the RLWrapper C++ Library

```bash
cd RLWrapper
mkdir build
cd build
cmake ..
cmake --build .
```

### 3. Copy Native Libraries

Copy the RL libraries from the RL project's `install/bin` directory to the appropriate RLlib folders:

```powershell
# Windows PowerShell (from project root)
.\copy_rl_libraries.ps1 -RLInstallPath "C:\path\to\rl\install"

# Or from RLlib directory
cd RLlib
.\copy_rl_libraries.ps1 -RLInstallPath "C:\path\to\rl\install"

# Include dependencies (VC++ runtime, Boost, etc.)
.\copy_rl_libraries.ps1 -RLInstallPath "C:\path\to\rl\install" -IncludeDependencies
```

```bash
# Linux/macOS (from project root)
chmod +x copy_rl_libraries.sh
./copy_rl_libraries.sh /path/to/rl/install

# Or from RLlib directory
cd RLlib
chmod +x copy_rl_libraries.sh
./copy_rl_libraries.sh /path/to/rl/install

# Include dependencies
./copy_rl_libraries.sh /path/to/rl/install --include-dependencies
```

**Note**: The scripts automatically copy all `rl*.dll` (Windows), `librl*.so` (Linux), or `librl*.dylib` (macOS) files from the `install/bin` directory. See `RLlib/README.md` for more details.

### 4. Build the C# Solution

```bash
dotnet build RLTrajectoryPlanner.sln
```

### 5. Use in Your Code

```csharp
using RLTrajectoryPlanner.Core;
using RLTrajectoryPlanner.Core.Models;

// Get singleton instance
var planner = TrajectoryPlanner.Instance;

// Initialize once with kinematics and scene
planner.Initialize(new InitializationRequest
{
    KinematicsXmlPath = "scara_robot.xml",
    SceneXmlPath = "workspace_with_obstacles.xml",
    RobotModelIndex = 0
});

// Plan trajectory (2D - Z-axis fixed)
var request = new PlanningRequest
{
    StartConfiguration = new double[] { 0.0, 0.0, 0.1 },
    GoalConfiguration = new double[] { 1.0, 1.0, 0.1 },
    UseZAxis = false,  // Fix Z-axis for 2D planning
    Algorithm = PlannerType.RRTConnect,
    Delta = 0.1,
    Epsilon = 0.001,
    Timeout = TimeSpan.FromSeconds(30)
};

var result = planner.PlanTrajectory(request);

if (result.Success)
{
    Console.WriteLine($"Planned trajectory with {result.Waypoints.Count} waypoints");
    foreach (var waypoint in result.Waypoints)
    {
        Console.WriteLine($"  [{string.Join(", ", waypoint)}]");
    }
}
else
{
    Console.WriteLine($"Planning failed: {result.ErrorMessage}");
}
```

## Usage Pattern

### Initialization (Once)

```csharp
var planner = TrajectoryPlanner.Instance;
planner.Initialize(new InitializationRequest 
{
    KinematicsXmlPath = "robot.xml",
    SceneXmlPath = "scene.xml",
    RobotModelIndex = 0
});
```

### Planning (Multiple Times)

```csharp
// All planning requests reuse the same loaded scene and kinematics
var result1 = planner.PlanTrajectory(new PlanningRequest { ... });
var result2 = planner.PlanTrajectory(new PlanningRequest { ... });
var result3 = planner.PlanTrajectory(new PlanningRequest { ... });
// No reloading needed!
```

## Key Concepts

### Collision-Free Planning

The planner automatically checks all configurations and path segments against obstacles in the scene. Only collision-free trajectories are returned.

### Scene and Kinematics Loading

- **Scene XML**: Contains robot model + obstacles (boxes, meshes, etc.)
- **Kinematics XML**: Defines robot structure (joints, links, transformations)
- Both are loaded **once** during `Initialize()` and persist for the singleton's lifetime

### Z-Axis Handling (SCARA Robots)

For SCARA robots with 2 rotational joints + 1 linear Z-axis:

- **`UseZAxis = false`**: Fixes Z-axis at start height, plans in 2D (X-Y plane)
- **`UseZAxis = true`**: Allows Z-axis to vary, plans in 3D

### Planner Algorithms

- **RRT**: Rapidly-exploring Random Tree (basic)
- **RRTConnect** (default): Bidirectional RRT (recommended)
- **RRTGoalBias**: RRT with goal biasing
- **PRM**: Probabilistic Roadmap Method

## Running Tests

```bash
cd RLTrajectoryPlanner.Test
dotnet run scara_robot.xml workspace.xml
```

## Troubleshooting

### Library Not Found

Ensure native libraries are in the correct RLlib platform folder:
- Windows: `RLlib/Windows/RLWrapper.dll`
- Linux: `RLlib/Linux/libRLWrapper.so`
- macOS: `RLlib/macOS/libRLWrapper.dylib`

### Planning Fails

- Check that start and goal configurations are valid (within joint limits)
- Verify scene contains obstacles correctly
- Try different planner algorithms
- Increase timeout if planning takes too long

### Initialization Fails

- Verify XML file paths are correct
- Check that XML files are valid RL format
- Ensure robot model index matches scene structure

## See Also

- [API Reference](API.md) - Detailed API documentation
- [Interoperability Guide](INTEROPERABILITY.md) - P/Invoke and native library details

## License

This wrapper follows the same license as the RL library.

