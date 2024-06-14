using System;

namespace MainFramework.GlobalUtils
{
    [Serializable]
    public class MissingCredentialsException : Exception
    {
        public MissingCredentialsException() : base() { }
        public MissingCredentialsException(string message) : base(message) { }
        public MissingCredentialsException(string message, Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client.
        protected MissingCredentialsException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
