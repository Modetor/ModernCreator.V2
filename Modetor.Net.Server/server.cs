using System;
using System.IO;
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
            {
                mServerReady = false;
                Logger.PrintError("[Server][Init] : something went wrong while reading settings.ini. not ready");
            }

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
            InternetProtocol = ip;
            Port = port;
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
                IsRunning = true;
                OnStart?.Invoke(InternetProtocol, Port);
                Thread thread = new Thread(() =>
                                                {
                                                    switch (Settings.ThreadMechanism)
                                                    {
                                                        case 0:
                                                            SingleThreadedServer();
                                                            break;
                                                        case 1:
                                                            Console.WriteLine("MTS way...");
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
            Console.WriteLine("Python running");
            if (Settings.ConnectionsHandler != null)
            {
                
                IronPythonObject connection_handler = IronPythonObject.GetObject(Settings.ConnectionsHandler);
                connection_handler.DefaultSearchPaths[0] = Directory.GetParent(Settings.ConnectionsHandler).FullName;
                connection_handler.Scope.TcpClient = client;
                connection_handler.Scope.ClientStream = client.GetStream();
                connection_handler.Scope.Write = new Action<string>((s) =>
                {
                    byte[] bs = System.Text.Encoding.UTF8.GetBytes(s);
                    client.GetStream().Write(bs, 0, bs.Length);
                });
                connection_handler.Scope.FWrite = new Action<string>((s) =>
                {
                    byte[] bs = System.Text.Encoding.UTF8.GetBytes(s);
                    client.GetStream().Write(bs, 0, bs.Length);
                    client.GetStream().FlushAsync();
                });
                string result = connection_handler.Run();
                if(result != null) // An error occurred...
                {
                    Logger.Log(result, System.Environment.StackTrace);
                    Logger.PrintError("[Server][ConnectionHandler] : "+result);
                }
            }
            else
            {
                Console.WriteLine("is null");

            }

            if (client.Connected)
                client.Close();
        }
        public bool IsRunning { get; private set; } = true;
        public string Address { get; private set; }
        public string InternetProtocol { get; private set; }
        public int Port { get; private set; }
        public readonly ErrorLogger Logger;

        public Action<string, int> OnStart;
        public Action<string, int> OnStop;
        public Action<string, int> OnReceiveConnection;
        public Action<string, int, object> OnSockerError;


        private bool mServerReady = true;
        private TcpListener mTcpListener;
        
    }
}
