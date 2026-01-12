# How Collision-Free Trajectories Are Ensured

## Overview

The RL Trajectory Planner ensures collision-free trajectories through a multi-layered approach using the RL (Robotics Library) planning framework. The collision checking happens at multiple stages:

1. **Model Setup** - Scene and kinematics integration
2. **Initial Validation** - Start/goal configuration checking
3. **Planning Phase** - Planner uses verifier during path exploration
4. **Path Optimization** - Optimizer uses verifier to ensure optimized paths remain collision-free

## Architecture Components

### 1. SimpleModel (Planning Model)
**Location**: Created in `LoadScene()` (lines 224-257)

The `SimpleModel` integrates:
- **Kinematics** (`model->kin` or `model->mdl`): Defines robot joint structure and forward kinematics
- **Scene** (`model->scene`): Contains collision geometry (robot + obstacles)
- **Robot Model** (`model->model`): The robot's collision geometry in the scene

```cpp
state->model->kin = state->kinematics.get();  // Robot kinematics
state->model->scene = state->scene.get();     // Collision scene
state->model->model = state->robotModel;      // Robot geometry
```

This integration allows the model to:
- Check if configurations are within joint limits (`isValid()`)
- Check collision between robot and obstacles
- Update robot pose based on joint angles

### 2. RecursiveVerifier
**Location**: Created in `PlanTrajectory()` (lines 760-765)

The `RecursiveVerifier` is responsible for checking if paths (sequences of configurations) are collision-free:

```cpp
state->verifier = std::make_shared<rl::plan::RecursiveVerifier>();
state->verifier->delta = delta;  // Step size for recursive checking
state->verifier->model = state->model.get();
```

**How RecursiveVerifier Works**:
- Takes a path (list of waypoints)
- Recursively subdivides segments between waypoints
- Checks collision at each subdivision point using `delta` step size
- Returns `true` only if the entire path is collision-free

**Delta Parameter**: Controls the resolution of collision checking:
- Smaller `delta` = more thorough checking (slower but safer)
- Larger `delta` = faster checking (may miss small obstacles)

### 3. Planner Integration
**Location**: `PlanTrajectory()` (lines 793-808)

The planner (RRT, PRM, etc.) uses the verifier internally:

```cpp
rlPlanner->model = state->model.get();
// Planner internally uses verifier to check:
// - New configurations before adding to tree/roadmap
// - Path segments during exploration
// - Final path before returning
```

**During Planning**:
- When planner explores new configurations, it uses `model->isValid()` to check joint limits
- When connecting configurations, it uses the verifier to ensure the path segment is collision-free
- Only collision-free paths are added to the planning tree/roadmap

### 4. Initial Validation
**Location**: `PlanTrajectory()` (lines 820-824)

Before planning starts, both start and goal configurations are validated:

```cpp
rlPlanner->start = startVec;
rlPlanner->goal = goalVec;
if (!rlPlanner->verify())  // Checks if start/goal are valid and collision-free
{
    return RL_ERROR_PLANNING_FAILED;
}
```

The `verify()` function checks:
- Start configuration is valid (joint limits + collision-free)
- Goal configuration is valid (joint limits + collision-free)
- Both configurations are reachable

### 5. Path Optimization
**Location**: `PlanTrajectory()` (lines 838-850)

After planning, the path is optimized to reduce waypoints while maintaining collision-free guarantee:

```cpp
optimizer->model = state->model.get();
optimizer->verifier = state->verifier.get();  // Uses verifier to ensure optimized path is collision-free
optimizer->process(path);
```

**Optimization Process**:
- Tries to remove intermediate waypoints
- Uses verifier to check if direct connection between remaining waypoints is collision-free
- Only removes waypoints if the resulting path segment passes collision check
- Result: Shorter path that is still guaranteed collision-free

## Collision Checking Flow

### During Planning (RRT Example)

```
1. Planner generates random configuration q_rand
2. Find nearest configuration q_near in tree
3. Extend from q_near toward q_rand by delta distance → q_new
4. Check q_new:
   a. Joint limits: model->isValid(q_new)
   b. Collision: verifier->isValid([q_near, q_new])
5. If both pass, add q_new to tree
6. Repeat until goal is reached
```

### During Path Verification

```
1. Planner finds path: [q_start, q1, q2, ..., q_goal]
2. For each segment [q_i, q_i+1]:
   a. RecursiveVerifier subdivides segment by delta
   b. Checks collision at each subdivision point
   c. If any point is in collision, path is invalid
3. Only collision-free paths are returned
```

### During Optimization

```
1. Start with path: [q_start, q1, q2, q3, q_goal]
2. Try removing q2:
   a. Check direct path [q1, q3] with verifier
   b. If collision-free, remove q2
3. Result: [q_start, q1, q3, q_goal] (shorter, still collision-free)
```

## Key Safety Guarantees

1. **Start/Goal Validation**: Both configurations must be collision-free before planning starts
2. **Segment Checking**: Every path segment is verified using RecursiveVerifier with delta resolution
3. **Optimization Safety**: Optimizer only removes waypoints if resulting path remains collision-free
4. **Model Integration**: SimpleModel ensures robot geometry is correctly positioned for collision checks

## Delta Parameter Importance

The `delta` parameter (typically 0.1 radians or degrees) controls collision checking resolution:

- **Too Large**: May miss collisions between waypoints (unsafe)
- **Too Small**: Very slow checking, but very safe
- **Optimal**: Balance between safety and performance

The RecursiveVerifier uses delta to:
- Subdivide path segments for checking
- Ensure no collision is missed between waypoints
- Provide guaranteed collision-free paths

## Example: Collision-Free Path Generation

```
Input: Start = [0°, 0°], Goal = [90°, -90°]

1. Validate start/goal:
   ✓ Start is collision-free
   ✓ Goal is collision-free

2. Planner explores (RRT):
   - Generates random configurations
   - Checks each with model->isValid() (joint limits)
   - Checks connections with verifier->isValid() (collision)
   - Builds collision-free tree

3. Path found: [0°,0°] → [30°,10°] → [60°,-30°] → [90°,-90°]

4. Verify path segments:
   ✓ [0°,0°] to [30°,10°]: collision-free (checked at delta intervals)
   ✓ [30°,10°] to [60°,-30°]: collision-free
   ✓ [60°,-30°] to [90°,-90°]: collision-free

5. Optimize:
   - Try removing [30°,10°]: Check [0°,0°] to [60°,-30°]
   - If collision-free: Remove waypoint
   - Result: [0°,0°] → [60°,-30°] → [90°,-90°]

6. Return: Collision-free trajectory
```

## Summary

Collision-free trajectories are ensured through:

1. **Proper Model Setup**: SimpleModel integrates kinematics, scene, and robot geometry
2. **Verifier Integration**: RecursiveVerifier checks all path segments with delta resolution
3. **Planner Safety**: Planners only add collision-free configurations and connections
4. **Optimization Safety**: Optimizer maintains collision-free guarantee
5. **Multi-Stage Validation**: Start/goal validation + path verification + optimization verification

The system provides **guaranteed collision-free paths** by checking collision at every stage of the planning and optimization process.
