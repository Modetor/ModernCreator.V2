namespace Modetor.Net.Server
{
    internal class InitializationException : System.Exception
    {
        public InitializationException() : base(){}
        public InitializationException(string msg) : base(msg) { }
    }

    internal class InvalidPropertyValue : System.Exception
    {
        public InvalidPropertyValue() : base() { }
        public InvalidPropertyValue(string msg) : base(msg) { }
    }
}
