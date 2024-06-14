using System;

namespace MainFramework.GlobalUtils
{
    [Serializable]
    public class SfLoginException : Exception
    {
        public SfLoginException() : base() { }
        public SfLoginException(string message) : base(message) { }
        public SfLoginException(string message, Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client.
        protected SfLoginException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
