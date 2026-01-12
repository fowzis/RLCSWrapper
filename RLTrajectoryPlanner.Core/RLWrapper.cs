using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using RLTrajectoryPlanner.Core.Exceptions;

// Win32 API for setting DLL search directory
internal static class NativeMethods
{
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [SupportedOSPlatform("windows")]
    internal static extern bool SetDllDirectory(string? lpPathName);
    
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [SupportedOSPlatform("windows")]
    internal static extern bool AddDllDirectory(string lpPathName);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    [SupportedOSPlatform("windows")]
    internal static extern bool RemoveDllDirectory(IntPtr cookie);
    
    // Flags for SetDefaultDllDirectories
    private const uint LOAD_LIBRARY_SEARCH_APPLICATION_DIR = 0x00000200;
    private const uint LOAD_LIBRARY_SEARCH_USER_DIRS = 0x00000400;
    private const uint LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800;
    
    [DllImport("kernel32.dll", SetLastError = true)]
    [SupportedOSPlatform("windows")]
    internal static extern bool SetDefaultDllDirectories(uint DirectoryFlags);
}

namespace RLTrajectoryPlanner.Core
{
    /// <summary>
    /// P/Invoke wrapper for RL library native functions.
    /// Provides platform-specific library loading and C-compatible function declarations.
    /// </summary>
    internal static class RLWrapper
    {
        private const string LibraryName = "RLWrapper";
        private static bool _libraryLoaded = false;
        private static readonly object _loadLock = new object();

        // Error codes from C++ wrapper
        private const int RL_SUCCESS = 0;
        private const int RL_ERROR_INVALID_POINTER = -1;
        private const int RL_ERROR_INVALID_PARAMETER = -2;
        private const int RL_ERROR_LOAD_FAILED = -3;
        private const int RL_ERROR_PLANNING_FAILED = -4;
        private const int RL_ERROR_NOT_INITIALIZED = -5;
        private const int RL_ERROR_EXCEPTION = -6;

