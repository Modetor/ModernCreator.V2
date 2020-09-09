using System;
using System.Net.Sockets;
using System.Threading;
using IronPython.Runtime;
namespace Modetor.Net.Server
{
    public class Server
    {
        internal static Server instance = null; 
        public static Server GetServer()
        {
            if (null == instance)
                instance = new Server();
            return instance;
        }
        private Server()
        {
            Logger = new ErrorLogger();
            if (!Settings.Read())
                mServerReady = false;

        }
        public void Shutdown()
        {
            IsRunning = false;
        }
        public void SetAddress(string ip, int port)
        {
            if (!mServerReady) return;
            if (ip == null || port < 0) throw new ArgumentNullException("address is null");
            if (IsRunning)
                Shutdown();
            Address = ip+":"+port;
            try { mTcpListener = new TcpListener(System.Net.IPAddress.Parse(ip), port); }
            catch(Exception exp) { Logger.Log(exp); Logger.PrintError("[Server] : Failed to initialize the server with the specified ip and/or port"); }
        } 

        public void Start()
        {
            if (!mServerReady) return;
            if (mTcpListener == null)
            {Logger.PrintError("[Server] : Call SetAddress before Start()"); return;}

            try
            {
                mTcpListener.Start();
                Thread thread = new Thread(() =>
                                                {
                                                    switch (Settings.ThreadMechanism)
                                                    {
                                                        case 0:
                                                            SingleThreadedServer();
                                                            break;
                                                        case 1:
                                                            MultiThreadedServer();
                                                            break;
                                                        case 2:
                                                            ActivePipsServer();
                                                            break;
                                                        default:
                                                            ActivePipsServer();
                                                            break;
                                                    }
                                                })
                {
                    Name = "TcpListener",
                    IsBackground = true,
                    Priority = ThreadPriority.Normal
                };
                thread.Start();
            }
            catch (Exception exp)
            {
                Logger.Log(exp); Logger.PrintError("[Server] : Failed to start the server, Message : "+exp.Message);
            }

        }

        public void Stop()
        {
            IsRunning = false;
        }

        private void ActivePipsServer()
        {
            throw new NotImplementedException();
        }

        private void MultiThreadedServer()
        {
            while (IsRunning)
            {
                TcpClient client = mTcpListener.AcceptTcpClient();
                new Thread(() => ManageClient(client)) { IsBackground = true }.Start();
            }
        }

        

        private void SingleThreadedServer()
        {
            throw new NotImplementedException();
        }

        
        private void ManageClient(TcpClient client)
        {
            if(Settings.ConnectionsHandler != null)
            {

            }
        }
        public bool IsRunning { get; private set; }
        public string Address { get; private set; }
        public string InternetProtocol { get; private set; }
        public int Port { get; private set; }

        private bool mServerReady = false;
        private TcpListener mTcpListener;
        public ErrorLogger Logger;
    }
}
