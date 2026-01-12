namespace RLTrajectoryPlanner.Core.Models
{
    /// <summary>
    /// Request model for initializing the trajectory planner with robot kinematics and scene.
    /// </summary>
    public class InitializationRequest
    {
        /// <summary>
        /// Path to the robot kinematics XML file.
        /// </summary>
        public string KinematicsXmlPath { get; set; } = string.Empty;

        /// <summary>
        /// Path to the scene XML file containing obstacles and robot model.
        /// </summary>
        public string SceneXmlPath { get; set; } = string.Empty;

        /// <summary>
        /// Index of the robot model in the scene (default: 0).
        /// </summary>
        public int RobotModelIndex { get; set; } = 0;
    }
}

