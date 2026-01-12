# Test Execution Quick Reference

## Quick Start

```cmd
cd C:\Tools\RoboticLibrary\GitHub\RLCSWrapper\RLCSWrapper.Test\bin\Release\net10.0
RLCSWrapper.Test.exe test_example.rlkin.xml test_example.rlsg.xml
```

## Required Files

### Must Have:
- `RLCSWrapper.Test.exe`
- `RLWrapper.dll` + all RL DLLs (`rlplan.dll`, `rlsg.dll`, etc.)
- `test_example.rlkin.xml` (or provide path)
- `test_example.rlsg.xml` (or provide path)

### Optional (for Test 6):
- `test_plan.xml` (custom plan XML)
- `example_rrtConCon.xml` (example RRT-Connect plan XML)
- `example_prm.xml` (example PRM plan XML)
  - Note: Example files require `rlkin/` and `rlsg/` subdirectories with referenced files

## Command Syntax

```cmd
RLCSWrapper.Test.exe [kinematics.xml] [scene.xml]
```

## Success Output

Look for:
- ✅ All "✓" checkmarks
- ✅ "Planning succeeded!" messages
- ✅ Waypoint counts > 0
- ✅ "All tests completed successfully!" at end
- ✅ Exit code 0

## Common Issues

| Problem | Solution |
|---------|----------|
| File not found | Provide full paths: `RLCSWrapper.Test.exe "C:\path\to\file.xml"` |
| DLL missing | Check all DLLs are in executable directory |
| Planning failed | Try simpler start/goal, increase timeout |
| Test 6 skipped | Create plan XML file(s): `test_plan.xml`, `example_rrtConCon.xml`, or `example_prm.xml` |
| Example XML load fails | Check `rlkin/` and `rlsg/` subdirectories exist with referenced files |

## Test Overview

1. **Test 1-4**: High-level API (TrajectoryPlanner singleton)
2. **Test 5**: Low-level API (SetStart/SetGoal)
3. **Test 6**: LoadPlanXml (tests all found plan XML files: `test_plan.xml`, `example_rrtConCon.xml`, `example_prm.xml`)

## Expected Runtime

- **Total time**: 30 seconds - 5 minutes (depends on scene complexity)
- **Per trajectory**: 50-5000 ms
- **Total trajectories**: ~13-15 planning operations

## Exit Codes

- **0** = Success
- **1** = Error (check error messages)

## Full Documentation

See `TEST_EXECUTION_GUIDE.md` for detailed instructions.
