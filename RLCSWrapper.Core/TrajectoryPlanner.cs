using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using RLCSWrapper.Core.Exceptions;
using RLCSWrapper.Core.Models;

namespace RLCSWrapper.Core
{
    /// <summary>
    /// Singleton trajectory planner service for collision-free path planning.
    /// Loads robot kinematics and scene once during initialization and reuses them for all planning requests.
    /// </summary>
    public sealed class TrajectoryPlanner : IDisposable
    {
        private static readonly Lazy<TrajectoryPlanner> _instance = 
            new Lazy<TrajectoryPlanner>(() => new TrajectoryPlanner(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        private IntPtr _plannerHandle;
        private bool _initialized;
        private bool _disposed;
        private readonly object _lockObject = new object();
        private int _dof = -1;

        /// <summary>
        /// Gets the singleton instance of TrajectoryPlanner.
        /// </summary>
        public static TrajectoryPlanner Instance => _instance.Value;

        /// <summary>
        /// Private constructor to enforce singleton pattern.
        /// </summary>
        private TrajectoryPlanner()
        {
            _plannerHandle = IntPtr.Zero;
            _initialized = false;
        }

        /// <summary>
        /// Gets a value indicating whether the planner has been initialized.
        /// </summary>
        public bool IsInitialized
        {
            get
            {
                lock (_lockObject)
                {
                    return _initialized && _plannerHandle != IntPtr.Zero;
                }
            }
        }

        /// <summary>
        /// Gets the degrees of freedom (number of joints) of the robot.
        /// Returns -1 if not initialized.
        /// </summary>
        public int Dof
        {
            get
            {
                lock (_lockObject)
                {
                    if (!_initialized || _plannerHandle == IntPtr.Zero)
                    {
                        return -1;
                    }
                    if (_dof < 0)
                    {
                        _dof = RLWrapper.GetDof(_plannerHandle);
                    }
                    return _dof;
                }
            }
        }

        /// <summary>
        /// Initializes the planner by loading kinematics and scene XML files.
        /// This method should be called once before planning trajectories.
        /// </summary>
        /// <param name="request">Initialization request containing paths to XML files.</param>
        /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
        /// <exception cref="PlanningException">Thrown when initialization fails.</exception>
        public void Initialize(InitializationRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            lock (_lockObject)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(TrajectoryPlanner));
                }

                if (_initialized)
                {
                    throw new PlanningException("Planner is already initialized. Dispose and create a new instance to reinitialize.");
                }

                if (string.IsNullOrWhiteSpace(request.KinematicsXmlPath))
                {
                    throw new ArgumentException("KinematicsXmlPath cannot be null or empty.", nameof(request));
                }

                if (string.IsNullOrWhiteSpace(request.SceneXmlPath))
                {
                    throw new ArgumentException("SceneXmlPath cannot be null or empty.", nameof(request));
                }

                if (!File.Exists(request.KinematicsXmlPath))
                {
                    throw new FileNotFoundException($"Kinematics XML file not found: {request.KinematicsXmlPath}");
                }

                if (!File.Exists(request.SceneXmlPath))
                {
                    throw new FileNotFoundException($"Scene XML file not found: {request.SceneXmlPath}");
                }

                try
                {
                    // Create planner instance
                    _plannerHandle = RLWrapper.CreatePlanner();

                    // Load kinematics
                    RLWrapper.LoadKinematics(_plannerHandle, Path.GetFullPath(request.KinematicsXmlPath));

                    // Load scene
                    RLWrapper.LoadScene(_plannerHandle, Path.GetFullPath(request.SceneXmlPath), request.RobotModelIndex);

                    // Get DOF
                    _dof = RLWrapper.GetDof(_plannerHandle);
                    if (_dof <= 0)
                    {
                        throw new PlanningException("Failed to get degrees of freedom from loaded kinematics.");
                    }

                    _initialized = true;
                }
                catch (PlanningException)
                {
                    // Re-throw planning exceptions
                    throw;
                }
                catch (Exception ex)
                {
                    // Clean up on failure
                    if (_plannerHandle != IntPtr.Zero)
                    {
                        RLWrapper.DestroyPlanner(_plannerHandle);
                        _plannerHandle = IntPtr.Zero;
                    }
                    throw new PlanningException($"Failed to initialize planner: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Plans a collision-free trajectory between start and goal configurations.
        /// </summary>
        /// <param name="request">Planning request with start/goal configurations and parameters.</param>
        /// <returns>Planning result containing waypoints or error information.</returns>
        /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
        /// <exception cref="PlanningException">Thrown when planning fails or planner is not initialized.</exception>
        public PlanningResult PlanTrajectory(PlanningRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            lock (_lockObject)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(TrajectoryPlanner));
                }

                if (!_initialized || _plannerHandle == IntPtr.Zero)
                {
                    throw new PlanningException("Planner is not initialized. Call Initialize() first.");
                }

                // Validate configurations
                if (request.StartConfiguration == null || request.StartConfiguration.Length == 0)
                {
                    throw new ArgumentException("StartConfiguration cannot be null or empty.", nameof(request));
                }

                if (request.GoalConfiguration == null || request.GoalConfiguration.Length == 0)
                {
                    throw new ArgumentException("GoalConfiguration cannot be null or empty.", nameof(request));
                }

                if (request.StartConfiguration.Length != request.GoalConfiguration.Length)
                {
                    throw new ArgumentException(
                        $"StartConfiguration length ({request.StartConfiguration.Length}) must match GoalConfiguration length ({request.GoalConfiguration.Length}).");
                }

                if (_dof > 0 && request.StartConfiguration.Length != _dof)
                {
                    throw new ArgumentException(
                        $"Configuration length ({request.StartConfiguration.Length}) must match robot DOF ({_dof}).");
                }

                var result = new PlanningResult();
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    // Convert planner type enum to string
                    string plannerTypeStr = request.Algorithm switch
                    {
                        PlannerType.RRT => "rrt",
                        PlannerType.RRTConnect => "rrtConCon",
                        PlannerType.RRTGoalBias => "rrtGoalBias",
                        PlannerType.PRM => "prm",
                        _ => "rrtConCon"
                    };

                    // Plan trajectory
                    int waypointCount;
                    double[] waypointsFlat = RLWrapper.PlanTrajectory(
                        _plannerHandle,
                        request.StartConfiguration,
                        request.GoalConfiguration,
                        request.UseZAxis,
                        plannerTypeStr,
                        request.Delta,
                        request.Epsilon,
                        request.Timeout,
                        out waypointCount);

                    stopwatch.Stop();
                    result.PlanningTime = stopwatch.Elapsed;

                    if (waypointCount > 0 && waypointsFlat.Length > 0)
                    {
                        // Convert flat array to list of waypoint arrays
                        int dof = request.StartConfiguration.Length;
                        result.Waypoints = new List<double[]>(waypointCount);

                        for (int i = 0; i < waypointCount; i++)
                        {
                            double[] waypoint = new double[dof];
                            Array.Copy(waypointsFlat, i * dof, waypoint, 0, dof);
                            result.Waypoints.Add(waypoint);
                        }

                        result.Success = true;
                    }
                    else
                    {
                        result.Success = false;
                        result.ErrorMessage = "Planning returned no waypoints.";
                    }
                }
                catch (PlanningException ex)
                {
                    stopwatch.Stop();
                    result.Success = false;
                    result.ErrorMessage = ex.Message;
                    result.PlanningTime = stopwatch.Elapsed;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    result.Success = false;
                    result.ErrorMessage = $"Unexpected error during planning: {ex.Message}";
                    result.PlanningTime = stopwatch.Elapsed;
                }

                return result;
            }
        }

