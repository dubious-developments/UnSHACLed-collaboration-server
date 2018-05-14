using System;
using System.Runtime.Serialization;

namespace UnSHACLed.Collaboration
{
    /// <summary>
    /// The type of exception thrown by the content tracker for
    /// invalid assumptions that are exposed by the content tracker
    /// logic itself.
    /// </summary>
    [Serializable]
    public class ContentTrackerException : Exception
    {
        public ContentTrackerException()
        {
        }

        public ContentTrackerException(string message)
            : base(message)
        {
        }

        public ContentTrackerException(
            string message,
            Exception innerException)
            : base(message, innerException)
        {
        }

        protected ContentTrackerException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        {
        }
    }
}