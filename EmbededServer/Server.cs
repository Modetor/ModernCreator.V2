using System.Net.Sockets;

namespace EmbededServer
{
    public class Server
    {
        private static Server instance;
        /// <summary>
        ///     Server would use one of these ports 5722, 2257
        /// </summary>
        public static void GetServer()
        {
            if (instance == null)
                instance = new Server();

        }


        private Server()
        {
            listener = new TcpListener(System.Net.IPAddress.Any, 5722);
            //TestServer();
        }

        public void Start()
        {
            listening = true;
            listener.Start();
            Listen();
        }
        public void End()
        {
            listening = false;
            listener.Stop();
        }

        private void Listen()
        {
            looperThread = new Thread(Process)
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal,
                Name = "ServerLopperThread"
            };
            
            looperThread.Start();
        }

        private void Process()
        {
            while (listening)
            {
                try
                {
                    TcpClient tcpClient = listener.AcceptTcpClient();

                }
                catch(Exception exp)
                {
                    Console.WriteLine("Error, {0}\n{1}", exp.Message,exp.StackTrace);
                }

            }
        }


        private Dictionary<String, Component> Components;
        private Thread looperThread;
        private TcpListener listener;
        private bool listening;
    }
}