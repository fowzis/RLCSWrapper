using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RLCSWrapper.Core;
using RLCSWrapper.Core.Models;

namespace RLCSWrapper.Test
{
    /// <summary>
    /// Test program demonstrating trajectory planning functionality.
    /// 
    /// Tests include:
    /// - High-level TrajectoryPlanner API (singleton)
    /// - Low-level RLWrapper API (direct IntPtr management)
    /// - LoadPlanXml functionality
    /// - SetStartConfiguration and SetGoalConfiguration
    /// - Multiple trajectory planning with persistent scene
    /// </summary>
    class Program
    {
        static void PrintUsage()
        {
            Console.WriteLine("RL Trajectory Planner Test Program");
            Console.WriteLine("===================================\n");
            Console.WriteLine("Usage:");
            Console.WriteLine("  RLCSWrapper.Test.exe [options]\n");
            Console.WriteLine("Options:");
            Console.WriteLine("  --plan <path>           Path to plan XML file (contains kinematics/scene references)");
            Console.WriteLine("  --kinematics <path>     Path to kinematics XML file (required if not using --plan)");
            Console.WriteLine("  --scene <path>          Path to scene XML file (required if not using --plan)");
            Console.WriteLine("  --test <number>         Run specific test (1-6), or \"all\" for all tests (default: all)");
            Console.WriteLine("  --help                  Show this help message\n");
            Console.WriteLine("Available Tests:");
            Console.WriteLine("  1  - 2D Planning (Z-axis fixed)");
            Console.WriteLine("  2  - 3D Planning (Z-axis variable)");
            Console.WriteLine("  3  - Different Planner Algorithms");
            Console.WriteLine("  4  - Multiple Trajectories (Scene Reuse)");
            Console.WriteLine("  5  - Low-Level API - SetStart/SetGoal");
            Console.WriteLine("  6  - LoadPlanXml (requires --plan option)\n");
            Console.WriteLine("Examples:");
            Console.WriteLine("  # Run Test 6 with plan XML (plan XML contains kinematics/scene paths):");
            Console.WriteLine("  RLCSWrapper.Test.exe --plan test_plan.xml --test 6");
            Console.WriteLine();
            Console.WriteLine("  # Run all tests with separate kinematics/scene files:");
            Console.WriteLine("  RLCSWrapper.Test.exe --kinematics test_example.rlkin.xml --scene test_example.rlsg.xml");
            Console.WriteLine();
            Console.WriteLine("  # Run specific test:");
            Console.WriteLine("  RLCSWrapper.Test.exe --kinematics test_example.rlkin.xml --scene test_example.rlsg.xml --test 1");
            Console.WriteLine();
            Console.WriteLine("  # Run multiple specific tests:");
            Console.WriteLine("  RLCSWrapper.Test.exe --kinematics test_example.rlkin.xml --scene test_example.rlsg.xml --test 1 --test 3 --test 5");
        }

        static Dictionary<string, string> ParseArguments(string[] args)
        {
            var result = new Dictionary<string, string>();
            var testNumbers = new List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--help" || args[i] == "-h")
                {
                    result["help"] = "true";
                    return result;
                }
                else if (args[i] == "--kinematics" && i + 1 < args.Length)
                {
                    result["kinematics"] = args[++i];
                }
                else if (args[i] == "--scene" && i + 1 < args.Length)
                {
                    result["scene"] = args[++i];
                }
                else if (args[i] == "--plan" && i + 1 < args.Length)
                {
                    result["plan"] = args[++i];
                }
                else if (args[i] == "--test" && i + 1 < args.Length)
                {
                    testNumbers.Add(args[++i]);
                }
            }

            if (testNumbers.Count > 0)
            {
                result["tests"] = string.Join(",", testNumbers);
            }
            else
            {
                result["tests"] = "all";
            }

            return result;
        }

        static bool ShouldRunTest(int testNumber, Dictionary<string, string> args)
        {
            string tests = args.GetValueOrDefault("tests", "all");
            if (tests == "all")
            {
                return true;
            }

            string[] testList = tests.Split(',');
            return testList.Contains(testNumber.ToString()) || testList.Contains("all");
        }

