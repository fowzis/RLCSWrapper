namespace RLTrajectoryPlanner.Core.Exceptions
{
    /// <summary>
    /// Exception thrown when trajectory planning operations fail.
    /// </summary>
    public class PlanningException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the PlanningException class.
        /// </summary>
        public PlanningException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the PlanningException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public PlanningException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the PlanningException class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public PlanningException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

