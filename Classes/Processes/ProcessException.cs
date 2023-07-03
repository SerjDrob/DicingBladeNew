using System;
using System.Runtime.Serialization;
using DicingBlade;
using DicingBlade.Classes;
using DicingBlade.Classes.Processes;

namespace DicingBlade.Classes.Processes
{
    class ProcessException : Exception
    {
        public ProcessException()
        {
        }

        public ProcessException(string message) : base(message)
        {
        }

        public ProcessException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ProcessException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
