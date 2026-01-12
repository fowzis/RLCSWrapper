namespace RLTrajectoryPlanner.Core.Models
{
    /// <summary>
    /// Enumeration of available trajectory planning algorithms.
    /// </summary>
    public enum PlannerType
    {
        /// <summary>
        /// Rapidly-exploring Random Tree - basic RRT algorithm
        /// </summary>
        RRT,

        /// <summary>
        /// RRT-Connect - Bidirectional RRT (default, recommended)
        /// </summary>
        RRTConnect,

        /// <summary>
        /// RRT with goal biasing - biases sampling toward goal
        /// </summary>
        RRTGoalBias,

        /// <summary>
        /// Probabilistic Roadmap Method - pre-computes roadmap
        /// </summary>
        PRM
    }
}

