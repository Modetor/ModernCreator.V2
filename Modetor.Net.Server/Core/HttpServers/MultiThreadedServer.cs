using System.Net.Sockets;
using System.Threading;

namespace Modetor.Net.Server.Core.HttpServers
{
    public class MultiThreadedServer : BaseServer
    {
        public MultiThreadedServer(string ip, int port) : base(ip, port) { }

        protected internal override void OnRecieveRequest(TcpClient client)
        {
            new Thread(() =>
            {
                if (client.Connected)
                {
                    ProcessRequest(client);
                }
                else
                {
                    client.Close();
                }
            })
            { IsBackground = true, Priority = ThreadPriority.Lowest }.Start();
        }

        protected override void OnStarted()
        {
            //System.Diagnostics.Trace.WriteLine($"{nameof(MultiThreadedServer)}::Start()");
            base.OnStarted();
        }



    }



}
