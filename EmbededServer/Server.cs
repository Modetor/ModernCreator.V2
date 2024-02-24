using EmbededServerInterface;
using System.Net.Sockets;
using System.Buffers;

namespace EmbededServer
{
    public class Server
    {
        private static Server? instance;
        /// <summary>
        ///     Server would use one of these ports 5722, 2257
        /// </summary>
        public static Server GetServer(Configuration config)
        {
            
            
            if (instance == null)
                instance = new Server(config);

            if (instance.Components.Count == 0)
                ComponentsLoader.Load(ref instance);
            return instance;
        }


        private Server(Configuration configuration)
        {
            listener = new TcpListener(System.Net.IPAddress.Any, 5722);
            Configuration = configuration;
            Components = new Dictionary<string, Component>();
            looperThread = new Thread(Process)
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal,
                Name = "ServerLopperThread"
            };
        }

        public void Start()
        {
            listener?.Start();
            Listen();
            listening = true;
        }
        public void End()
        {
            listening = false;
            listener?.Stop();
        }

        private void Listen()
        {
            if (!listening)
                 looperThread.Start();
        }

        private void Process()
        {
            while (listening)
            {
                try
                {
                    TcpClient? tcpClient = listener?.AcceptTcpClient();
                    if(tcpClient == null)
                        continue;
                    new Thread(() =>
                    {
                        Client client = new Handshake.ClientBuilder(Configuration,tcpClient).Build();
                        if (client.Equals(Client.InvalidClient()))
                        {
                            tcpClient.Close();
                            return;
                        }

                        Session session = new Session(client, tcpClient);
                        CancellationTokenSource cancellationTokenSource = new();                        
                        if (!Components.ContainsKey(client.Rout))
                        {
                            Console.Error.WriteLine("Rout not found");
                            //tcpClient.Close();
                            CommunicationChannel component = new(session);
                            component.Start();
                        } 
                         else
                        {
                            Component component = Components[client.Rout];
                            if (Activator.CreateInstance(component.Type, new { session }) is not CommunicationChannel channel)
                                tcpClient.Close();
                            else
                            {
                                channel.Start();
                            }

                        }
                    })
                    {
                        IsBackground = true,
                        Priority = ThreadPriority.Normal
                    }.Start();
                    
                    
                    new Thread(() =>
                    {
                        /*byte[] rout = new byte[512];
                        tcpClient.ReceiveTimeout = 30000;
                        // try to read rout
                        int read = 0, chances = 5;
                        while(chances > 0)
                        {
                            read = tcpClient.GetStream().Read(rout, 0, rout.Length);
                            if (read == 0)
                                Thread.SpinWait(--chances);
                            else
                                break;
                        }

                        string stringRout = System.Text.Encoding.UTF8.GetString(rout);
                        // search in defined routs
                        if(!Components.ContainsKey(stringRout))
                            tcpClient.Close();
                        else
                        {
                            Component component = Components[stringRout];
                            EmbededServerInterface.CommuniationChannel? channel = 
                                    Activator.CreateInstance(component.Type, new { tcpClient}) as EmbededServerInterface.CommuniationChannel;


                        }*/
                        


                    })
                    { }.Start();

                }
                catch(Exception exp)
                {
                    Console.WriteLine("Error, {0}\n{1}", exp.Message,exp.StackTrace);
                }

            }
        }


        internal void AddComponent(string rout, Component component)
        {
            _ = Components.TryAdd(rout, component);
        }
        public Configuration Configuration { get; private set; }

        private readonly Dictionary<string, Component> Components;
        private Thread looperThread;
        private readonly TcpListener? listener;
        private bool listening;
        
        private CancellationTokenSource CancellationTokenSource;
    }
}