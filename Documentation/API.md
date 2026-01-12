# API Reference

## TrajectoryPlanner Class

Singleton service class for collision-free trajectory planning.

### Properties

#### `Instance` (static)

Gets the singleton instance of TrajectoryPlanner.

```csharp
public static TrajectoryPlanner Instance { get; }
```

**Returns**: The singleton instance.

#### `IsInitialized`

Gets a value indicating whether the planner has been initialized.

```csharp
public bool IsInitialized { get; }
```

**Returns**: `true` if initialized; otherwise, `false`.

#### `Dof`

Gets the degrees of freedom (number of joints) of the robot.

```csharp
public int Dof { get; }
```

**Returns**: Number of joints, or `-1` if not initialized.

### Methods

#### `Initialize`

Initializes the planner by loading kinematics and scene XML files.

```csharp
public void Initialize(InitializationRequest request)
```

**Parameters**:
- `request` (InitializationRequest): Initialization request containing paths to XML files.

**Exceptions**:
- `ArgumentNullException`: When request is null.
- `ArgumentException`: When required paths are null or empty.
- `FileNotFoundException`: When XML files are not found.
- `PlanningException`: When initialization fails.

**Example**:
```csharp
planner.Initialize(new InitializationRequest
{
    KinematicsXmlPath = "robot.xml",
    SceneXmlPath = "scene.xml",
    RobotModelIndex = 0
});
```

#### `PlanTrajectory`

Plans a collision-free trajectory between start and goal configurations.

```csharp
public PlanningResult PlanTrajectory(PlanningRequest request)
```

**Parameters**:
- `request` (PlanningRequest): Planning request with start/goal and parameters.

**Returns**: Planning result containing waypoints or error information.

**Exceptions**:
- `ArgumentNullException`: When request is null.
- `ArgumentException`: When configurations are invalid.
- `PlanningException`: When planning fails or planner is not initialized.

**Example**:
```csharp
var result = planner.PlanTrajectory(new PlanningRequest
{
    StartConfiguration = new double[] { 0.0, 0.0, 0.1 },
    GoalConfiguration = new double[] { 1.0, 1.0, 0.1 },
    UseZAxis = false,
    Algorithm = PlannerType.RRTConnect,
    Delta = 0.1,
    Epsilon = 0.001,
    Timeout = TimeSpan.FromSeconds(30)
});
```

#### `IsValidConfiguration`

Checks if a configuration is valid (collision-free and within joint limits).

```csharp
public bool IsValidConfiguration(double[] config)
```

**Parameters**:
- `config` (double[]): Configuration to check.

**Returns**: `true` if valid; otherwise, `false`.

**Exceptions**:
- `ArgumentNullException`: When config is null.
- `PlanningException`: When planner is not initialized.

**Example**:
```csharp
bool isValid = planner.IsValidConfiguration(new double[] { 0.0, 0.5, 0.1 });
```

#### `Dispose`

Releases all resources used by the TrajectoryPlanner.

```csharp
public void Dispose()
```

## Models

### InitializationRequest

Request model for initializing the trajectory planner.

**Properties**:
- `KinematicsXmlPath` (string): Path to robot kinematics XML file.
- `SceneXmlPath` (string): Path to scene XML file with obstacles.
- `RobotModelIndex` (int): Index of robot model in scene (default: 0).

### PlanningRequest

Request model for planning a trajectory.

**Properties**:
- `StartConfiguration` (double[]): Start configuration (joint angles/positions).
- `GoalConfiguration` (double[]): Goal configuration (joint angles/positions).
- `UseZAxis` (bool): If false, fixes Z-axis for 2D planning (default: false).
- `Algorithm` (PlannerType): Planner algorithm to use (default: RRTConnect).
- `Delta` (double): Step size for planner expansion (default: 0.1).
- `Epsilon` (double): Goal tolerance (default: 0.001).
- `Timeout` (TimeSpan): Maximum planning time (default: 30 seconds).

### PlanningResult

Result model containing planned trajectory.

**Properties**:
- `Success` (bool): Indicates whether planning succeeded.
- `Waypoints` (List<double[]>): List of waypoints (each is array of joint angles).
- `ErrorMessage` (string): Error message if planning failed.
- `PlanningTime` (TimeSpan): Time taken for planning.

### PlannerType

Enumeration of available planner algorithms.

**Values**:
- `RRT`: Rapidly-exploring Random Tree
- `RRTConnect`: Bidirectional RRT (default, recommended)
- `RRTGoalBias`: RRT with goal biasing
- `PRM`: Probabilistic Roadmap Method

## Exceptions

### PlanningException

Exception thrown when trajectory planning operations fail.

**Inherits**: `Exception`

**Constructors**:
- `PlanningException()`
- `PlanningException(string message)`
- `PlanningException(string message, Exception innerException)`

## Usage Examples

### Basic Usage

```csharp
using RLTrajectoryPlanner.Core;
using RLTrajectoryPlanner.Core.Models;

var planner = TrajectoryPlanner.Instance;

// Initialize
planner.Initialize(new InitializationRequest
{
    KinematicsXmlPath = "robot.xml",
    SceneXmlPath = "scene.xml"
});

// Plan trajectory
var result = planner.PlanTrajectory(new PlanningRequest
{
    StartConfiguration = new double[] { 0.0, 0.0 },
    GoalConfiguration = new double[] { 1.0, 1.0 },
    Algorithm = PlannerType.RRTConnect
});

if (result.Success)
{
    Console.WriteLine($"Planned {result.Waypoints.Count} waypoints");
}
```

### 2D vs 3D Planning

```csharp
// 2D planning (Z-axis fixed)
var request2D = new PlanningRequest
{
    StartConfiguration = new double[] { 0.0, 0.0, 0.1 },
    GoalConfiguration = new double[] { 1.0, 1.0, 0.1 },
    UseZAxis = false  // Z stays at 0.1
};

// 3D planning (Z-axis variable)
var request3D = new PlanningRequest
{
    StartConfiguration = new double[] { 0.0, 0.0, 0.1 },
    GoalConfiguration = new double[] { 1.0, 1.0, 0.2 },
    UseZAxis = true  // Z can change
};
```

### Multiple Trajectories

```csharp
// Initialize once
planner.Initialize(initRequest);

// Plan multiple trajectories (scene reused)
var result1 = planner.PlanTrajectory(request1);
var result2 = planner.PlanTrajectory(request2);
var result3 = planner.PlanTrajectory(request3);
```

### Error Handling

```csharp
try
{
    planner.Initialize(initRequest);
    var result = planner.PlanTrajectory(request);
    
    if (!result.Success)
    {
        Console.WriteLine($"Planning failed: {result.ErrorMessage}");
    }
}
catch (PlanningException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
catch (FileNotFoundException ex)
{
    Console.WriteLine($"File not found: {ex.FileName}");
}
```

