namespace RLCSWrapper.Core.Models
{
    /// <summary>
    /// Request model for planning a trajectory between start and goal configurations.
    /// Note: Kinematics and Scene files are loaded during Initialize(), not per request.
    /// </summary>
    public class PlanningRequest
    {
        /// <summary>
        /// Start configuration (array of joint angles/positions).
        /// </summary>
        public double[] StartConfiguration { get; set; } = Array.Empty<double>();

        /// <summary>
        /// Goal configuration (array of joint angles/positions).
        /// </summary>
        public double[] GoalConfiguration { get; set; } = Array.Empty<double>();

        /// <summary>
        /// If false, fixes Z-axis at current height for 2D planning (SCARA robots).
        /// If true, allows Z-axis to vary for 3D planning.
        /// </summary>
        public bool UseZAxis { get; set; } = false;

        /// <summary>
        /// Planner algorithm to use (default: RRTConnect).
        /// </summary>
        public PlannerType Algorithm { get; set; } = PlannerType.RRTConnect;

        /// <summary>
        /// Step size for planner expansion (default: 0.1).
        /// </summary>
        public double Delta { get; set; } = 0.1;

        /// <summary>
        /// Goal tolerance - distance threshold for reaching goal (default: 0.001).
        /// </summary>
        public double Epsilon { get; set; } = 0.001;

        /// <summary>
        /// Maximum time allowed for planning (default: 30 seconds).
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}

