using System;

namespace DicingBlade.Classes.Processes
{
    [Serializable]
    public class ProcessInterruptedException : Exception
    {
        public ProcessInterruptedException() { }
        public ProcessInterruptedException(string message) : base(message) { }
        public ProcessInterruptedException(string message, Exception inner) : base(message, inner) { }
        protected ProcessInterruptedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
