using System;
using System.Runtime.Serialization;

namespace MasterCrack
{
    [Serializable]
    internal class ApplicationExitException : Exception
    {
        public ApplicationExitException()
        {
        }

        public ApplicationExitException(string message) : base(message)
        {
        }

        public ApplicationExitException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ApplicationExitException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}