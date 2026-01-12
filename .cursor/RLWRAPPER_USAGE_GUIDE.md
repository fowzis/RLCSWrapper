# RLWrapper C# Usage Guide

## Table of Contents
1. [Overview](#overview)
2. [Understanding the IntPtr Planner](#understanding-the-intptr-planner)
3. [Two Approaches to Using RLWrapper](#two-approaches-to-using-rlwrapper)
4. [Approach 1: Using TrajectoryPlanner (High-Level)](#approach-1-using-trajectoryplanner-high-level)
5. [Approach 2: Using RLWrapper Directly (Low-Level)](#approach-2-using-rlwrapper-directly-low-level)
6. [Complete Examples](#complete-examples)
7. [Function Reference](#function-reference)
8. [Troubleshooting](#troubleshooting)

---

## Overview

The RLWrapper provides two ways to use trajectory planning functionality:

1. **High-Level API**: `TrajectoryPlanner` class (singleton) - Easier to use, manages IntPtr internally
2. **Low-Level API**: `RLWrapper` static class - Direct access to native functions, requires manual IntPtr management

Both approaches support:
- Loading kinematics and scene XML files separately
- Loading plan XML files (new feature)
- Setting start/goal configurations separately
- Planning multiple trajectories with the same loaded scene

---

## Understanding the IntPtr Planner

The `IntPtr planner` parameter is a handle to a native C++ planner object. Think of it as a pointer to the planner instance in unmanaged memory.

### How to Get an IntPtr Planner

You must create a planner instance first using `RLWrapper.CreatePlanner()`:

```csharp
using RLTrajectoryPlanner.Core;

// Create a planner instance
IntPtr planner = RLWrapper.CreatePlanner();

if (planner == IntPtr.Zero)
{
    throw new Exception("Failed to create planner");
}

// Use planner for all operations...
// ... load files, set configurations, plan trajectories ...

// Clean up when done
RLWrapper.DestroyPlanner(planner);
```

**Important**: Always call `DestroyPlanner()` when you're done to free native resources!

---

## Two Approaches to Using RLWrapper

### Approach Comparison

| Feature | TrajectoryPlanner (High-Level) | RLWrapper (Low-Level) |
|---------|-------------------------------|----------------------|
| IntPtr Management | Automatic (hidden) | Manual |
| Plan XML Loading | Not yet supported* | ✅ Supported |
| SetStart/SetGoal | Not yet supported* | ✅ Supported |
| Multiple Planners | Single singleton | Multiple instances |
| Error Handling | Exceptions | Return codes |
| Thread Safety | Built-in locking | Manual |

*These features can be added to TrajectoryPlanner wrapper if needed.

---

## Approach 1: Using TrajectoryPlanner (High-Level)

The `TrajectoryPlanner` class manages the IntPtr internally, so you don't need to worry about it.

### Basic Usage

```csharp
using RLTrajectoryPlanner.Core;
using RLTrajectoryPlanner.Core.Models;

// Get singleton instance
var planner = TrajectoryPlanner.Instance;

// Initialize with separate XML files
var initRequest = new InitializationRequest
{
    KinematicsXmlPath = "robot.xml",
    SceneXmlPath = "scene.xml",
    RobotModelIndex = 0
};

planner.Initialize(initRequest);

// Plan a trajectory
var request = new PlanningRequest
{
    StartConfiguration = new double[] { 0.0, 0.0, 0.0 },
    GoalConfiguration = new double[] { 1.0, 1.0, 0.5 },
    UseZAxis = false,
    Algorithm = PlannerType.RRTConnect,
    Delta = 0.1,
    Epsilon = 0.001,
    Timeout = TimeSpan.FromSeconds(30)
};

var result = planner.PlanTrajectory(request);

if (result.Success)
{
    Console.WriteLine($"Found path with {result.Waypoints.Count} waypoints");
    foreach (var waypoint in result.Waypoints)
    {
        Console.WriteLine($"  [{string.Join(", ", waypoint)}]");
    }
}

// Clean up (optional - singleton persists)
planner.Dispose();
```

### Limitations

Currently, `TrajectoryPlanner` doesn't expose:
- `LoadPlanXml()` - Use low-level API for this
- `SetStartConfiguration()` / `SetGoalConfiguration()` - Use low-level API for this

---

## Approach 2: Using RLWrapper Directly (Low-Level)

This approach gives you full control but requires manual IntPtr management.

**Important Note**: The `RLWrapper` methods are currently marked as `internal`. To use them directly in your own project, you have two options:

1. **Add your code to the `RLTrajectoryPlanner.Core` project** (same assembly)
2. **Make the methods public** by changing `internal` to `public` in `RLWrapper.cs`

For this guide, we'll assume you've made them public or are working within the same assembly.

### Step-by-Step Guide

#### Step 1: Create a Planner Instance

```csharp
using RLTrajectoryPlanner.Core;
using System;

IntPtr planner = RLWrapper.CreatePlanner();

if (planner == IntPtr.Zero)
{
    throw new Exception("Failed to create planner instance");
}
```

#### Step 2: Load Your Robot and Scene

You have two options:

**Option A: Load Separate XML Files**

```csharp
// Load kinematics XML
RLWrapper.LoadKinematics(planner, "path/to/kinematics.xml");

// Load scene XML (includes obstacles)
RLWrapper.LoadScene(planner, "path/to/scene.xml", robotModelIndex: 0);
```

**Option B: Load Plan XML File (New Feature!)**

```csharp
// Load plan XML that references both kinematics and scene
RLWrapper.LoadPlanXml(planner, "path/to/plan.xml");
```

The plan XML file format looks like this:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<rlplan xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:noNamespaceSchemaLocation="rlplan.xsd">
    <rrtConCon>
        <model>
            <kinematics href="../rlmdl/robot.xml" type="mdl"/>
            <model>0</model>
            <scene href="../rlsg/scene.xml"/>
        </model>
        <start>
            <q unit="deg">0</q>
            <q unit="deg">0</q>
            <q unit="deg">90</q>
        </start>
        <goal>
            <q unit="deg">90</q>
            <q unit="deg">-180</q>
            <q unit="deg">90</q>
        </goal>
        <delta unit="deg">1</delta>
        <epsilon>0.001</epsilon>
        <duration>120</duration>
    </rrtConCon>
</rlplan>
```

#### Step 3: Get Robot DOF (Degrees of Freedom)

```csharp
int dof = RLWrapper.GetDof(planner);
if (dof < 0)
{
    throw new Exception("Failed to get DOF - planner not initialized");
}

Console.WriteLine($"Robot has {dof} degrees of freedom");
```

#### Step 4: Set Start and Goal Configurations

You can set configurations in two ways:

**Method 1: Set Separately (Recommended for Multiple Plans)**

```csharp
// Set start configuration
double[] startConfig = new double[] { 0.0, 0.0, 0.0 };
RLWrapper.SetStartConfiguration(planner, startConfig);

// Set goal configuration
double[] goalConfig = new double[] { 1.0, 1.0, 0.5 };
RLWrapper.SetGoalConfiguration(planner, goalConfig);
```

**Method 2: Pass Directly to PlanTrajectory**

```csharp
// Configurations passed directly (see Step 5)
```

#### Step 5: Plan Trajectory

**Using Stored Configurations:**

**Note**: The current C# wrapper requires arrays to be passed. To use stored configurations, first retrieve them (if you stored them) or set them before calling PlanTrajectory:

```csharp
// Set configurations first
double[] startConfig = new double[] { 0.0, 0.0, 0.0 };
double[] goalConfig = new double[] { 1.0, 1.0, 0.5 };

RLWrapper.SetStartConfiguration(planner, startConfig);
RLWrapper.SetGoalConfiguration(planner, goalConfig);

// Then plan (the native code will use stored values if arrays match)
double[] waypoints = RLWrapper.PlanTrajectory(
    planner,
    start: startConfig,      // Can reuse same arrays
    goal: goalConfig,
    useZAxis: false,
    plannerType: "rrtConCon",
    delta: 0.1,
    epsilon: 0.001,
    timeout: TimeSpan.FromSeconds(30),
    waypointCount: out int waypointCount
);
```

**Passing Configurations Directly:**

```csharp
double[] startConfig = new double[] { 0.0, 0.0, 0.0 };
double[] goalConfig = new double[] { 1.0, 1.0, 0.5 };

double[] waypoints = RLWrapper.PlanTrajectory(
    planner,
    start: startConfig,
    goal: goalConfig,
    useZAxis: false,
    plannerType: "rrtConCon",
    delta: 0.1,
    epsilon: 0.001,
    timeout: TimeSpan.FromSeconds(30),
    waypointCount: out int waypointCount
);
```

#### Step 6: Process Waypoints

```csharp
if (waypointCount > 0)
{
    int dof = RLWrapper.GetDof(planner);
    
    // Convert flat array to list of waypoint arrays
    List<double[]> waypointList = new List<double[]>();
    
    for (int i = 0; i < waypointCount; i++)
    {
        double[] waypoint = new double[dof];
        Array.Copy(waypoints, i * dof, waypoint, 0, dof);
        waypointList.Add(waypoint);
        
        Console.WriteLine($"Waypoint {i}: [{string.Join(", ", waypoint)}]");
    }
}
```

#### Step 7: Clean Up

```csharp
// Always destroy planner when done
RLWrapper.DestroyPlanner(planner);
planner = IntPtr.Zero;
```

---

## Complete Examples

### Example 1: Using Plan XML File

```csharp
using RLTrajectoryPlanner.Core;
using System;
using System.IO;

class Program
{
    static void Main()
    {
        IntPtr planner = IntPtr.Zero;
        
        try
        {
            // Step 1: Create planner
            planner = RLWrapper.CreatePlanner();
            if (planner == IntPtr.Zero)
            {
                throw new Exception("Failed to create planner");
            }
            
            // Step 2: Load plan XML (includes kinematics, scene, start/goal)
            string planXmlPath = "unimation-puma560_boxes_rrtConCon.mdl.xml";
            if (!File.Exists(planXmlPath))
            {
                throw new FileNotFoundException($"Plan XML not found: {planXmlPath}");
            }
            
            RLWrapper.LoadPlanXml(planner, planXmlPath);
            Console.WriteLine("✓ Plan XML loaded successfully");
            
            // Step 3: Get DOF
            int dof = RLWrapper.GetDof(planner);
            Console.WriteLine($"✓ Robot DOF: {dof}");
            
            // Step 4: Plan using start/goal from XML
            // Note: LoadPlanXml stores start/goal internally, but C# wrapper
            // still requires arrays. You can pass dummy arrays - the native code
            // will use stored values if they match DOF.
            int dof = RLWrapper.GetDof(planner);
            double[] dummyStart = new double[dof];  // Dummy arrays
            double[] dummyGoal = new double[dof];
            
            double[] waypoints = RLWrapper.PlanTrajectory(
                planner,
                start: dummyStart,
                goal: dummyGoal,
                useZAxis: false,
                plannerType: "rrtConCon",
                delta: 0.1,
                epsilon: 0.001,
                timeout: TimeSpan.FromSeconds(30),
                waypointCount: out int waypointCount
            );
            
            if (waypointCount > 0)
            {
                Console.WriteLine($"✓ Planning succeeded! Found {waypointCount} waypoints");
                
                // Process waypoints
                for (int i = 0; i < waypointCount; i++)
                {
                    double[] waypoint = new double[dof];
                    Array.Copy(waypoints, i * dof, waypoint, 0, dof);
                    Console.WriteLine($"  Waypoint {i}: [{string.Join(", ", waypoint)}]");
                }
            }
            else
            {
                Console.WriteLine("✗ Planning failed - no waypoints found");
            }
            else
            {
                Console.WriteLine("✗ Planning failed - no waypoints found");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            // Clean up
            if (planner != IntPtr.Zero)
            {
                RLWrapper.DestroyPlanner(planner);
            }
        }
    }
}
```

### Example 2: Multiple Trajectories with Same Scene

```csharp
using RLTrajectoryPlanner.Core;
using System;

class Program
{
    static void Main()
    {
        IntPtr planner = IntPtr.Zero;
        
        try
        {
            // Create and initialize planner once
            planner = RLWrapper.CreatePlanner();
            RLWrapper.LoadKinematics(planner, "robot.xml");
            RLWrapper.LoadScene(planner, "scene.xml", robotModelIndex: 0);
            
            int dof = RLWrapper.GetDof(planner);
            Console.WriteLine($"Robot DOF: {dof}");
            
            // Plan multiple trajectories without reloading scene
            for (int i = 0; i < 5; i++)
            {
                // Set new start/goal for each trajectory
                double[] start = new double[dof];
                double[] goal = new double[dof];
                
                for (int j = 0; j < dof; j++)
                {
                    start[j] = i * 0.2;
                    goal[j] = (i + 1) * 0.2;
                }
                
                RLWrapper.SetStartConfiguration(planner, start);
                RLWrapper.SetGoalConfiguration(planner, goal);
                
                // Plan using stored configurations
                // Pass the same arrays that were used in SetStart/SetGoal
                double[] waypoints = RLWrapper.PlanTrajectory(
                    planner,
                    start: start,  // Same arrays used in SetStartConfiguration
                    goal: goal,    // Same arrays used in SetGoalConfiguration
                    useZAxis: false,
                    plannerType: "rrtConCon",
                    delta: 0.1,
                    epsilon: 0.001,
                    timeout: TimeSpan.FromSeconds(10),
                    waypointCount: out int waypointCount
                );
                
                Console.WriteLine($"Trajectory {i + 1}: {(waypointCount > 0 ? "Success" : "Failed")} ({waypointCount} waypoints)");
            }
        }
        finally
        {
            if (planner != IntPtr.Zero)
            {
                RLWrapper.DestroyPlanner(planner);
            }
        }
    }
}
```

### Example 3: Checking Configuration Validity

```csharp
using RLTrajectoryPlanner.Core;

IntPtr planner = RLWrapper.CreatePlanner();
RLWrapper.LoadKinematics(planner, "robot.xml");
RLWrapper.LoadScene(planner, "scene.xml", robotModelIndex: 0);

int dof = RLWrapper.GetDof(planner);

// Test a configuration
double[] config = new double[] { 0.5, 0.3, 0.2 };
bool isValid = RLWrapper.IsValidConfiguration(planner, config);

if (isValid)
{
    Console.WriteLine("Configuration is valid (collision-free and within joint limits)");
}
else
{
    Console.WriteLine("Configuration is invalid (collision or joint limit violation)");
}

RLWrapper.DestroyPlanner(planner);
```

---

## Function Reference

### RLWrapper.CreatePlanner()

Creates a new planner instance.

**Returns**: `IntPtr` - Handle to planner instance, or `IntPtr.Zero` on failure

**Example**:
```csharp
IntPtr planner = RLWrapper.CreatePlanner();
```

---

### RLWrapper.LoadKinematics(IntPtr planner, string xmlPath)

Loads robot kinematics from XML file.

**Parameters**:
- `planner`: Planner instance handle
- `xmlPath`: Path to kinematics XML file

**Throws**: `PlanningException` on failure

**Example**:
```csharp
RLWrapper.LoadKinematics(planner, "robot.xml");
```

---

### RLWrapper.LoadScene(IntPtr planner, string xmlPath, int robotModelIndex)

Loads scene (obstacles) from XML file.

**Parameters**:
- `planner`: Planner instance handle
- `xmlPath`: Path to scene XML file
- `robotModelIndex`: Index of robot model in scene (usually 0)

**Throws**: `PlanningException` on failure

**Example**:
```csharp
RLWrapper.LoadScene(planner, "scene.xml", robotModelIndex: 0);
```

---

### RLWrapper.LoadPlanXml(IntPtr planner, string xmlPath)

Loads plan XML file that references kinematics and scene XMLs. Also extracts start/goal configurations and planner parameters if present.

**Parameters**:
- `planner`: Planner instance handle
- `xmlPath`: Path to plan XML file

**Throws**: `PlanningException` on failure

**Example**:
```csharp
RLWrapper.LoadPlanXml(planner, "plan.xml");
```

**Note**: This function internally calls `LoadKinematics` and `LoadScene`, so you don't need to call them separately.

---

### RLWrapper.SetStartConfiguration(IntPtr planner, double[] config)

Sets the start configuration for planning. Stored in planner instance for reuse.

**Parameters**:
- `planner`: Planner instance handle
- `config`: Start configuration array (length must match robot DOF)

**Throws**: `PlanningException` on failure

**Example**:
```csharp
double[] start = new double[] { 0.0, 0.0, 0.0 };
RLWrapper.SetStartConfiguration(planner, start);
```

---

### RLWrapper.SetGoalConfiguration(IntPtr planner, double[] config)

Sets the goal configuration for planning. Stored in planner instance for reuse.

**Parameters**:
- `planner`: Planner instance handle
- `config`: Goal configuration array (length must match robot DOF)

**Throws**: `PlanningException` on failure

**Example**:
```csharp
double[] goal = new double[] { 1.0, 1.0, 0.5 };
RLWrapper.SetGoalConfiguration(planner, goal);
```

---

### RLWrapper.PlanTrajectory(...)

Plans a trajectory between start and goal configurations.

**Signature**:
```csharp
internal static double[] PlanTrajectory(
    IntPtr planner,
    double[] start,
    double[] goal,
    bool useZAxis,
    string plannerType,
    double delta,
    double epsilon,
    TimeSpan timeout,
    out int waypointCount
)
```

**Parameters**:
- `planner`: Planner instance handle
- `start`: Start configuration array (must match robot DOF)
- `goal`: Goal configuration array (must match robot DOF)
- `useZAxis`: `false` = fix Z-axis, `true` = allow Z-axis movement
- `plannerType`: Planner algorithm string (`"rrt"`, `"rrtConCon"`, `"rrtGoalBias"`, `"prm"`)
- `delta`: Step size for planning
- `epsilon`: Goal tolerance
- `timeout`: Timeout as `TimeSpan`
- `waypointCount`: Output - actual number of waypoints returned

**Returns**: `double[]` - Flat array of waypoints (waypointCount * dof values)

**Throws**: `PlanningException` on failure

**Example**:
```csharp
double[] waypoints = RLWrapper.PlanTrajectory(
    planner,
    start: new double[] { 0.0, 0.0, 0.0 },
    goal: new double[] { 1.0, 1.0, 0.5 },
    useZAxis: false,
    plannerType: "rrtConCon",
    delta: 0.1,
    epsilon: 0.001,
    timeout: TimeSpan.FromSeconds(30),
    waypointCount: out int waypointCount
);

// Process waypoints
int dof = RLWrapper.GetDof(planner);
for (int i = 0; i < waypointCount; i++)
{
    double[] waypoint = new double[dof];
    Array.Copy(waypoints, i * dof, waypoint, 0, dof);
    Console.WriteLine($"Waypoint {i}: [{string.Join(", ", waypoint)}]");
}
```

**Note**: The native C++ function supports `null` pointers for start/goal to use stored values, but the C# wrapper currently requires arrays. The native code will use stored start/goal if they were set via `SetStartConfiguration`/`SetGoalConfiguration` and match the DOF.

---

### RLWrapper.IsValidConfiguration(IntPtr planner, double[] config)

Checks if a configuration is valid (collision-free and within joint limits).

**Parameters**:
- `planner`: Planner instance handle
- `config`: Configuration array to check

**Returns**: `bool` - `true` if valid, `false` otherwise

**Example**:
```csharp
bool isValid = RLWrapper.IsValidConfiguration(planner, config);
```

---

### RLWrapper.GetDof(IntPtr planner)

Gets the degrees of freedom (number of joints) of the robot.

**Parameters**:
- `planner`: Planner instance handle

**Returns**: `int` - Number of degrees of freedom, or negative error code on failure

**Example**:
```csharp
int dof = RLWrapper.GetDof(planner);
```

---

### RLWrapper.DestroyPlanner(IntPtr planner)

Destroys planner instance and frees all resources.

**Parameters**:
- `planner`: Planner instance handle to destroy

**Example**:
```csharp
RLWrapper.DestroyPlanner(planner);
```

**Important**: Always call this when done with a planner instance!

---

## Troubleshooting

### "Failed to create planner instance"

**Cause**: `CreatePlanner()` returned `IntPtr.Zero`

**Solution**: Check that RLWrapper.dll and all dependencies are available. Ensure DLL search paths are configured correctly.

---

### "Planner not initialized"

**Cause**: Trying to use planner before loading kinematics/scene

**Solution**: Call `LoadKinematics()` and `LoadScene()`, or `LoadPlanXml()` first.

---

### "Configuration length mismatch"

**Cause**: Configuration array size doesn't match robot DOF

**Solution**: Get DOF first with `GetDof()`, then create arrays of correct size:
```csharp
int dof = RLWrapper.GetDof(planner);
double[] config = new double[dof];
```

---

### "Planning failed - no waypoints"

**Cause**: Planner couldn't find a path

**Possible Solutions**:
- Check that start/goal configurations are valid: `IsValidConfiguration()`
- Increase timeout
- Try different planner algorithm
- Check if obstacles are blocking the path
- Adjust delta/epsilon parameters

---

### Memory Leaks

**Cause**: Not calling `DestroyPlanner()`

**Solution**: Always use try/finally or `using` pattern:
```csharp
IntPtr planner = IntPtr.Zero;
try
{
    planner = RLWrapper.CreatePlanner();
    // ... use planner ...
}
finally
{
    if (planner != IntPtr.Zero)
    {
        RLWrapper.DestroyPlanner(planner);
    }
}
```

---

### "File not found" errors

**Cause**: XML file paths are incorrect

**Solution**: Use absolute paths or ensure working directory is correct:
```csharp
string fullPath = Path.GetFullPath("relative/path/to/file.xml");
RLWrapper.LoadKinematics(planner, fullPath);
```

---

## Best Practices

1. **Always destroy planners**: Use try/finally or `using` pattern
2. **Check return values**: Verify `CreatePlanner()` doesn't return `IntPtr.Zero`
3. **Validate configurations**: Use `IsValidConfiguration()` before planning
4. **Reuse planners**: Create once, use for multiple trajectories
5. **Use absolute paths**: Avoid path resolution issues
6. **Handle exceptions**: Wrap calls in try/catch for `PlanningException`
7. **Check DOF**: Always verify DOF matches your configuration arrays

---

## Summary

- **IntPtr planner** = Handle from `RLWrapper.CreatePlanner()`
- **Load files** = `LoadKinematics()` + `LoadScene()` OR `LoadPlanXml()`
- **Set configs** = `SetStartConfiguration()` + `SetGoalConfiguration()` (optional)
- **Plan** = `PlanTrajectory()` (pass arrays - native code uses stored configs if set)
- **Clean up** = `DestroyPlanner()` (always!)

For most use cases, prefer the high-level `TrajectoryPlanner` class. Use `RLWrapper` directly only when you need:
- Plan XML loading
- Separate start/goal setting
- Multiple planner instances
- Fine-grained control

## Making RLWrapper Methods Public

If you want to use `RLWrapper` methods from outside the `RLTrajectoryPlanner.Core` assembly, you need to make them public:

1. Open `RLTrajectoryPlanner.Core/RLWrapper.cs`
2. Change `internal static` to `public static` for the methods you need:
   - `CreatePlanner()`
   - `LoadKinematics()`
   - `LoadScene()`
   - `LoadPlanXml()`
   - `SetStartConfiguration()`
   - `SetGoalConfiguration()`
   - `PlanTrajectory()`
   - `IsValidConfiguration()`
   - `GetDof()`
   - `DestroyPlanner()`

Example:
```csharp
// Change from:
internal static IntPtr CreatePlanner()

// To:
public static IntPtr CreatePlanner()
```

Then you can use them in any project that references `RLTrajectoryPlanner.Core`.
