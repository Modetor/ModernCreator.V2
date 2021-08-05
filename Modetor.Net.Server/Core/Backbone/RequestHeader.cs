using System.Net.Sockets;

namespace Modetor.Net.Server.Core.Backbone
{
    //public class ClientRequestEvenArgs : EventArgs
    //{

    //    public string host;
    //    public ClientRequestEvenArgs(string x)
    //    {

    //    }
    //}



    public abstract class RequestHeader
    {
        /*public static async System.Threading.Tasks.Task<RequestHeader> GetRequestHeader(TcpClient client, HttpServers.BaseServer server)
        {
            MemoryStream memory = new MemoryStream();
            NetworkStream stream = client.GetStream();
            if (stream.CanRead)
            {
                long maxBytes = (int)server.Settings.Current.MaxHttpRequestSize * 1024 * 1024;
                long length = 0;
                int readingCounts = 5;
                byte[] b = new byte[1024];
                do
                {
                    if (stream.DataAvailable)
                    {
                        if (length > maxBytes) break;


                        int read = await stream.ReadAsync(b, 0, b.Length);
                        if (read <= 0) break;
                        length += read;

                        await memory.WriteAsync(b, 0, read);
                    }
                    else
                    {
                        if (readingCounts-- <= 0) break;
                        System.Threading.Thread.Sleep(1);
                    }

                }
                while (true);
            }
            else
                return null;

            memory.Seek(0, SeekOrigin.Begin);

            byte[] mem = memory.ToArray();

            string test = Encoding.ASCII.GetString(mem[..10]);
            if (test.Contains("HTTP/"))
            {
                HttpRequestHeader requestHeader = new HttpRequestHeader(client, server);
                requestHeader.ProcessRequestHeader(mem);
                return requestHeader;
            }
            else
                return null;

        }*/
        public RequestHeader(TcpClient client, HttpServers.BaseServer server)
        {
            Client = client;
            System.Net.IPEndPoint remoteEndPoint = ((System.Net.IPEndPoint)Client.Client.RemoteEndPoint);
            ClientAddress = remoteEndPoint.ToString();
            ClientIP = remoteEndPoint.Address.ToString();
            ClientPort = remoteEndPoint.Port;
            Server = server;

        }
        public readonly HttpServers.BaseServer Server;
        public readonly string ClientAddress;
        public readonly string ClientIP;
        public readonly int ClientPort;
        internal readonly TcpClient Client;
        public bool State { get; protected set; }
        public bool KeepAlive { get; protected set; }
    }
}