        /// <summary>
        /// Checks if a configuration is valid (collision-free and within joint limits).
        /// </summary>
        /// <param name="config">Configuration to check.</param>
        /// <returns>True if configuration is valid, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
        /// <exception cref="PlanningException">Thrown when planner is not initialized.</exception>
        public bool IsValidConfiguration(double[] config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            lock (_lockObject)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(TrajectoryPlanner));
                }

                if (!_initialized || _plannerHandle == IntPtr.Zero)
                {
                    throw new PlanningException("Planner is not initialized. Call Initialize() first.");
                }

                if (_dof > 0 && config.Length != _dof)
                {
                    return false;
                }

                return RLWrapper.IsValidConfiguration(_plannerHandle, config);
            }
        }

        /// <summary>
        /// Releases all resources used by the TrajectoryPlanner.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the TrajectoryPlanner and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                lock (_lockObject)
                {
                    if (!_disposed)
                    {
                        if (_plannerHandle != IntPtr.Zero)
                        {
                            RLWrapper.DestroyPlanner(_plannerHandle);
                            _plannerHandle = IntPtr.Zero;
                        }

                        _initialized = false;
                        _dof = -1;
                        _disposed = true;
                    }
                }
            }
        }

        /// <summary>
        /// Finalizer.
        /// </summary>
        ~TrajectoryPlanner()
        {
            Dispose(false);
        }
    }
}