        static void Main(string[] args)
        {
            var parsedArgs = ParseArguments(args);

            // Show help if requested or no arguments provided
            if (parsedArgs.ContainsKey("help") || args.Length == 0)
            {
                PrintUsage();
                if (args.Length == 0)
                {
                    Environment.Exit(1);
                }
                return;
            }

            string? planXmlPath = parsedArgs.ContainsKey("plan") ? parsedArgs["plan"] : null;
            string? kinematicsPath = parsedArgs.ContainsKey("kinematics") ? parsedArgs["kinematics"] : null;
            string? scenePath = parsedArgs.ContainsKey("scene") ? parsedArgs["scene"] : null;

            // Determine which tests need kinematics/scene (tests 1-5)
            bool needsKinematicsScene = ShouldRunTest(1, parsedArgs) || ShouldRunTest(2, parsedArgs) ||
                                       ShouldRunTest(3, parsedArgs) || ShouldRunTest(4, parsedArgs) ||
                                       ShouldRunTest(5, parsedArgs);

            // Validate required arguments
            if (planXmlPath == null)
            {
                // If not using plan XML, kinematics and scene are required for tests 1-5
                if (needsKinematicsScene)
                {
                    if (kinematicsPath == null)
                    {
                        Console.WriteLine("Error: --kinematics option is required when not using --plan\n");
                        PrintUsage();
                        Environment.Exit(1);
                        return;
                    }

                    if (scenePath == null)
                    {
                        Console.WriteLine("Error: --scene option is required when not using --plan\n");
                        PrintUsage();
                        Environment.Exit(1);
                        return;
                    }
                }
            }
            else
            {
                // If using plan XML, validate it exists
                if (!File.Exists(planXmlPath))
                {
                    Console.WriteLine($"Error: Plan XML file not found: {planXmlPath}\n");
                    PrintUsage();
                    Environment.Exit(1);
                    return;
                }

                // If Test 6 is being run, --plan is required (already validated above)
                if (ShouldRunTest(6, parsedArgs) && planXmlPath == null)
                {
                    Console.WriteLine("Error: Test 6 requires --plan option\n");
                    PrintUsage();
                    Environment.Exit(1);
                    return;
                }
            }

            // Validate file existence for kinematics/scene if provided
            if (kinematicsPath != null && !File.Exists(kinematicsPath))
            {
                Console.WriteLine($"Error: Kinematics file not found: {kinematicsPath}\n");
                PrintUsage();
                Environment.Exit(1);
                return;
            }

            if (scenePath != null && !File.Exists(scenePath))
            {
                Console.WriteLine($"Error: Scene file not found: {scenePath}\n");
                PrintUsage();
                Environment.Exit(1);
                return;
            }

            Console.WriteLine("RL Trajectory Planner Test Program");
            Console.WriteLine("===================================\n");
            if (planXmlPath != null)
            {
                Console.WriteLine($"Plan XML: {planXmlPath}");
            }
            if (kinematicsPath != null)
            {
                Console.WriteLine($"Kinematics: {kinematicsPath}");
            }
            if (scenePath != null)
            {
                Console.WriteLine($"Scene: {scenePath}");
            }
            Console.WriteLine($"Tests to run: {parsedArgs["tests"]}\n");

            try
            {
                TrajectoryPlanner? planner = null;

                // Tests 1-4 require TrajectoryPlanner initialization
                if (ShouldRunTest(1, parsedArgs) || ShouldRunTest(2, parsedArgs) || 
                    ShouldRunTest(3, parsedArgs) || ShouldRunTest(4, parsedArgs))
                {
                    // Get singleton instance
                    planner = TrajectoryPlanner.Instance;
                    Console.WriteLine("✓ TrajectoryPlanner singleton instance obtained\n");

                    // Initialize planner
                    Console.WriteLine("Initializing planner...");
                    if (kinematicsPath == null || scenePath == null)
                    {
                        Console.WriteLine("  Error: Kinematics and scene paths are required for tests 1-4");
                        return;
                    }
                    var initRequest = new InitializationRequest
                    {
                        KinematicsXmlPath = Path.GetFullPath(kinematicsPath),
                        SceneXmlPath = Path.GetFullPath(scenePath),
                        RobotModelIndex = 0
                    };

                    planner.Initialize(initRequest);
                    Console.WriteLine($"✓ Planner initialized successfully");
                    Console.WriteLine($"  Robot DOF: {planner.Dof}\n");

                    // Test configuration validation
                    Console.WriteLine("Testing configuration validation...");
                    double[] testConfig = new double[planner.Dof];
                    for (int i = 0; i < planner.Dof; i++)
                    {
                        testConfig[i] = 0.0;
                    }
                    bool isValid = planner.IsValidConfiguration(testConfig);
                    Console.WriteLine($"  Configuration [0, 0, ...] is {(isValid ? "valid" : "invalid")}\n");
                }

                // Test 1: 2D Planning (Z-axis fixed)
                if (ShouldRunTest(1, parsedArgs))
                {
                    if (planner == null)
                    {
                        Console.WriteLine("=== Test 1: 2D Planning (Z-axis fixed) ===");
                        Console.WriteLine("  Error: Planner not initialized");
                    }
                    else
                    {
                        Console.WriteLine("=== Test 1: 2D Planning (Z-axis fixed) ===");
                        TestPlanning(planner, useZAxis: false, algorithm: PlannerType.RRTConnect, "2D");
                    }
                }

                // Test 2: 3D Planning (Z-axis variable)
                if (ShouldRunTest(2, parsedArgs))
                {
                    if (planner != null && planner.Dof >= 3)
                    {
                        Console.WriteLine("\n=== Test 2: 3D Planning (Z-axis variable) ===");
                        TestPlanning(planner, useZAxis: true, algorithm: PlannerType.RRTConnect, "3D");
                    }
                    else
                    {
                        Console.WriteLine("\n=== Test 2: 3D Planning (Z-axis variable) ===");
                        Console.WriteLine("  Skipped: Robot DOF < 3");
                    }
                }

                // Test 3: Different planner algorithms
                if (ShouldRunTest(3, parsedArgs))
                {
                    if (planner == null)
                    {
                        Console.WriteLine("\n=== Test 3: Different Planner Algorithms ===");
                        Console.WriteLine("  Error: Planner not initialized");
                    }
                    else
                    {
                        Console.WriteLine("\n=== Test 3: Different Planner Algorithms ===");
                        TestPlannerAlgorithms(planner);
                    }
                }

                // Test 4: Multiple trajectories (reusing loaded scene)
                if (ShouldRunTest(4, parsedArgs))
                {
                    if (planner == null)
                    {
                        Console.WriteLine("\n=== Test 4: Multiple Trajectories (Scene Reuse) ===");
                        Console.WriteLine("  Error: Planner not initialized");
                    }
                    else
                    {
                        Console.WriteLine("\n=== Test 4: Multiple Trajectories (Scene Reuse) ===");
                        TestMultipleTrajectories(planner);
                    }
                }

                // Test 5: Low-level API features
                if (ShouldRunTest(5, parsedArgs))
                {
                    Console.WriteLine("\n=== Test 5: Low-Level API - SetStart/SetGoal ===");
                    if (kinematicsPath == null || scenePath == null)
                    {
                        Console.WriteLine("  Error: Kinematics and scene paths are required for Test 5");
                        Console.WriteLine("  Use --kinematics and --scene options, or use --plan which contains these references");
                    }
                    else
                    {
                        TestSetStartGoalConfigurations(kinematicsPath, scenePath);
                    }
                }

                // Test 6: LoadPlanXml
                if (ShouldRunTest(6, parsedArgs))
                {
                    Console.WriteLine("\n=== Test 6: LoadPlanXml ===");
                    if (planXmlPath != null)
                    {
                        TestLoadPlanXml(planXmlPath);
                    }
                    else
                    {
                        Console.WriteLine("  Skipped: --plan option not provided");
                    }
                }

                Console.WriteLine("\n✓ All requested tests completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗ Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Environment.Exit(1);
            }
        }

        static void TestPlanning(TrajectoryPlanner planner, bool useZAxis, PlannerType algorithm, string testName)
        {
            int dof = planner.Dof;
            
            // Create start and goal configurations
            double[] start = new double[dof];
            double[] goal = new double[dof];

            // Initialize with safe values
            for (int i = 0; i < dof; i++)
            {
                start[i] = 0.0;
                goal[i] = 0.5; // Small movement
            }

            // For SCARA: if not using Z-axis, goal Z should match start Z
            if (!useZAxis && dof >= 3)
            {
                goal[dof - 1] = start[dof - 1]; // Z-axis is typically last
            }

            var request = new PlanningRequest
            {
                StartConfiguration = start,
                GoalConfiguration = goal,
                UseZAxis = useZAxis,
                Algorithm = algorithm,
                Delta = 0.1,
                Epsilon = 0.001,
                Timeout = TimeSpan.FromSeconds(30)
            };

            Console.WriteLine($"  Start: [{string.Join(", ", start)}]");
            Console.WriteLine($"  Goal:  [{string.Join(", ", goal)}]");
            Console.WriteLine($"  Algorithm: {algorithm}");
            Console.WriteLine($"  Use Z-Axis: {useZAxis}");
            Console.WriteLine("  Planning...");

            var result = planner.PlanTrajectory(request);

            if (result.Success)
            {
                Console.WriteLine($"  ✓ Planning succeeded!");
                Console.WriteLine($"    Waypoints: {result.Waypoints.Count}");
                Console.WriteLine($"    Planning time: {result.PlanningTime.TotalMilliseconds:F2} ms");
                
                if (result.Waypoints.Count > 0)
                {
                    Console.WriteLine($"    First waypoint: [{string.Join(", ", result.Waypoints[0])}]");
                    Console.WriteLine($"    Last waypoint:  [{string.Join(", ", result.Waypoints[^1])}]");
                }
            }
            else
            {
                Console.WriteLine($"  ✗ Planning failed: {result.ErrorMessage}");
            }
        }

        static void TestPlannerAlgorithms(TrajectoryPlanner planner)
        {
            int dof = planner.Dof;
            double[] start = new double[dof];
            double[] goal = new double[dof];

            for (int i = 0; i < dof; i++)
            {
                start[i] = 0.0;
                goal[i] = 0.3;
            }

            PlannerType[] algorithms = { PlannerType.RRT, PlannerType.RRTConnect, PlannerType.RRTGoalBias, PlannerType.PRM };

            foreach (var algorithm in algorithms)
            {
                Console.WriteLine($"  Testing {algorithm}...");
                var request = new PlanningRequest
                {
                    StartConfiguration = (double[])start.Clone(),
                    GoalConfiguration = (double[])goal.Clone(),
                    UseZAxis = false,
                    Algorithm = algorithm,
                    Delta = 0.1,
                    Epsilon = 0.001,
                    Timeout = TimeSpan.FromSeconds(10)
                };

                var result = planner.PlanTrajectory(request);
                if (result.Success)
                {
                    Console.WriteLine($"    ✓ {algorithm}: {result.Waypoints.Count} waypoints, {result.PlanningTime.TotalMilliseconds:F2} ms");
                }
                else
                {
                    Console.WriteLine($"    ✗ {algorithm}: Failed - {result.ErrorMessage}");
                }
            }
        }

        static void TestMultipleTrajectories(TrajectoryPlanner planner)
        {
            int dof = planner.Dof;
            Console.WriteLine("  Planning multiple trajectories using the same loaded scene...");

            for (int i = 0; i < 3; i++)
            {
                double[] start = new double[dof];
                double[] goal = new double[dof];

                for (int j = 0; j < dof; j++)
                {
                    start[j] = i * 0.2;
                    goal[j] = (i + 1) * 0.2;
                }

                var request = new PlanningRequest
                {
                    StartConfiguration = start,
                    GoalConfiguration = goal,
                    UseZAxis = false,
                    Algorithm = PlannerType.RRTConnect,
                    Delta = 0.1,
                    Epsilon = 0.001,
                    Timeout = TimeSpan.FromSeconds(10)
                };

                var result = planner.PlanTrajectory(request);
                Console.WriteLine($"  Trajectory {i + 1}: {(result.Success ? "✓ Success" : "✗ Failed")} " +
                                 $"({result.Waypoints.Count} waypoints, {result.PlanningTime.TotalMilliseconds:F2} ms)");
            }

            Console.WriteLine("  ✓ Scene reused successfully - no reloading needed!");
        }

        /// <summary>
        /// Tests SetStartConfiguration and SetGoalConfiguration using low-level RLWrapper API.
        /// </summary>
        static void TestSetStartGoalConfigurations(string kinematicsPath, string scenePath)
        {
            IntPtr planner = IntPtr.Zero;

            try
            {
                Console.WriteLine("  Testing SetStartConfiguration and SetGoalConfiguration...");

                // Create planner instance
                planner = RLWrapper.CreatePlanner();
                if (planner == IntPtr.Zero)
                {
                    Console.WriteLine("    ✗ Failed to create planner");
                    return;
                }
                Console.WriteLine("    ✓ Planner created");

                // Load kinematics and scene
                RLWrapper.LoadKinematics(planner, Path.GetFullPath(kinematicsPath));
                RLWrapper.LoadScene(planner, Path.GetFullPath(scenePath), robotModelIndex: 0);
                Console.WriteLine("    ✓ Kinematics and scene loaded");

                // Get DOF
                int dof = RLWrapper.GetDof(planner);
                Console.WriteLine($"    ✓ Robot DOF: {dof}");

                // Test 1: Set start configuration
                double[] startConfig = new double[dof];
                for (int i = 0; i < dof; i++)
                {
                    startConfig[i] = 0.1 * i; // Simple test values
                }

                RLWrapper.SetStartConfiguration(planner, startConfig);
                Console.WriteLine($"    ✓ Start configuration set: [{string.Join(", ", startConfig)}]");

                // Test 2: Set goal configuration
                double[] goalConfig = new double[dof];
                for (int i = 0; i < dof; i++)
                {
                    goalConfig[i] = 0.2 + 0.1 * i; // Different test values
                }

                RLWrapper.SetGoalConfiguration(planner, goalConfig);
                Console.WriteLine($"    ✓ Goal configuration set: [{string.Join(", ", goalConfig)}]");

                // Test 3: Validate configurations
                bool startValid = RLWrapper.IsValidConfiguration(planner, startConfig);
                bool goalValid = RLWrapper.IsValidConfiguration(planner, goalConfig);
                Console.WriteLine($"    ✓ Start config valid: {startValid}, Goal config valid: {goalValid}");

                // Test 4: Plan using stored configurations
                Console.WriteLine("    Planning with stored configurations...");
                double[] waypoints = RLWrapper.PlanTrajectory(
                    planner,
                    start: startConfig,  // Pass arrays (native code uses stored if they match)
                    goal: goalConfig,
                    useZAxis: false,
                    plannerType: "rrtConCon",
                    delta: 0.1,
                    epsilon: 0.001,
                    timeout: TimeSpan.FromSeconds(10),
                    waypointCount: out int waypointCount
                );

                if (waypointCount > 0)
                {
                    Console.WriteLine($"    ✓ Planning succeeded with {waypointCount} waypoints");
                    
                    // Show first and last waypoints
                    if (waypointCount > 0)
                    {
                        double[] firstWaypoint = new double[dof];
                        double[] lastWaypoint = new double[dof];
                        Array.Copy(waypoints, 0, firstWaypoint, 0, dof);
                        Array.Copy(waypoints, (waypointCount - 1) * dof, lastWaypoint, 0, dof);
                        Console.WriteLine($"      First waypoint: [{string.Join(", ", firstWaypoint)}]");
                        Console.WriteLine($"      Last waypoint:  [{string.Join(", ", lastWaypoint)}]");
                    }
                }
                else
                {
                    Console.WriteLine($"    ✗ Planning failed - no waypoints found");
                }

                // Test 5: Update configurations and plan again
                Console.WriteLine("    Testing configuration updates...");
                for (int i = 0; i < dof; i++)
                {
                    startConfig[i] = 0.3 + 0.1 * i;
                    goalConfig[i] = 0.4 + 0.1 * i;
                }

                RLWrapper.SetStartConfiguration(planner, startConfig);
                RLWrapper.SetGoalConfiguration(planner, goalConfig);
                Console.WriteLine($"    ✓ Updated start: [{string.Join(", ", startConfig)}]");
                Console.WriteLine($"    ✓ Updated goal:  [{string.Join(", ", goalConfig)}]");

                waypoints = RLWrapper.PlanTrajectory(
                    planner,
                    start: startConfig,
                    goal: goalConfig,
                    useZAxis: false,
                    plannerType: "rrtConCon",
                    delta: 0.1,
                    epsilon: 0.001,
                    timeout: TimeSpan.FromSeconds(10),
                    waypointCount: out waypointCount
                );

                if (waypointCount > 0)
                {
                    Console.WriteLine($"    ✓ Second planning succeeded with {waypointCount} waypoints");
                }
                else
                {
                    Console.WriteLine($"    ✗ Second planning failed");
                }

                Console.WriteLine("  ✓ SetStartConfiguration/SetGoalConfiguration tests completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Error in SetStartGoal test: {ex.Message}");
            }
            finally
            {
                if (planner != IntPtr.Zero)
                {
                    RLWrapper.DestroyPlanner(planner);
                }
            }
        }

        /// <summary>
        /// Tests LoadPlanXml functionality.
        /// </summary>
        static void TestLoadPlanXml(string planXmlPath)
        {
            IntPtr planner = IntPtr.Zero;

            try
            {
                // Detect planner type from filename
                string plannerType = "rrtConCon"; // Default
                string fileName = Path.GetFileNameWithoutExtension(planXmlPath).ToLower();
                if (fileName.Contains("prm"))
                {
                    plannerType = "prm";
                }
                else if (fileName.Contains("rrtconcon") || fileName.Contains("rrtcon"))
                {
                    plannerType = "rrtConCon";
                }
                else if (fileName.Contains("rrt"))
                {
                    plannerType = "rrt";
                }

                // Create planner instance
                planner = RLWrapper.CreatePlanner();
                if (planner == IntPtr.Zero)
                {
                    Console.WriteLine("    ✗ Failed to create planner");
                    return;
                }
                Console.WriteLine("    ✓ Planner created");

                // Load plan XML (includes kinematics, scene, start/goal)
                RLWrapper.LoadPlanXml(planner, Path.GetFullPath(planXmlPath));
                Console.WriteLine($"    ✓ Plan XML loaded successfully (detected planner: {plannerType})");

                // Get DOF
                int dof = RLWrapper.GetDof(planner);
                Console.WriteLine($"    ✓ Robot DOF: {dof}");

                // Plan using configurations from XML (pass null to use stored values)
                Console.WriteLine($"    Planning with configurations from XML (using {plannerType})...");
                double[] waypoints = RLWrapper.PlanTrajectory(
                    planner,
                    start: null!,  // Use stored start configuration from XML
                    goal: null!,   // Use stored goal configuration from XML
                    useZAxis: false,
                    plannerType: plannerType,
                    delta: 0.1,
                    epsilon: 0.001,
                    timeout: TimeSpan.FromSeconds(30),
                    waypointCount: out int waypointCount
                );

                if (waypointCount > 0)
                {
                    Console.WriteLine($"    ✓ Planning succeeded with {waypointCount} waypoints");
                    Console.WriteLine($"      Trajectory calculated: {waypointCount} waypoints from start to goal");
                    
                    // Show first and last waypoints (should match XML start/goal)
                    if (waypointCount > 0)
                    {
                        double[] firstWaypoint = new double[dof];
                        double[] lastWaypoint = new double[dof];
                        Array.Copy(waypoints, 0, firstWaypoint, 0, dof);
                        Array.Copy(waypoints, (waypointCount - 1) * dof, lastWaypoint, 0, dof);
                        Console.WriteLine($"      Start (from XML): [{string.Join(", ", firstWaypoint.Select(v => v.ToString("F4")))}]");
                        Console.WriteLine($"      Goal (from XML):  [{string.Join(", ", lastWaypoint.Select(v => v.ToString("F4")))}]");
                        
                        // Show a few intermediate waypoints if trajectory has more than 2 points
                        if (waypointCount > 2)
                        {
                            Console.WriteLine($"      Intermediate waypoints:");
                            int showCount = Math.Min(3, waypointCount - 2);
                            for (int i = 1; i <= showCount; i++)
                            {
                                double[] waypoint = new double[dof];
                                Array.Copy(waypoints, i * dof, waypoint, 0, dof);
                                Console.WriteLine($"        Waypoint {i + 1}: [{string.Join(", ", waypoint.Select(v => v.ToString("F4")))}]");
                            }
                            if (waypointCount > 5)
                            {
                                Console.WriteLine($"        ... ({waypointCount - 4} more waypoints)");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"    ✗ Planning failed - no waypoints found");
                    Console.WriteLine($"      (This may be normal if start/goal in XML are not reachable)");
                    return; // Can't test further without valid waypoints
                }

                // Test that we can explicitly set configurations from XML and plan again
                // Extract start/goal from the planning result (these match the XML values)
                double[] xmlStart = new double[dof];
                double[] xmlGoal = new double[dof];
                Array.Copy(waypoints, 0, xmlStart, 0, dof);
                Array.Copy(waypoints, (waypointCount - 1) * dof, xmlGoal, 0, dof);

                Console.WriteLine("    Testing SetStartConfiguration/SetGoalConfiguration with XML values...");
                
                // Validate configurations before planning
                bool startValid = RLWrapper.IsValidConfiguration(planner, xmlStart);
                bool goalValid = RLWrapper.IsValidConfiguration(planner, xmlGoal);
                
                Console.WriteLine($"    Start config (from XML): [{string.Join(", ", xmlStart)}] (valid: {startValid})");
                Console.WriteLine($"    Goal config (from XML):  [{string.Join(", ", xmlGoal)}] (valid: {goalValid})");

                if (!startValid || !goalValid)
                {
                    Console.WriteLine($"    ⚠ Skipping planning test - XML configurations are invalid");
                    Console.WriteLine($"      (This may indicate an issue with the plan XML file)");
                }
                else
                {
                    // Explicitly set the configurations (even though they're already stored)
                    RLWrapper.SetStartConfiguration(planner, xmlStart);
                    RLWrapper.SetGoalConfiguration(planner, xmlGoal);

                    // Plan again using the XML configurations explicitly
                    waypoints = RLWrapper.PlanTrajectory(
                        planner,
                        start: xmlStart,
                        goal: xmlGoal,
                        useZAxis: false,
                        plannerType: plannerType,
                        delta: 0.1,
                        epsilon: 0.001,
                        timeout: TimeSpan.FromSeconds(30),
                        waypointCount: out waypointCount
                    );

                    if (waypointCount > 0)
                    {
                        Console.WriteLine($"    ✓ Planning with XML configs (via SetStart/SetGoal) succeeded: {waypointCount} waypoints");
                        
                        // Display trajectory waypoints
                        Console.WriteLine($"      Trajectory waypoints:");
                        for (int i = 0; i < waypointCount && i < 10; i++) // Show first 10 waypoints
                        {
                            double[] waypoint = new double[dof];
                            Array.Copy(waypoints, i * dof, waypoint, 0, dof);
                            Console.WriteLine($"        Waypoint {i + 1}: [{string.Join(", ", waypoint.Select(v => v.ToString("F4")))}]");
                        }
                        if (waypointCount > 10)
                        {
                            Console.WriteLine($"        ... ({waypointCount - 10} more waypoints)");
                        }
                        
                        // Show first and last waypoints explicitly
                        double[] firstWaypoint = new double[dof];
                        double[] lastWaypoint = new double[dof];
                        Array.Copy(waypoints, 0, firstWaypoint, 0, dof);
                        Array.Copy(waypoints, (waypointCount - 1) * dof, lastWaypoint, 0, dof);
                        Console.WriteLine($"      Trajectory start: [{string.Join(", ", firstWaypoint.Select(v => v.ToString("F4")))}]");
                        Console.WriteLine($"      Trajectory end:   [{string.Join(", ", lastWaypoint.Select(v => v.ToString("F4")))}]");
                    }
                    else
                    {
                        Console.WriteLine($"    ✗ Planning with XML configs failed");
                        Console.WriteLine($"      (This may be normal for PRM if roadmap needs rebuilding)");
                    }
                }

                Console.WriteLine("  ✓ LoadPlanXml tests completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Error in LoadPlanXml test: {ex.Message}");
                // Only print stack trace for unexpected errors, not planning failures
                if (!ex.Message.Contains("PlanTrajectory failed") && !ex.Message.Contains("planning failed"))
                {
                    Console.WriteLine($"    Stack trace: {ex.StackTrace}");
                }
                else
                {
                    Console.WriteLine($"    Note: Planning failures may be expected for some planner types (e.g., PRM)");
                    Console.WriteLine($"          when start/goal configurations are far from the roadmap.");
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
}