        /// <summary>
        /// Gets the platform-specific library name.
        /// </summary>
        private static string GetLibraryName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return $"{LibraryName}.dll";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return $"lib{LibraryName}.so";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return $"lib{LibraryName}.dylib";
            }
            else
            {
                throw new PlatformNotSupportedException($"Platform {RuntimeInformation.OSDescription} is not supported");
            }
        }

        /// <summary>
        /// Ensures the native library is loaded.
        /// 
        /// DLL Search Pattern:
        /// ===================
        /// Windows uses the following search order to locate DLLs and their dependencies:
        /// 
        /// 1. The directory containing the DLL being loaded (RLWrapper.dll)
        /// 2. Directories added via AddDllDirectory() API
        /// 3. System directories (System32, etc.)
        /// 4. Current directory
        /// 5. Directories in PATH environment variable
        /// 
        /// Requirements:
        /// =============
        /// For RLWrapper.dll to load successfully, all required DLLs must be accessible:
        /// - RLWrapper.dll itself
        /// - RL library DLLs (rlhal.dll, rlkin.dll, rlmdl.dll, rlsg.dll, rlplan.dll)
        /// - Third-party DLLs (Bullet, FCL, etc. from rl-3rdparty/install/bin)
        /// 
        /// Solution:
        /// =========
        /// All DLLs can be placed in a single folder. This code adds that folder (and the
        /// third-party DLL folder) to Windows' DLL search path, allowing Windows to automatically
        /// resolve all dependencies when RLWrapper.dll is loaded.
        /// 
        /// If all DLLs are in one folder, simply add that folder to the search path and load
        /// RLWrapper.dll. Windows will automatically load all transitive dependencies.
        /// </summary>
        internal static void EnsureLibraryLoaded()
        {
            if (_libraryLoaded)
            {
                return;
            }

            lock (_loadLock)
            {
                if (_libraryLoaded)
                {
                    return;
                }

                try
                {
                    string libraryName = GetLibraryName();
                    string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    
                    // Try multiple locations in order of preference
                    string[] searchPaths = new string[]
                    {
                        // 1. Current directory (for deployed applications)
                        System.IO.Path.Combine(baseDir, libraryName),
                        
                        // 2. RLlib platform folder relative to current directory
                        System.IO.Path.Combine(baseDir, "RLlib", 
                            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" :
                            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" : "macOS", 
                            libraryName),
                        
                        // 3. RLlib platform folder relative to project root (for development)
                        System.IO.Path.GetFullPath(System.IO.Path.Combine(
                            baseDir,
                            "..", "..", "..", "RLlib",
                            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" :
                            RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" : "macOS",
                            libraryName))
                    };
                    
                    bool loaded = false;
                    string? attemptedPath = null;
                    foreach (string libraryPath in searchPaths)
                    {
                        string normalizedPath = System.IO.Path.GetFullPath(libraryPath);
                        if (System.IO.File.Exists(normalizedPath))
                        {
                            attemptedPath = normalizedPath;
                            string? dllDirectory = System.IO.Path.GetDirectoryName(normalizedPath);
                            
                            // Configure DLL search path on Windows
                            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !string.IsNullOrEmpty(dllDirectory))
                            {
                                try
                                {
                                    // Set default DLL directories (Windows 8+)
                                    // This enables AddDllDirectory and ensures system directories are included
                                    uint defaultDirs = 0x00000200 | 0x00000400 | 0x00000800; // APPLICATION_DIR | USER_DIRS | SYSTEM32
                                    NativeMethods.SetDefaultDllDirectories(defaultDirs);
                                    
                                    // Add the directory containing RLWrapper.dll to the search path
                                    // This allows Windows to find RL DLLs (rlhal.dll, rlkin.dll, etc.)
                                    NativeMethods.AddDllDirectory(dllDirectory);
                                    
                                    // Add rl-3rdparty install/bin directory for third-party DLLs (Bullet, FCL, etc.)
                                    // Try multiple possible paths
                                    string[] possibleThirdPartyPaths = new string[]
                                    {
                                        System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, "..", "..", "..", "..", "rl-3rdparty", "install", "bin")),
                                        @"C:\Tools\RoboticLibrary\GitHub\rl-3rdparty\install\bin"
                                    };
                                    
                                    foreach (string thirdPartyPath in possibleThirdPartyPaths)
                                    {
                                        string normalizedThirdPartyPath = System.IO.Path.GetFullPath(thirdPartyPath);
                                        if (System.IO.Directory.Exists(normalizedThirdPartyPath))
                                        {
                                            NativeMethods.AddDllDirectory(normalizedThirdPartyPath);
                                            break; // Only add the first existing path
                                        }
                                    }
                                }
                                catch (Exception)
                                {
                                    // If AddDllDirectory fails (e.g., on older Windows), fall back to SetDllDirectory
                                    // Note: SetDllDirectory REPLACES the search path, so we also add to PATH
                                    string currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
                                    if (!currentPath.Contains(dllDirectory))
                                    {
                                        Environment.SetEnvironmentVariable("PATH", $"{dllDirectory};{currentPath}", EnvironmentVariableTarget.Process);
                                    }
                                    NativeMethods.SetDllDirectory(dllDirectory);
                                }
                            }
                            
                            // Load the DLL - Windows will automatically resolve all dependencies
                            // from the directories we added to the search path
                            NativeLibrary.Load(normalizedPath);
                            
                            loaded = true;
                            break;
                        }
                    }
                    
                    if (!loaded)
                    {
                        // Last resort: try loading from system path or current directory
                        NativeLibrary.Load(libraryName);
                    }
                    
                    _libraryLoaded = true;
                }
                catch (DllNotFoundException ex)
                {
                    throw new PlanningException(
                        $"Failed to load native library '{GetLibraryName()}'. " +
                        "Ensure the RLWrapper library and all dependencies (RL DLLs and third-party DLLs) " +
                        "are available in the RLlib folder or rl-3rdparty/install/bin directory.", ex);
                }
                catch (Exception ex)
                {
                    throw new PlanningException($"Failed to load native library: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Converts an error code to an exception.
        /// </summary>
        private static void ThrowOnError(int errorCode, string operation)
        {
            if (errorCode == RL_SUCCESS)
            {
                return;
            }

            string errorMessage = errorCode switch
            {
                RL_ERROR_INVALID_POINTER => "Invalid pointer parameter",
                RL_ERROR_INVALID_PARAMETER => "Invalid parameter",
                RL_ERROR_LOAD_FAILED => "Failed to load kinematics or scene",
                RL_ERROR_PLANNING_FAILED => "Trajectory planning failed",
                RL_ERROR_NOT_INITIALIZED => "Planner not initialized",
                RL_ERROR_EXCEPTION => "Exception occurred in native code",
                _ => $"Unknown error code: {errorCode}"
            };

            throw new PlanningException($"{operation} failed: {errorMessage}");
        }

        // Native function declarations

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreatePlanner")]
        private static extern IntPtr CreatePlannerNative();

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LoadKinematics", CharSet = CharSet.Ansi)]
        private static extern int LoadKinematicsNative(IntPtr planner, [MarshalAs(UnmanagedType.LPStr)] string xmlPath);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LoadScene", CharSet = CharSet.Ansi)]
        private static extern int LoadSceneNative(IntPtr planner, [MarshalAs(UnmanagedType.LPStr)] string xmlPath, int robotModelIndex);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "LoadPlanXml", CharSet = CharSet.Ansi)]
        private static extern int LoadPlanXmlNative(IntPtr planner, [MarshalAs(UnmanagedType.LPStr)] string xmlPath);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SetStartConfiguration")]
        private static extern int SetStartConfigurationNative(IntPtr planner, [MarshalAs(UnmanagedType.LPArray)] double[] config, int configSize);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SetGoalConfiguration")]
        private static extern int SetGoalConfigurationNative(IntPtr planner, [MarshalAs(UnmanagedType.LPArray)] double[] config, int configSize);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "PlanTrajectory", CharSet = CharSet.Ansi)]
        private static extern int PlanTrajectoryNative(
            IntPtr planner,
            [MarshalAs(UnmanagedType.LPArray)] double[] start, int startSize,
            [MarshalAs(UnmanagedType.LPArray)] double[] goal, int goalSize,
            int useZAxis, [MarshalAs(UnmanagedType.LPStr)] string plannerType,
            double delta, double epsilon, int timeoutMs,
            [MarshalAs(UnmanagedType.LPArray)] double[] waypoints, int maxWaypoints, out int waypointCount);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "IsValidConfiguration")]
        private static extern int IsValidConfigurationNative(IntPtr planner, [MarshalAs(UnmanagedType.LPArray)] double[] config, int configSize);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "GetDof")]
        private static extern int GetDofNative(IntPtr planner);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "DestroyPlanner")]
        private static extern void DestroyPlannerNative(IntPtr planner);

        // Managed wrapper methods

        /// <summary>
        /// Creates a new planner instance.
        /// </summary>
        internal static IntPtr CreatePlanner()
        {
            EnsureLibraryLoaded();
            IntPtr planner = CreatePlannerNative();
            if (planner == IntPtr.Zero)
            {
                throw new PlanningException("Failed to create planner instance");
            }
            return planner;
        }

        /// <summary>
        /// Loads kinematics from XML file.
        /// </summary>
        internal static void LoadKinematics(IntPtr planner, string xmlPath)
        {
            EnsureLibraryLoaded();
            int result = LoadKinematicsNative(planner, xmlPath);
            ThrowOnError(result, "LoadKinematics");
        }

        /// <summary>
        /// Loads scene from XML file.
        /// </summary>
        internal static void LoadScene(IntPtr planner, string xmlPath, int robotModelIndex)
        {
            EnsureLibraryLoaded();
            int result = LoadSceneNative(planner, xmlPath, robotModelIndex);
            ThrowOnError(result, "LoadScene");
        }

        /// <summary>
        /// Loads plan XML file that references kinematics and scene XMLs (like rlPlanDemo).
        /// Parses plan XML to extract scene path, kinematics path, robot model index, planner type, and parameters.
        /// Also extracts start/goal configurations if present in XML.
        /// </summary>
        internal static void LoadPlanXml(IntPtr planner, string xmlPath)
        {
            EnsureLibraryLoaded();
            int result = LoadPlanXmlNative(planner, xmlPath);
            ThrowOnError(result, "LoadPlanXml");
        }

        /// <summary>
        /// Sets start configuration - stored in planner instance for reuse.
        /// </summary>
        internal static void SetStartConfiguration(IntPtr planner, double[] config)
        {
            EnsureLibraryLoaded();
            int result = SetStartConfigurationNative(planner, config, config.Length);
            ThrowOnError(result, "SetStartConfiguration");
        }

        /// <summary>
        /// Sets goal configuration - stored in planner instance for reuse.
        /// </summary>
        internal static void SetGoalConfiguration(IntPtr planner, double[] config)
        {
            EnsureLibraryLoaded();
            int result = SetGoalConfigurationNative(planner, config, config.Length);
            ThrowOnError(result, "SetGoalConfiguration");
        }

        /// <summary>
        /// Plans a trajectory between start and goal configurations.
        /// </summary>
        internal static double[] PlanTrajectory(
            IntPtr planner,
            double[] start, double[] goal,
            bool useZAxis, string plannerType,
            double delta, double epsilon, TimeSpan timeout,
            out int waypointCount)
        {
            EnsureLibraryLoaded();

            // Determine DOF - use from arrays if provided, otherwise get from planner
            int dof;
            if (start != null && start.Length > 0)
            {
                dof = start.Length;
            }
            else if (goal != null && goal.Length > 0)
            {
                dof = goal.Length;
            }
            else
            {
                // Both are null - get DOF from planner
                dof = GetDof(planner);
                if (dof <= 0)
                {
                    throw new InvalidOperationException("Cannot determine DOF: arrays are null and GetDof failed");
                }
            }

            // Estimate maximum waypoints (conservative estimate)
            int maxWaypoints = 10000;
            double[] waypointsBuffer = new double[maxWaypoints * dof];

            int timeoutMs = (int)timeout.TotalMilliseconds;
            int result = PlanTrajectoryNative(
                planner, 
                start!, start?.Length ?? 0, 
                goal!, goal?.Length ?? 0,
                useZAxis ? 1 : 0, plannerType,
                delta, epsilon, timeoutMs,
                waypointsBuffer, maxWaypoints, out waypointCount);

            ThrowOnError(result, "PlanTrajectory");

            if (waypointCount <= 0)
            {
                return Array.Empty<double>();
            }

            double[] waypoints = new double[waypointCount * dof];
            Array.Copy(waypointsBuffer, waypoints, waypointCount * dof);
            return waypoints;
        }

        /// <summary>
        /// Checks if a configuration is valid (collision-free and within joint limits).
        /// </summary>
        internal static bool IsValidConfiguration(IntPtr planner, double[] config)
        {
            EnsureLibraryLoaded();
            int result = IsValidConfigurationNative(planner, config, config.Length);
            return result == 1;
        }

        /// <summary>
        /// Gets the degrees of freedom (number of joints).
        /// </summary>
        internal static int GetDof(IntPtr planner)
        {
            EnsureLibraryLoaded();
            int dof = GetDofNative(planner);
            if (dof < 0)
            {
                ThrowOnError(dof, "GetDof");
            }
            return dof;
        }

        /// <summary>
        /// Destroys a planner instance.
        /// </summary>
        internal static void DestroyPlanner(IntPtr planner)
        {
            if (planner != IntPtr.Zero)
            {
                EnsureLibraryLoaded();
                DestroyPlannerNative(planner);
            }
        }
    }
}
