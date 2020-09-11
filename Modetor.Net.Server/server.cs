using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using IronPython.Runtime;
namespace Modetor.Net.Server
{
    public class Server : IDisposable
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
            mTcpListener.Stop();
            IsRunning = false;
        }

        private void ActivePipsServer()
        {
            throw new NotImplementedException();
        }

        private void MultiThreadedServer()
        {
            try
            {

                while (IsRunning)
                {
                    TcpClient client = mTcpListener.AcceptTcpClient();
                    new Thread(() => ManageClient(client)) { IsBackground = true }.Start();
                }
            }
            catch (Exception exp)
            {
                OnListenerError ?. Invoke(exp);
            }
        }

        

        private void SingleThreadedServer()
        {
            throw new NotImplementedException();
        }

        
        private void ManageClient(TcpClient client)
        {
            string content = null;
            byte[] b_data = new byte[client.Available];
            if (client.GetStream().DataAvailable) { client.GetStream().Read(b_data, 0, b_data.Length); content = System.Text.Encoding.UTF8.GetString(b_data); }
            else content = "failed to read header";
             
            HeaderKeys hk = HeaderKeys.From(content);
            Console.WriteLine("HK-Target= {0}, Address:{1}",hk.GetValue("target"), client.Client.RemoteEndPoint.ToString());
            string actual_file = null;
            PathResolver.Resolve(hk.GetValue("target"), hk.GetValue("referrer") ?? string.Empty, out actual_file);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(HeaderKeys.GenerateJSON(hk.GetCollection()));
            Console.ResetColor();
            /*if (Settings.ConnectionsHandler != null)
            {
                IronPythonObject connection_handler = IronPythonObject.GetObject(Settings.ConnectionsHandler);
                connection_handler.DefaultSearchPaths[0] = Directory.GetParent(Settings.ConnectionsHandler).FullName;
                IronPythonObject.SetupScope(connection_handler.Scope, client, hk);
                connection_handler.Scope.RequestedFile = actual_file;
                string result = connection_handler.Run();
                if(result != null) // An error occurred...
                {
                    Logger.Log(result, Environment.StackTrace);
                    Logger.PrintError("[Server][ConnectionHandler] : "+result);
                }
            }
            else
            {
                Console.WriteLine("is null");
            }
            */
            client.GetStream().Write(System.Text.Encoding.UTF8.GetBytes("<b>" + content + "</b>"));
            client.GetStream().Flush();
            if (client.Connected)
                client.Close();
        }

        public void Dispose()
        {
            ((IDisposable)instance).Dispose();
        }

        public bool IsRunning { get; private set; } = true;
        public string Address { get; private set; }
        public string InternetProtocol { get; private set; }
        public int Port { get; private set; }
        public readonly ErrorLogger Logger;

        public Action<string, int> OnStart;
        public Action<string, int> OnStop;
        public Action<string, int> OnReceiveConnection;
        public Action<string, int, object> OnSocketError;
        public Action<Exception> OnListenerError;


        private bool mServerReady = true;
        private TcpListener mTcpListener;
        
    }
}
