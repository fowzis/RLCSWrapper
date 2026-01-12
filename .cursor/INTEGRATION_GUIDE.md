# RLTrajectoryPlanner Integration Guide

This guide explains how to integrate the RLTrajectoryPlanner wrapper into your own C# .NET solution.

## Table of Contents

1. [Overview](#overview)
2. [Integration Methods](#integration-methods)
3. [Method 1: Project Reference](#method-1-project-reference)
4. [Method 2: NuGet Package](#method-2-nuget-package)
5. [Method 3: DLL Reference](#method-3-dll-reference)
6. [Native Library Deployment](#native-library-deployment)
7. [Usage Examples](#usage-examples)
8. [Configuration](#configuration)
9. [Troubleshooting](#troubleshooting)

---

## Overview

The RLTrajectoryPlanner wrapper provides a managed .NET API for trajectory planning using the RL (Robotics Library) C++ library. To use it in your project, you need:

1. **Managed Library:** `RLTrajectoryPlanner.Core.dll`
2. **Native Wrapper:** `RLWrapper.dll` (Windows), `libRLWrapper.so` (Linux), `libRLWrapper.dylib` (macOS)
3. **RL Native Libraries:** `rlplan.dll`, `rlkin.dll`, `rlsg.dll`, `rlmdl.dll`, `rlhal.dll`, etc.
4. **Dependencies:** Visual C++ Runtime, Boost, libxml2, collision detection libraries

---

## Integration Methods

There are three main ways to integrate RLTrajectoryPlanner into your solution:

1. **Project Reference** - Reference the source project directly (recommended for development)
2. **NuGet Package** - Install from NuGet (if available)
3. **DLL Reference** - Reference the built DLL directly

Choose the method that best fits your workflow.

---

## Method 1: Project Reference

This method is best when:
- You're actively developing both projects
- You need to debug into the wrapper code
- You want to modify the wrapper

### Steps

1. **Add Project Reference**

   In your `.csproj` file:
   ```xml
   <ItemGroup>
     <ProjectReference Include="..\RLTrajectoryPlanner\RLTrajectoryPlanner.Core\RLTrajectoryPlanner.Core.csproj" />
   </ItemGroup>
   ```

   Or in Visual Studio:
   - Right-click your project → Add → Project Reference
   - Select `RLTrajectoryPlanner.Core`

2. **Ensure Native Libraries Are Accessible**

   The wrapper will look for native libraries in:
   ```
   [YourApp]/bin/[Configuration]/net10.0/RLlib/[Platform]/
   ```

   Copy the native libraries structure:
   ```
   YourSolution/
   ├── YourProject/
   │   └── bin/
   │       └── Debug/
   │           └── net10.0/
   │               ├── YourApp.dll
   │               └── RLlib/
   │                   ├── Windows/
   │                   │   ├── RLWrapper.dll
   │                   │   ├── rlplan.dll
   │                   │   ├── rlkin.dll
   │                   │   └── ...
   │                   ├── Linux/
   │                   └── macOS/
   ```

3. **Add Post-Build Script (Optional)**

   To automatically copy native libraries during build, add to your `.csproj`:
   ```xml
   <Target Name="CopyNativeLibraries" AfterTargets="Build">
     <ItemGroup>
       <NativeLibs Include="$(SolutionDir)RLTrajectoryPlanner\RLlib\**\*.*" />
     </ItemGroup>
     <Copy SourceFiles="@(NativeLibs)" 
           DestinationFolder="$(OutputPath)RLlib\%(RecursiveDir)" 
           SkipUnchangedFiles="true" />
   </Target>
   ```

4. **Use the Library**

   ```csharp
   using RLTrajectoryPlanner.Core;
   using RLTrajectoryPlanner.Core.Models;
   
   var planner = TrajectoryPlanner.Instance;
   // ... rest of your code
   ```

---

## Method 2: NuGet Package

This method is best when:
- You want version management
- You're distributing your application
- You prefer package management

### Creating a NuGet Package (If Not Available)

If a NuGet package doesn't exist, you can create one:

1. **Add NuGet Package Properties**

   Create `RLTrajectoryPlanner.Core.nuspec`:
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <package xmlns="http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd">
     <metadata>
       <id>RLTrajectoryPlanner.Core</id>
       <version>1.0.0</version>
       <authors>Your Name</authors>
       <description>C# wrapper for RL trajectory planning library</description>
       <dependencies>
         <group targetFramework="net10.0" />
       </dependencies>
     </metadata>
     <files>
       <file src="bin\Release\net10.0\RLTrajectoryPlanner.Core.dll" target="lib\net10.0\" />
       <file src="bin\Release\net10.0\RLTrajectoryPlanner.Core.pdb" target="lib\net10.0\" />
       <file src="RLlib\**\*.*" target="content\RLlib\" />
     </files>
   </package>
   ```

2. **Pack the NuGet Package**

   ```powershell
   nuget pack RLTrajectoryPlanner.Core.nuspec
   ```

3. **Install in Your Project**

   ```powershell
   # From local file
   dotnet add package RLTrajectoryPlanner.Core --source ./packages
   
   # Or from NuGet feed
   dotnet add package RLTrajectoryPlanner.Core
   ```

### Using the NuGet Package

1. **Install Package**

   ```bash
   dotnet add package RLTrajectoryPlanner.Core
   ```

2. **Native Libraries**

   NuGet packages typically include native libraries in a `content` folder. Ensure they're copied to your output directory.

3. **Use the Library**

   ```csharp
   using RLTrajectoryPlanner.Core;
   using RLTrajectoryPlanner.Core.Models;
   ```

---

## Method 3: DLL Reference

This method is best when:
- You have a pre-built DLL
- You want minimal project dependencies
- You're integrating into an existing solution

### Steps

1. **Copy DLLs to Your Project**

   Create a `libs` or `packages` folder in your solution:
   ```
   YourSolution/
   ├── libs/
   │   └── RLTrajectoryPlanner.Core/
   │       ├── RLTrajectoryPlanner.Core.dll
   │       ├── RLTrajectoryPlanner.Core.pdb
   │       └── RLlib/
   │           ├── Windows/
   │           ├── Linux/
   │           └── macOS/
   ```

2. **Add DLL Reference**

   In your `.csproj`:
   ```xml
   <ItemGroup>
     <Reference Include="RLTrajectoryPlanner.Core">
       <HintPath>..\libs\RLTrajectoryPlanner.Core\RLTrajectoryPlanner.Core.dll</HintPath>
     </Reference>
   </ItemGroup>
   ```

   Or in Visual Studio:
   - Right-click References → Add Reference → Browse
   - Select `RLTrajectoryPlanner.Core.dll`

3. **Copy Native Libraries to Output**

   Add a post-build step or manually copy:
   ```xml
   <Target Name="CopyNativeLibraries" AfterTargets="Build">
     <ItemGroup>
       <NativeLibs Include="$(SolutionDir)libs\RLTrajectoryPlanner.Core\RLlib\**\*.*" />
     </ItemGroup>
     <Copy SourceFiles="@(NativeLibs)" 
           DestinationFolder="$(OutputPath)RLlib\%(RecursiveDir)" 
           SkipUnchangedFiles="true" />
   </Target>
   ```

4. **Use the Library**

   ```csharp
   using RLTrajectoryPlanner.Core;
   using RLTrajectoryPlanner.Core.Models;
   ```

---

## Native Library Deployment

Regardless of integration method, native libraries must be accessible at runtime. The wrapper searches for libraries in this order:

1. `[AppBase]/RLlib/[Platform]/` (relative to executable)
2. System PATH (Windows) or `LD_LIBRARY_PATH` (Linux) or `DYLD_LIBRARY_PATH` (macOS)
3. Current directory

### Deployment Strategies

#### Strategy 1: Copy to Output Directory (Recommended)

Copy native libraries to your application's output directory:

```
YourApp/
└── bin/
    └── Release/
        └── net10.0/
            ├── YourApp.dll
            └── RLlib/
                ├── Windows/
                │   ├── RLWrapper.dll
                │   ├── rlplan.dll
                │   └── ...
                ├── Linux/
                └── macOS/
```

**Advantages:**
- Self-contained deployment
- No system PATH modifications needed
- Works well for distribution

**Implementation:**

Add to your `.csproj`:
```xml
<ItemGroup>
  <Content Include="RLlib\**\*.*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

#### Strategy 2: System-Wide Installation

Install native libraries to a system directory:

**Windows:**
- Copy DLLs to `C:\Windows\System32\` (x64) or `C:\Windows\SysWOW64\` (x86)
- Or add to PATH environment variable

**Linux:**
- Copy to `/usr/local/lib/`
- Run `ldconfig` to update library cache
- Or set `LD_LIBRARY_PATH`

**macOS:**
- Copy to `/usr/local/lib/`
- Or set `DYLD_LIBRARY_PATH`

**Advantages:**
- Shared across applications
- Single installation point

**Disadvantages:**
- Requires administrator privileges
- Version conflicts possible
- Not recommended for distribution

#### Strategy 3: Custom Path

Set library path programmatically:

```csharp
using System;
using System.Runtime.InteropServices;

// Before using TrajectoryPlanner
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    string libPath = @"C:\MyApp\NativeLibs\Windows";
    Environment.SetEnvironmentVariable("PATH", 
        $"{libPath};{Environment.GetEnvironmentVariable("PATH")}", 
        EnvironmentVariableTarget.Process);
}
```

---

## Usage Examples

### Basic Integration

```csharp
using System;
using RLTrajectoryPlanner.Core;
using RLTrajectoryPlanner.Core.Models;

namespace YourApp
{
    public class RobotController
    {
        private TrajectoryPlanner _planner;

        public RobotController()
        {
            _planner = TrajectoryPlanner.Instance;
        }

        public void Initialize(string kinematicsPath, string scenePath)
        {
            _planner.Initialize(new InitializationRequest
            {
                KinematicsXmlPath = kinematicsPath,
                SceneXmlPath = scenePath,
                RobotModelIndex = 0
            });
        }

        public PlanningResult PlanPath(double[] start, double[] goal)
        {
            var request = new PlanningRequest
            {
                StartConfiguration = start,
                GoalConfiguration = goal,
                UseZAxis = false,
                Algorithm = PlannerType.RRTConnect,
                Delta = 0.1,
                Epsilon = 0.001,
                Timeout = TimeSpan.FromSeconds(30)
            };

            return _planner.PlanTrajectory(request);
        }
    }
}
```

### ASP.NET Core Integration

```csharp
using RLTrajectoryPlanner.Core;
using RLTrajectoryPlanner.Core.Models;

// In Startup.cs or Program.cs
public void ConfigureServices(IServiceCollection services)
{
    // Register as singleton (wrapper is already singleton)
    services.AddSingleton<TrajectoryPlanner>(sp => TrajectoryPlanner.Instance);
    
    // Or register your service that uses it
    services.AddScoped<IRobotPlanningService, RobotPlanningService>();
}

// Service implementation
public class RobotPlanningService : IRobotPlanningService
{
    private readonly TrajectoryPlanner _planner;
    private readonly ILogger<RobotPlanningService> _logger;

    public RobotPlanningService(TrajectoryPlanner planner, ILogger<RobotPlanningService> logger)
    {
        _planner = planner;
        _logger = logger;
    }

    public async Task<PlanningResult> PlanAsync(PlanningRequest request)
    {
        if (!_planner.IsInitialized)
        {
            throw new InvalidOperationException("Planner not initialized");
        }

        return await Task.Run(() => _planner.PlanTrajectory(request));
    }
}
```

### WPF Application Integration

```csharp
using System.Windows;
using RLTrajectoryPlanner.Core;
using RLTrajectoryPlanner.Core.Models;

public partial class MainWindow : Window
{
    private TrajectoryPlanner _planner;

    public MainWindow()
    {
        InitializeComponent();
        _planner = TrajectoryPlanner.Instance;
        
        // Initialize on startup
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _planner.Initialize(new InitializationRequest
            {
                KinematicsXmlPath = "robot.xml",
                SceneXmlPath = "scene.xml",
                RobotModelIndex = 0
            });
            
            StatusLabel.Content = $"Planner initialized (DOF: {_planner.Dof})";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to initialize planner: {ex.Message}", "Error");
        }
    }

    private void PlanButton_Click(object sender, RoutedEventArgs e)
    {
        var request = new PlanningRequest
        {
            StartConfiguration = ParseConfig(StartTextBox.Text),
            GoalConfiguration = ParseConfig(GoalTextBox.Text),
            Algorithm = (PlannerType)AlgorithmComboBox.SelectedItem,
            Timeout = TimeSpan.FromSeconds(30)
        };

        var result = _planner.PlanTrajectory(request);
        
        if (result.Success)
        {
            ResultTextBox.Text = $"Planned {result.Waypoints.Count} waypoints";
        }
        else
        {
            ResultTextBox.Text = $"Failed: {result.ErrorMessage}";
        }
    }
}
```

### Console Application Integration

```csharp
using System;
using RLTrajectoryPlanner.Core;
using RLTrajectoryPlanner.Core.Models;

class Program
{
    static void Main(string[] args)
    {
        var planner = TrajectoryPlanner.Instance;

        try
        {
            // Initialize
            planner.Initialize(new InitializationRequest
            {
                KinematicsXmlPath = args[0],
                SceneXmlPath = args[1],
                RobotModelIndex = 0
            });

            Console.WriteLine($"Planner initialized. DOF: {planner.Dof}");

            // Plan trajectory
            var result = planner.PlanTrajectory(new PlanningRequest
            {
                StartConfiguration = new double[] { 0.0, 0.0, 0.1 },
                GoalConfiguration = new double[] { 1.0, 1.0, 0.1 },
                Algorithm = PlannerType.RRTConnect,
                Timeout = TimeSpan.FromSeconds(30)
            });

            if (result.Success)
            {
                Console.WriteLine($"Success! {result.Waypoints.Count} waypoints");
            }
            else
            {
                Console.WriteLine($"Failed: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
```

---

## Configuration

### Target Framework

The wrapper targets .NET 10.0. Ensure your project targets a compatible framework:

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
</PropertyGroup>
```

### Platform Target

Match your platform target with the native library architecture:

```xml
<PropertyGroup>
  <PlatformTarget>x64</PlatformTarget>  <!-- or x86, AnyCPU -->
</PropertyGroup>
```

**Important:** If using `AnyCPU`, ensure you have both x64 and x86 native libraries available, or prefer x64.

### Unsafe Code

The wrapper uses unsafe code blocks. Your project must allow unsafe code:

```xml
<PropertyGroup>
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>
```

---

## Troubleshooting

### DLL Not Found at Runtime

**Symptoms:**
```
DllNotFoundException: Failed to load native library 'RLWrapper.dll'
```

**Solutions:**

1. **Verify library location:**
   - Check that `RLlib/[Platform]/RLWrapper.dll` exists relative to your executable
   - Verify all RL DLLs are present

2. **Check architecture:**
   - Ensure native libraries match your application architecture (x64/x86)
   - Check .NET application platform target

3. **Set library path:**
   ```csharp
   // Before using TrajectoryPlanner
   string libPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RLlib", "Windows");
   Environment.SetEnvironmentVariable("PATH", 
       $"{libPath};{Environment.GetEnvironmentVariable("PATH")}", 
       EnvironmentVariableTarget.Process);
   ```

### BadImageFormatException

**Symptoms:**
```
BadImageFormatException: An attempt was made to load a program with an incorrect format
```

**Solutions:**

1. **Architecture mismatch:**
   - Ensure all components are built for the same architecture
   - Check native libraries are x64 if your app is x64

2. **Platform target:**
   ```xml
   <PropertyGroup>
     <PlatformTarget>x64</PlatformTarget>  <!-- Explicitly set -->
   </PropertyGroup>
   ```

### Planning Always Fails

**Symptoms:**
- `PlanningResult.Success` is always `false`
- Error messages indicate planning failed

**Solutions:**

1. **Verify initialization:**
   ```csharp
   if (!planner.IsInitialized)
   {
       throw new InvalidOperationException("Planner not initialized");
   }
   ```

2. **Check configurations:**
   ```csharp
   bool isValidStart = planner.IsValidConfiguration(startConfig);
   bool isValidGoal = planner.IsValidConfiguration(goalConfig);
   ```

3. **Validate XML files:**
   - Ensure kinematics XML is valid
   - Ensure scene XML is valid
   - Check robot model index matches scene

4. **Adjust parameters:**
   ```csharp
   var request = new PlanningRequest
   {
       // ... other properties
       Delta = 0.05,        // Smaller step size
       Epsilon = 0.0001,    // Tighter tolerance
       Timeout = TimeSpan.FromSeconds(60)  // Longer timeout
   };
   ```

### Thread Safety

The `TrajectoryPlanner` singleton is thread-safe for:
- Multiple concurrent `PlanTrajectory` calls
- Concurrent `IsValidConfiguration` calls

However, initialization should be done once before any planning calls.

**Best Practice:**
```csharp
// Initialize once at application startup
public class AppInitializer
{
    public static void Initialize()
    {
        var planner = TrajectoryPlanner.Instance;
        planner.Initialize(new InitializationRequest { ... });
    }
}

// Use anywhere
var planner = TrajectoryPlanner.Instance;
var result = planner.PlanTrajectory(request);  // Thread-safe
```

---

## Best Practices

1. **Initialize Once:**
   - Initialize the planner once at application startup
   - Don't reinitialize unless necessary

2. **Error Handling:**
   ```csharp
   try
   {
       var result = planner.PlanTrajectory(request);
       if (!result.Success)
       {
           // Handle planning failure
           Logger.LogWarning($"Planning failed: {result.ErrorMessage}");
       }
   }
   catch (PlanningException ex)
   {
       // Handle planning exceptions
       Logger.LogError(ex, "Planning exception occurred");
   }
   ```

3. **Resource Management:**
   - The singleton manages its own resources
   - Call `Dispose()` only if you need to reinitialize
   - Generally, let the singleton live for the application lifetime

4. **Configuration Validation:**
   ```csharp
   // Validate before planning
   if (!planner.IsValidConfiguration(startConfig))
   {
       throw new ArgumentException("Start configuration is invalid");
   }
   ```

5. **Performance:**
   - Reuse the same planner instance (singleton pattern)
   - Scene and kinematics are loaded once and reused
   - Multiple planning calls are efficient

---

## Summary

To integrate RLTrajectoryPlanner into your .NET solution:

1. ✅ Choose integration method (Project Reference, NuGet, or DLL Reference)
2. ✅ Add reference to `RLTrajectoryPlanner.Core`
3. ✅ Deploy native libraries (`RLWrapper.dll` and RL DLLs)
4. ✅ Initialize planner once at startup
5. ✅ Use `TrajectoryPlanner.Instance` for planning

The wrapper provides a clean, managed API for trajectory planning while handling all the complexity of native library interop internally.
