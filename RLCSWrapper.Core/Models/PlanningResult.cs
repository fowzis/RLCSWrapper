namespace RLCSWrapper.Core.Models
{
    /// <summary>
    /// Result model containing the planned trajectory waypoints and planning status.
    /// </summary>
    public class PlanningResult
    {
        /// <summary>
        /// Indicates whether planning was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// List of waypoints, where each waypoint is an array of joint angles/positions.
        /// </summary>
        public List<double[]> Waypoints { get; set; } = new List<double[]>();

        /// <summary>
        /// Error message if planning failed, empty if successful.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Time taken for planning.
        /// </summary>
        public TimeSpan PlanningTime { get; set; }
    }
}

