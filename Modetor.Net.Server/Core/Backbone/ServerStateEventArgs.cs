using System;

namespace Modetor.Net.Server.Core.Backbone
{
    public class ServerStateEventArgs : EventArgs
    {
        public readonly HttpServers.BaseServer Server;
        public readonly string IP;
        public readonly int Port;
        internal ServerStateEventArgs(HttpServers.BaseServer server)
        {
            Server = server;
            IP = Server.IP;
            Port = Server.Port;
        }
    }
}
