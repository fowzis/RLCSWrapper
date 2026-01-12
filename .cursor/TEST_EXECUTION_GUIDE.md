# RLCSWrapper Test Execution Guide

## Table of Contents
1. [Quick Start](#quick-start)
2. [Prerequisites](#prerequisites)
3. [Required Input Files](#required-input-files)
4. [Running the Tests](#running-the-tests)
5. [Expected Output](#expected-output)
6. [Command-Line Options](#command-line-options)
7. [Troubleshooting](#troubleshooting)

---

## Quick Start

### Basic Execution (Using Default Files)

1. Navigate to the test executable directory:
   ```cmd
   cd C:\Tools\RoboticLibrary\GitHub\RLCSWrapper\RLCSWrapper.Test\bin\Release\net10.0
   ```

2. Run the test executable:
   ```cmd
   RLCSWrapper.Test.exe
   ```

   **Note**: The test will look for default files (`scara_robot.xml` and `workspace_with_obstacles.xml`) in the current directory. If these don't exist, you'll need to provide paths to existing XML files `kinematics.rlkin.xml` and `scene.rlsg.xml`

### Using Custom Input Files

```cmd
RLCSWrapper.Test.exe test_example.rlkin.xml test_example.rlsg.xml
or
RLCSWrapper.Test.exe kinematics.rlkin.xml scene.rlsg.xml
```

---

## Prerequisites

### Required Files in Test Directory

The test executable directory (`bin/Release/net10.0`) should contain:

#### Essential Files:
- ✅ **RLCSWrapper.Test.exe** - Test executable
- ✅ **RLCSWrapper.Core.dll** - Core library
- ✅ **RLWrapper.dll** - Native wrapper DLL
- ✅ **RL library DLLs**: `rlplan.dll`, `rlsg.dll`, `rlkin.dll`, `rlmdl.dll`, `rlhal.dll`
- ✅ **Third-party DLLs**: Boost libraries, ODE, etc.

#### Input XML Files (at least one set):
- **Option 1**: Separate kinematics and scene files
  - `test_example.rlkin.xml` - Robot kinematics definition
  - `test_example.rlsg.xml` - Scene with obstacles
  
- **Option 2**: Plan XML file (for Test 6)
  - `test_plan.xml` - Plan XML that references kinematics and scene

#### Supporting Files:
- `test_example.wrl` - 3D model file referenced by scene XML
- `*.xsd` files - XML schema definitions (optional, for validation)

### Verify Files Are Present

Before running, check that these files exist:

```cmd
dir test_example.rlkin.xml
dir test_example.rlsg.xml
dir test_plan.xml
dir RLWrapper.dll
dir RLCSWrapper.Core.dll
```

---

## Required Input Files

### 1. Kinematics XML File (`test_example.rlkin.xml`)

**Purpose**: Defines robot joint structure, limits, and kinematics.

**Location**: Should be in the same directory as the executable, or provide full path.

**Example content structure**:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<rlkin>
    <kinematics>
        <revolute id="joint0">
            <max>180</max>
            <min>-180</min>
        </revolute>
        <!-- More joints... -->
    </kinematics>
</rlkin>
```

### 2. Scene XML File (`test_example.rlsg.xml`)

**Purpose**: Defines the scene with robot model and obstacles for collision checking.

**Location**: Should be in the same directory as the executable, or provide full path.

**Example content structure**:
```xml
<?xml version="1.0" encoding="UTF-8"?>
<rlsg>
    <scene href="test_example.wrl">
        <model name="robot">
            <!-- Robot bodies -->
        </model>
        <model name="obstacles">
            <!-- Obstacle bodies -->
        </model>
    </scene>
</rlsg>
```

**Note**: The scene XML references a `.wrl` file (VRML format) that contains the 3D geometry. Make sure `test_example.wrl` is also present.

### 3. Plan XML Files - Optional (for Test 6)

**Purpose**: Single XML files that reference both kinematics and scene, plus start/goal configurations and planner parameters.

**Location**: Should be in the same directory as the executable.

**Supported Plan XML Files**:
The test automatically detects and tests these plan XML files if they exist:
- `test_plan.xml` - Custom test plan XML
- `example_rrtConCon.xml` - Example RRT-Connect planner configuration
- `example_prm.xml` - Example PRM (Probabilistic Roadmap) planner configuration

**Example content structure** (`example_rrtConCon.xml`):
```xml
<?xml version="1.0" encoding="UTF-8"?>
<rlplan xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:noNamespaceSchemaLocation="rlplan.xsd">
    <rrtConCon>
        <duration>120</duration>
        <goal>
            <q unit="deg">50</q>
            <q unit="deg">-50</q>
        </goal>
        <model>
            <kinematics href="../rlkin/example.xml"/>
            <model>0</model>
            <scene href="../rlsg/example.xml"/>
        </model>
        <start>
            <q unit="deg">-90</q>
            <q unit="deg">50</q>
        </start>
        <delta unit="deg">1</delta>
        <epsilon>0.001</epsilon>
        <uniformSampler/>
    </rrtConCon>
</rlplan>
```

**Example content structure** (`example_prm.xml`):
```xml
<?xml version="1.0" encoding="UTF-8"?>
<rlplan xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:noNamespaceSchemaLocation="rlplan.xsd">
    <prm>
        <duration>120</duration>
        <goal>
            <q unit="deg">50</q>
            <q unit="deg">-50</q>
        </goal>
        <model>
            <kinematics href="../rlkin/example.xml"/>
            <model>0</model>
            <scene href="../rlsg/example.xml"/>
        </model>
        <start>
            <q unit="deg">-90</q>
            <q unit="deg">50</q>
        </start>
        <recursiveVerifier>
            <delta unit="deg">1</delta>
        </recursiveVerifier>
        <uniformSampler/>
    </prm>
</rlplan>
```

**Important Notes**:
- Plan XML files can reference kinematics/scene files using **relative paths**
- Paths are resolved relative to the plan XML file location
- Example files use `../rlkin/example.xml` and `../rlsg/example.xml` (parent directory structure)
- The test automatically detects the planner type from the filename (`prm`, `rrtConCon`, etc.)
- If referenced files don't exist at those paths, the test will fail with a load error

---

## Using Example Plan XML Files

The test directory includes example plan XML files (`example_rrtConCon.xml` and `example_prm.xml`) that demonstrate different planner configurations.

### Directory Structure for Example Files

The example plan XML files reference kinematics and scene files using relative paths:

```
bin/Release/net10.0/
├── example_rrtConCon.xml      (references ../rlkin/example.xml)
├── example_prm.xml             (references ../rlkin/example.xml)
├── rlkin/
│   └── example.xml            (kinematics file)
└── rlsg/
    ├── example.xml            (scene file)
    └── example.wrl            (3D geometry file)
```

**Note**: The `../` in the plan XML means "go up one directory", so from `bin/Release/net10.0/`, it goes to `bin/Release/`, then into `rlkin/` or `rlsg/`.

### Verifying Example File Structure

Before running tests with example plan XML files, verify the structure:

```cmd
cd C:\Tools\RoboticLibrary\GitHub\RLCSWrapper\RLCSWrapper.Test\bin\Release\net10.0

# Check plan XML files exist
dir example_*.xml

# Check referenced directories exist
dir rlkin\example.xml
dir rlsg\example.xml
dir rlsg\example.wrl
```

### What the Example Files Test

- **`example_rrtConCon.xml`**: Tests RRT-Connect planner with configurations from XML
- **`example_prm.xml`**: Tests PRM (Probabilistic Roadmap) planner with configurations from XML

Both files include:
- Start/goal configurations in degrees (automatically converted to radians)
- Planner-specific parameters (delta, epsilon, duration)
- References to kinematics and scene files
- Planner type specification (`rrtConCon` or `prm`)

### Creating Your Own Plan XML

You can create a custom `test_plan.xml` file. Example:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<rlplan xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:noNamespaceSchemaLocation="rlplan.xsd">
    <rrtConCon>
        <duration>30</duration>
        <goal>
            <q unit="deg">30</q>
            <q unit="deg">-30</q>
        </goal>
        <model>
            <kinematics href="test_example.rlkin.xml"/>
            <model>0</model>
            <scene href="test_example.rlsg.xml"/>
        </model>
        <start>
            <q unit="deg">0</q>
            <q unit="deg">0</q>
        </start>
        <delta unit="deg">1</delta>
        <epsilon>0.001</epsilon>
        <uniformSampler/>
    </rrtConCon>
</rlplan>
```

**Path Tips**:
- Use `filename.xml` for files in the same directory as plan XML
- Use `../subdir/file.xml` to reference files in parent/subdirectories
- Use absolute paths if needed (less portable)

---

## Running the Tests

### Method 1: Default Files (Current Directory)

If `test_example.rlkin.xml` and `test_example.rlsg.xml` are in the current directory:

```cmd
cd C:\Tools\RoboticLibrary\GitHub\RLCSWrapper\RLCSWrapper.Test\bin\Release\net10.0
RLCSWrapper.Test.exe
```

### Method 2: Specify File Paths

```cmd
RLCSWrapper.Test.exe test_example.rlkin.xml test_example.rlsg.xml
```

### Method 3: Using Full Paths

```cmd
RLCSWrapper.Test.exe "C:\path\to\kinematics.xml" "C:\path\to\scene.xml"
```

### Method 4: From Visual Studio

1. Set `RLCSWrapper.Test` as startup project
2. Set command-line arguments in project properties:
   - Right-click project → Properties → Debug → Application arguments
   - Enter: `test_example.rlkin.xml test_example.rlsg.xml`
3. Press F5 to run

### Method 5: Testing with Example Plan XML Files

If you have the example plan XML files set up:

```cmd
cd C:\Tools\RoboticLibrary\GitHub\RLCSWrapper\RLCSWrapper.Test\bin\Release\net10.0

# Run tests (Test 6 will automatically test example files if they exist)
RLCSWrapper.Test.exe test_example.rlkin.xml test_example.rlsg.xml
```

The test will automatically:
- Detect `example_rrtConCon.xml` and test it
- Detect `example_prm.xml` and test it
- Detect `test_plan.xml` and test it (if exists)

Each plan XML file is tested independently with its own planner instance.

---

## Expected Output

### Successful Execution Output

When the tests run successfully, you should see output similar to this:

```
RL Trajectory Planner Test Program
===================================

✓ TrajectoryPlanner singleton instance obtained

Initializing planner...
✓ Planner initialized successfully
  Robot DOF: 2

Testing configuration validation...
  Configuration [0, 0, ...] is valid

=== Test 1: 2D Planning (Z-axis fixed) ===
  Start: [0, 0]
  Goal:  [0.5, 0.5]
  Algorithm: RRTConnect
  Use Z-Axis: False
  Planning...
  ✓ Planning succeeded!
    Waypoints: 15
    Planning time: 234.56 ms
    First waypoint: [0, 0]
    Last waypoint:  [0.5, 0.5]

=== Test 2: 3D Planning (Z-axis variable) ===
  Start: [0, 0, 0]
  Goal:  [0.5, 0.5, 0.5]
  Algorithm: RRTConnect
  Use Z-Axis: True
  Planning...
  ✓ Planning succeeded!
    Waypoints: 18
    Planning time: 287.34 ms
    First waypoint: [0, 0, 0]
    Last waypoint:  [0.5, 0.5, 0.5]

=== Test 3: Different Planner Algorithms ===
  Testing RRT...
    ✓ RRT: 12 waypoints, 156.78 ms
  Testing RRTConnect...
    ✓ RRTConnect: 15 waypoints, 189.23 ms
  Testing RRTGoalBias...
    ✓ RRTGoalBias: 14 waypoints, 201.45 ms
  Testing PRM...
    ✓ PRM: 16 waypoints, 312.67 ms

=== Test 4: Multiple Trajectories (Scene Reuse) ===
  Planning multiple trajectories using the same loaded scene...
  Trajectory 1: ✓ Success (12 waypoints, 145.23 ms)
  Trajectory 2: ✓ Success (14 waypoints, 167.89 ms)
  Trajectory 3: ✓ Success (13 waypoints, 156.34 ms)
  ✓ Scene reused successfully - no reloading needed!

=== Test 5: Low-Level API - SetStart/SetGoal ===
  Testing SetStartConfiguration and SetGoalConfiguration...
    ✓ Planner created
    ✓ Kinematics and scene loaded
    ✓ Robot DOF: 2
    ✓ Start configuration set: [0, 0.1]
    ✓ Goal configuration set: [0.2, 0.3]
    ✓ Start config valid: True, Goal config valid: True
    Planning with stored configurations...
    ✓ Planning succeeded with 11 waypoints
      First waypoint: [0, 0.1]
      Last waypoint:  [0.2, 0.3]
    Testing configuration updates...
    ✓ Updated start: [0.3, 0.4]
    ✓ Updated goal:  [0.4, 0.5]
    ✓ Second planning succeeded with 9 waypoints
  ✓ SetStartConfiguration/SetGoalConfiguration tests completed

=== Test 6: LoadPlanXml ===

  Testing LoadPlanXml with: test_plan.xml
    ✓ Planner created
    ✓ Plan XML loaded successfully (detected planner: rrtConCon)
    ✓ Robot DOF: 2
    Planning with configurations from XML (using rrtConCon)...
    ✓ Planning succeeded with 13 waypoints
      First waypoint: [0, 0]
      Last waypoint:  [0.523599, -0.523599]
    Testing with new configurations...
    ✓ Planning with new configs succeeded: 10 waypoints

  Testing LoadPlanXml with: example_rrtConCon.xml
    ✓ Planner created
    ✓ Plan XML loaded successfully (detected planner: rrtConCon)
    ✓ Robot DOF: 2
    Planning with configurations from XML (using rrtConCon)...
    ✓ Planning succeeded with 15 waypoints
      First waypoint: [-1.5708, 0.872665]
      Last waypoint:  [0.872665, -0.872665]
    Testing with new configurations...
    ✓ Planning with new configs succeeded: 12 waypoints

  Testing LoadPlanXml with: example_prm.xml
    ✓ Planner created
    ✓ Plan XML loaded successfully (detected planner: prm)
    ✓ Robot DOF: 2
    Planning with configurations from XML (using prm)...
    ✓ Planning succeeded with 18 waypoints
      First waypoint: [-1.5708, 0.872665]
      Last waypoint:  [0.872665, -0.872665]
    Testing with new configurations...
    ✓ Planning with new configs succeeded: 14 waypoints

  ✓ LoadPlanXml tests completed

✓ All tests completed successfully!
```

### Output Explanation

#### Test 1-4: High-Level API Tests
- **Test 1**: Basic 2D planning (Z-axis fixed)
- **Test 2**: 3D planning (Z-axis variable) - Only runs if DOF >= 3
- **Test 3**: Tests different planner algorithms (RRT, RRTConnect, RRTGoalBias, PRM)
- **Test 4**: Multiple trajectories using the same loaded scene (demonstrates scene reuse)

#### Test 5: Low-Level API - SetStart/SetGoal
- Creates planner using `RLWrapper.CreatePlanner()`
- Loads kinematics and scene separately
- Tests `SetStartConfiguration()` and `SetGoalConfiguration()`
- Validates configurations
- Plans trajectories using stored configurations
- Updates configurations and plans again

#### Test 6: LoadPlanXml
- Creates planner using `RLWrapper.CreatePlanner()`
- Automatically tests all available plan XML files:
  - `test_plan.xml`
  - `example_rrtConCon.xml`
  - `example_prm.xml`
- For each plan XML file found:
  - Loads plan XML (includes kinematics, scene, start/goal, planner parameters)
  - Detects planner type from filename (PRM, RRTConCon, etc.)
  - Plans using configurations from XML
  - Sets new configurations and plans again
- If no plan XML files are found, test is skipped with a message

### Key Indicators of Success

✅ **Success indicators**:
- All tests show "✓" checkmarks
- "Planning succeeded" messages
- Waypoint counts > 0
- Planning times shown in milliseconds
- "All tests completed successfully!" at the end
- Exit code 0 (no error)

❌ **Failure indicators**:
- "✗" symbols
- "Planning failed" messages
- Error messages
- Exception stack traces
- Exit code 1

---

## Command-Line Options

### Syntax

```
RLCSWrapper.Test.exe [kinematics.xml] [scene.xml]
```

### Arguments

| Argument | Required | Description | Default |
|----------|----------|-------------|---------|
| `kinematics.xml` | No | Path to kinematics XML file | `scara_robot.xml` |
| `scene.xml` | No | Path to scene XML file | `workspace_with_obstacles.xml` |

### Examples

```cmd
# Use default file names (must exist in current directory)
RLCSWrapper.Test.exe

# Specify custom file names
RLCSWrapper.Test.exe robot.xml scene.xml

# Use full paths
RLCSWrapper.Test.exe "C:\Robots\puma560.xml" "C:\Scenes\workspace.xml"

# Use relative paths
RLCSWrapper.Test.exe ..\..\data\robot.xml ..\..\data\scene.xml
```

---

## Troubleshooting

### Problem: "Kinematics file not found"

**Symptoms**:
```
Error: Kinematics file not found: scara_robot.xml
Usage: RLCSWrapper.Test.exe [kinematics.xml] [scene.xml]
```

**Solutions**:
1. Provide the correct file path as first argument:
   ```cmd
   RLCSWrapper.Test.exe test_example.rlkin.xml test_example.rlsg.xml
   ```

2. Copy the file to the executable directory

3. Use full path:
   ```cmd
   RLCSWrapper.Test.exe "C:\full\path\to\kinematics.xml" "C:\full\path\to\scene.xml"
   ```

---

### Problem: "Scene file not found"

**Symptoms**:
```
Error: Scene file not found: workspace_with_obstacles.xml
```

**Solutions**:
1. Provide the correct file path as second argument
2. Ensure the scene XML file exists
3. Check that referenced `.wrl` file exists (scene XML may reference it)

---

### Problem: "Failed to load native library"

**Symptoms**:
```
Failed to load native library 'RLWrapper.dll'. 
Ensure the RLWrapper library and all dependencies...
```

**Solutions**:
1. Verify `RLWrapper.dll` is in the executable directory
2. Check that all RL DLLs are present:
   - `rlplan.dll`
   - `rlsg.dll`
   - `rlkin.dll`
   - `rlmdl.dll`
   - `rlhal.dll`
3. Ensure Boost and other third-party DLLs are present
4. Check Windows Event Viewer for missing DLL errors

---

### Problem: "Planning failed - no waypoints found"

**Symptoms**:
```
✗ Planning failed: Trajectory planning failed
```

**Possible Causes**:
1. **Start/goal configurations are invalid**:
   - Check joint limits
   - Verify configurations are collision-free
   - Use `IsValidConfiguration()` to test

2. **Obstacles blocking path**:
   - Check scene XML for obstacles
   - Try different start/goal positions
   - Increase timeout

3. **Timeout too short**:
   - Increase timeout in test code
   - Complex scenes may need more time

4. **Invalid planner parameters**:
   - Delta too large
   - Epsilon too small
   - Planner type not suitable

**Solutions**:
- Verify start/goal configurations are valid
- Try simpler start/goal positions
- Increase timeout duration
- Adjust delta/epsilon parameters

---

### Problem: "Planner not initialized"

**Symptoms**:
```
Planner is not initialized. Call Initialize() first.
```

**Solutions**:
- This shouldn't happen in the test program
- Indicates a bug - check that `Initialize()` is called before planning

---

### Problem: "Configuration length mismatch"

**Symptoms**:
```
Configuration length (3) must match robot DOF (2).
```

**Solutions**:
- Ensure configuration arrays match robot DOF
- Get DOF first: `int dof = planner.Dof;`
- Create arrays of correct size: `new double[dof]`

---

### Problem: Test 6 Skipped

**Symptoms**:
```
=== Test 6: LoadPlanXml ===
  Skipped: No plan XML files found.
  Available plan XML files: test_plan.xml, example_rrtConCon.xml, example_prm.xml
```

**Solutions**:
1. Ensure at least one plan XML file exists in the executable directory:
   - `test_plan.xml`
   - `example_rrtConCon.xml`
   - `example_prm.xml`

2. **For example files** (`example_rrtConCon.xml`, `example_prm.xml`):
   - These reference `../rlkin/example.xml` and `../rlsg/example.xml`
   - Ensure the directory structure exists:
     ```
     bin/Release/net10.0/
       ├── example_rrtConCon.xml
       ├── example_prm.xml
       ├── rlkin/
       │   └── example.xml
       └── rlsg/
           ├── example.xml
           └── example.wrl
     ```

3. **For custom `test_plan.xml`**:
   - Create it in the executable directory
   - Ensure it references valid kinematics and scene files
   - Use relative paths from the plan XML location
   - Example: If plan XML is in `bin/Release/net10.0/` and kinematics is in same directory:
     ```xml
     <kinematics href="test_example.rlkin.xml"/>
     <scene href="test_example.rlsg.xml"/>
     ```

4. **Path resolution**:
   - Paths in plan XML are resolved relative to the plan XML file location
   - `../rlkin/example.xml` means: go up one directory, then into `rlkin/example.xml`
   - `test_example.rlkin.xml` means: same directory as plan XML file

---

### Problem: Plan XML Load Fails (File Not Found)

**Symptoms**:
```
✗ Error in LoadPlanXml test: LoadPlanXml failed: Failed to load kinematics or scene
```

**Possible Causes**:
1. **Referenced files don't exist**:
   - Example files reference `../rlkin/example.xml` and `../rlsg/example.xml`
   - These paths may not exist in your directory structure

2. **Incorrect relative paths**:
   - Paths in plan XML are relative to the plan XML file location
   - `../rlkin/example.xml` from `bin/Release/net10.0/` goes to `bin/Release/rlkin/example.xml`

**Solutions**:

**Option 1: Create the expected directory structure**
```cmd
cd C:\Tools\RoboticLibrary\GitHub\RLCSWrapper\RLCSWrapper.Test\bin\Release\net10.0
mkdir rlkin
mkdir rlsg
copy test_example.rlkin.xml rlkin\example.xml
copy test_example.rlsg.xml rlsg\example.xml
copy test_example.wrl rlsg\example.wrl
```

**Option 2: Modify example XML files to use correct paths**
Edit `example_rrtConCon.xml` and `example_prm.xml` to reference files in the same directory:
```xml
<!-- Change from: -->
<kinematics href="../rlkin/example.xml"/>

<!-- To: -->
<kinematics href="test_example.rlkin.xml"/>
```

**Option 3: Create your own test_plan.xml**
Create `test_plan.xml` that references files you know exist:
```xml
<kinematics href="test_example.rlkin.xml"/>
<scene href="test_example.rlsg.xml"/>
```

---

### Problem: DLL Load Errors

**Symptoms**:
- Application crashes on startup
- "The specified module could not be found" error
- Missing DLL errors in Event Viewer

**Solutions**:
1. Install Visual C++ Redistributable:
   - Download from Microsoft
   - Install vc_redist.x64.exe

2. Verify all DLLs are present:
   ```cmd
   dir *.dll
   ```

3. Check DLL architecture matches:
   - All DLLs must be x64 (64-bit)
   - Executable must be x64

4. Use Dependency Walker to find missing DLLs:
   - Download Dependency Walker
   - Open `RLWrapper.dll`
   - Check for missing dependencies

---

## Verifying Successful Execution

### Step-by-Step Verification

1. **Check Initialization**:
   ```
   ✓ TrajectoryPlanner singleton instance obtained
   ✓ Planner initialized successfully
   Robot DOF: 2
   ```
   ✅ DOF should be > 0

2. **Check Configuration Validation**:
   ```
   Configuration [0, 0, ...] is valid
   ```
   ✅ Should show "valid"

3. **Check Planning Success**:
   ```
   ✓ Planning succeeded!
   Waypoints: 15
   ```
   ✅ Waypoint count should be > 0

4. **Check All Tests Complete**:
   ```
   ✓ All tests completed successfully!
   ```
   ✅ Should see this message at the end

5. **Check Exit Code**:
   ```cmd
   echo %ERRORLEVEL%
   ```
   ✅ Should be 0 (success)

### Expected Test Counts

- **Test 1**: 1 planning operation
- **Test 2**: 1 planning operation (if DOF >= 3)
- **Test 3**: 4 planning operations (one per algorithm)
- **Test 4**: 3 planning operations
- **Test 5**: 2 planning operations (low-level API)
- **Test 6**: 2 planning operations per plan XML file found
  - Tests `test_plan.xml` if exists
  - Tests `example_rrtConCon.xml` if exists  
  - Tests `example_prm.xml` if exists
  - Example: If all 3 files exist = 6 planning operations (2 per file)

**Total**: Approximately 13-21 planning operations depending on:
- DOF >= 3 (Test 2 runs)
- Number of plan XML files found (Test 6 runs 2 operations per file)
  - 1 file = ~13-15 operations
  - 2 files = ~15-17 operations
  - 3 files = ~17-19 operations

---

## Performance Expectations

### Typical Planning Times

- **Simple scenes**: 50-500 ms per trajectory
- **Complex scenes**: 500-5000 ms per trajectory
- **Timeout**: Tests use 10-30 second timeouts

### Factors Affecting Performance

1. **Scene complexity**: More obstacles = longer planning
2. **Start/goal distance**: Longer paths take more time
3. **Planner algorithm**: PRM typically slower than RRT variants
4. **Delta parameter**: Smaller delta = more precise but slower
5. **Hardware**: CPU speed affects planning time

---

## Advanced Usage

### Running Individual Tests

To test specific functionality, you can modify `Program.cs` to comment out unwanted tests:

```csharp
// Comment out tests you don't want to run
// TestPlannerAlgorithms(planner);
```

### Increasing Timeout

If planning times out frequently, increase timeout in test code:

```csharp
Timeout = TimeSpan.FromSeconds(60)  // Increase from 30 to 60 seconds
```

### Debugging Failed Planning

1. Check configuration validity:
   ```csharp
   bool isValid = planner.IsValidConfiguration(startConfig);
   ```

2. Try simpler configurations:
   ```csharp
   // Start closer to goal
   goal[i] = start[i] + 0.1;  // Small movement
   ```

3. Increase planner parameters:
   ```csharp
   Delta = 0.2,      // Larger step size
   Epsilon = 0.01,   // Larger tolerance
   ```

---

## Summary

### Quick Checklist

Before running tests, ensure:
- ✅ Executable directory contains all required DLLs
- ✅ Input XML files exist (kinematics and scene)
- ✅ Optional: Plan XML files exist for Test 6:
  - `test_plan.xml` (custom)
  - `example_rrtConCon.xml` (if using example files, ensure `rlkin/` and `rlsg/` subdirectories exist)
  - `example_prm.xml` (if using example files, ensure `rlkin/` and `rlsg/` subdirectories exist)
- ✅ Current directory is set to executable location
- ✅ If using example plan XML files, verify referenced files exist at the specified relative paths

### Running Tests

```cmd
cd C:\Tools\RoboticLibrary\GitHub\RLCSWrapper\RLCSWrapper.Test\bin\Release\net10.0
RLCSWrapper.Test.exe test_example.rlkin.xml test_example.rlsg.xml
```

### Success Criteria

- ✅ All tests show "✓" checkmarks
- ✅ Planning operations succeed (waypoints > 0)
- ✅ "All tests completed successfully!" message appears
- ✅ Exit code is 0
- ✅ No error messages or exceptions

### Getting Help

If tests fail:
1. Check error messages carefully
2. Verify all files are present
3. Check DLL dependencies
4. Review troubleshooting section above
5. Check that XML files are valid (use xmllint if available)

---

## Example Complete Session

```cmd
C:\> cd C:\Tools\RoboticLibrary\GitHub\RLCSWrapper\RLCSWrapper.Test\bin\Release\net10.0

C:\...\net10.0> dir test_example.*
 Volume in drive C has no label.
 Volume Serial Number is XXXX-XXXX

 Directory of C:\...\net10.0

2024-01-15  10:30 AM               472 test_example.rlsg.xml
2024-01-15  10:30 AM             1,600 test_example.rlkin.xml
2024-01-15  10:30 AM             2,700 test_example.wrl
               3 File(s)          4,772 bytes

C:\...\net10.0> dir example_*.xml
2024-01-15  10:30 AM             1,100 example_rrtConCon.xml
2024-01-15  10:30 AM             1,100 example_prm.xml
               2 File(s)          2,200 bytes

C:\...\net10.0> RLCSWrapper.Test.exe test_example.rlkin.xml test_example.rlsg.xml

RL Trajectory Planner Test Program
===================================

✓ TrajectoryPlanner singleton instance obtained

Initializing planner...
✓ Planner initialized successfully
  Robot DOF: 2

[... test output ...]

✓ All tests completed successfully!

C:\...\net10.0> echo %ERRORLEVEL%
0

C:\...\net10.0>
```

This indicates a successful test run! 🎉
