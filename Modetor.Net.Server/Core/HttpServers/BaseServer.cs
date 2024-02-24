/*\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
III
III          بسم الله الرحمن الرحيم
III 
\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\*/


#define XY

using Modetor.Net.Server.Core.Backbone;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Modetor.Net.Server.Core.HttpServers
{
    public abstract partial class BaseServer
    {
        public static Dictionary<string, BaseServer> Servers { get; private set; } = new();
        public static bool IsValidIP(string ip)
        {
            return IPAddress.TryParse(ip, out _);
        }
        public static BaseServer InitializeServer(string ip, int port)
        {
            ErrorLogger.Initialize();

            string address = $"{ip}:{port}";
            if (Servers.ContainsKey(address))
                return Servers[address];
            else
            {
                Settings settings = new();
                if (settings.IsReady)
                {
                    BaseServer x;
                    if (settings.Current.ThreadMechanism == 1)
                        x = new MultiThreadedServer(ip, port);
                    else if (settings.Current.ThreadMechanism == 2)
                        x = new ActivePipsServer(ip, port);
                    else
                        x = null;

                    if (x == null || x.Listener == null)
                        return null;

                    x.SetSettings(settings);
                    Servers.Add(address, x);
#pragma warning disable CA1416 // Validate platform compatibility
                    x.Listener.Server.SetIPProtectionLevel(settings.Current.IPProtectionLevel == -1 ? IPProtectionLevel.Unspecified : 
                        settings.Current.IPProtectionLevel == 10 ? IPProtectionLevel.Unrestricted : 
                        settings.Current.IPProtectionLevel == 20 ? IPProtectionLevel.EdgeRestricted : IPProtectionLevel.Restricted);
#pragma warning restore CA1416 // Validate platform compatibility
                    return x;
                }
                else
                {
                    return null;
                }
            }
        }

        private void SetSettings(Settings settings)
        {
            if(settings != null)
            {
                Settings = settings;
            }

            if (Settings != null)
            {
                string serverName = $"{IP}:{Port}";

                for (int i = 0; i < Settings.RepositoriesRules.Count; i++)
                {
                    string exclusiveForServer = Settings.RepositoriesRules[Settings.Repositories[i]].ExclusiveForServer;
                    if (exclusiveForServer.Contains("*"))
                    {

                        if (exclusiveForServer.Equals("*:*"))
                        {
                            continue;
                        }
                        else if (exclusiveForServer.EndsWith(":*"))
                        {
                            if (!IP.Equals(exclusiveForServer[..exclusiveForServer.IndexOf(':')]))
                            {
                                Settings.RepositoriesRules.Remove(Settings.Repositories[i]);
                            }
                        }
                        else if (exclusiveForServer.StartsWith("*:"))
                        {
                            Range startIndex = (exclusiveForServer.IndexOf(':')+1)..;
                            exclusiveForServer = exclusiveForServer[startIndex];
                            if (!exclusiveForServer.Equals(Port.ToString()))
                            {
                                Settings.RepositoriesRules.Remove(Settings.Repositories[i]);
                            }
                        }
                    }
                    else if (!exclusiveForServer.Equals(string.Empty) &&
                        !exclusiveForServer.Equals(serverName))
                    {
                        Settings.RepositoriesRules.Remove(Settings.Repositories[i]);
                    }
                }
            }
            
        }

        internal protected BaseServer(string ip, int port)
        {
            IP = ip;
            Port = port;
            if (!SetupManualy(ip, port))
                return;
            RequestsCount = 0;
            Signal = new ManualResetEventSlim();
            BroadcastReceiverControlSignal = ControlSignal.OFF;
            RegisteredClients = new Dictionary<string, Device>();
            BlockedClients = new List<string>();
            ClientDictionary = new Dictionary<string, List<TcpClient>>();
            BroadcastReceiver = new Broadcast(this);
            ServerTimer = new();
        }

        public void ClearRegisteredClients() => RegisteredClients.Clear();
        public void Start() {
            try
            {
                if (Listener == null)
                    throw new InitializationException("Call Constructor before Start()");

                // متولعش السيارة مرتين :)
                if (Active)
                    return;
                // تمام .. ولع :)
                Listener.Start();

                // REPOSITORIES STARTUP SECTION GOES HERE :::
                // LOADING DLL FILES AS WELL
                foreach(string name in Settings.Repositories)
                {
                    Rule rule = Settings.RepositoriesRules[name];
                    if(rule.HasStartupFile)
                        PythonRunner.RunStartups(this, rule);

                    if(rule.HasConnectionHandler && rule.ConnectionHandler.EndsWith(".dll"))
                        rule.LoadConnectionHandler(this);

                }

                // SETUP SERVER'S TIMER
                if ((bool)Settings.Current.AllowServerIntervalLoop)
                {
                    BroadcastReceiver.Register(Broadcast.PUBLIC_TIME_CHANNEL);
                    
                    ServerTimer.Elapsed += (s, e) =>
                    {
                        if (BroadcastReceiverControlSignal.Equals(ControlSignal.OFF) ||
                            BroadcastReceiver.GetActionsCount(Broadcast.PUBLIC_TIME_CHANNEL) <= 0) return;

                        BroadcastReceiver.Receive(Broadcast.PUBLIC_TIME_CHANNEL, null, false);
                    };

                    ServerTimer.Enabled = true;
                    ServerTimer.AutoReset = true;
                    ServerTimer.Interval = (int)Settings.Current.ServerIntervalLoop;


                }

                if ((bool)Settings.Current.AllowSmartSwitch)
                {
                    bool ignore = true;
                    System.Net.NetworkInformation.NetworkChange.NetworkAddressChanged += (s, e) =>
                    {
                        if (!Active || IsSuspended)
                            return;

                        if (ignore) { ignore = !ignore; return; }

                        string newip = !(bool)Settings.Current.AllowNetworkConnections ? "127.0.0.1" : GetNeworkIPs()?[0];
                        if (string.IsNullOrEmpty(newip))
                            return;

                        if (Servers.ContainsKey($"{newip}:{Port}"))
                            return;

                        if (IP.Equals(newip))
                            return;

                        Shutdown();
                        
                        IP = newip;
                        if(!Servers.ContainsKey($"{IP}:{Port}"))
                        {
                            Servers.Add($"{IP}:{Port}", this);
                        }

                        SetupManualy(newip, Port);
                        OnSmartSwitchEnd();
                        Start();
                    };
                }


                Active = true;
                IsSuspended = false;

                Signal?.Set();
                ServerThread = new Thread(() =>
                {
                    A:
                    try
                    {
                        while (Active)
                        {
                            if (!Signal.IsSet)
                                
                            if (IsSuspended)
                            {
                                Signal.Reset();
                                Signal.Wait();
                            }


                            TcpClient client = Listener.AcceptTcpClient();
                            if(IsSuspended)
                                client.Close();
                            else
                            {
                                SetupServerProcedure(client);
                                RequestsCount++;
                            }

                        }
                    }
                    catch (Exception exp)
                    {
                        ErrorLogger.WithTrace(Settings, string.Format("[SLL][Server error => ServerThread~] : exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), GetType());
                    }

                    if(Active)
                        goto A;
                    
                })
                { Name = "SERVER THREAD", Priority = ThreadPriority.AboveNormal, IsBackground = true };
                // START SERVER..
                ServerThread.Start();
                // AND SERVER'S TIMER..
                ServerTimer.Start();

                OnStarted();
            }
            catch (Exception exp)
            {
                StartupError();
                ErrorLogger.WithTrace(Settings, string.Format("[Warning][Server error => Start()] : exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), GetType());
            }
        }
        public bool SetupManualy(string ip, int port)
        {
            try {
                Listener = new TcpListener(IPAddress.Parse(ip), port);
                Listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                return true;
            }
            catch(Exception exp) {
                Listener = null;
                ErrorLogger.WithTrace(string.Format("[FATEL][Server error => SetupManualy] : exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), GetType());
                return false;
            }
        }
        public void StartupError()
        {
            OnStartupError();
            Shutdown();
        }
        public void Suspend()
        {
            //Listener.Stop();
            IsSuspended = true;
            ServerTimer.Enabled = false;
            OnSuspended();
        }
        public void Resume()
        {
            IsSuspended = false;
            Signal.Set();
            ServerTimer.Enabled = true;
            OnResumed();
        }
        public void Shutdown() {
            OnShutteddown();
            Active = false;
            IsSuspended = false;
            //ServerTimer.Enabled = false;
            ServerTimer?.Stop();
            BroadcastReceiverControlSignal = ControlSignal.OFF;
            Signal?.Set();
            Signal?.Dispose();
            Listener?.Stop();

            IsShutdown = true;

            Servers.Remove($"{IP}:{Port}");
        }


        protected internal void SetupServerProcedure(TcpClient client)
        {
            try
            {
                //if (IsSuspended) return;

                

                IPEndPoint endPoint = (IPEndPoint)client.Client.RemoteEndPoint;
                
                if((bool)Settings.Current.AllowSocketControlFlow && Settings.SocketControlFlow.Count > 0)
                {
                    bool processed = false;
                    foreach(SocketControlFlow controlFlow in Settings.SocketControlFlow)
                    {
                        if (controlFlow.IsMatch(endPoint.Address.ToString(), endPoint.Port))
                        {
                            System.Threading.Tasks.Task.Run(() => {
                                PythonRunner.SpecialRun(new Backbone.HttpRequestHeader(client, this, false), new HttpRespondHeader(), controlFlow.Target);
                            }).ContinueWith(t => {
                                if (client.Connected)
                                    client.Close();
                            }).ConfigureAwait(false);
                            processed = true;
                            return;
                        }
                    }

                    /// socket مفيش داعي ينزل بال 
                    if (processed)
                    {
                        return;
                    }
                }
                ///
                ///   فلتر مستوى 0
                ///

                if (IsBlockedClient(endPoint.Address.ToString()) && (bool)Settings.Current.IgnoreBlockedClients)
                {
                    ErrorLogger.WithTrace(Settings, $"[Fatel][Server's Settings violation warning] : Cannot accept connection(s) from blocked clients({endPoint})\n", GetType());
                    client.Close();
                }
                else if(!(bool)Settings.Current.AllowNetworkConnections && !IP.Equals(endPoint.Address.ToString())) {
                    ErrorLogger.WithTrace(Settings, $"[Fatel][Server's Settings violation warning] : Cannot accept network connection(s). clients({endPoint})\n", GetType());
                    client.Close();
                }
                else
                {
                    OnRecieveRequest(client);
                }
            }
            catch (Exception exp)
            {
                ErrorLogger.WithTrace(Settings,string.Format("[Warning][Server error => SetupServerProcedure()] : exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), GetType());
            }

        }
        internal protected virtual void OnRecieveRequest(TcpClient client) => client.Close();
        internal protected virtual void OnBroadcast(Action<object> action, object obj)
        {
            new Thread(() =>
            {
                try { action?.Invoke(obj); }
                catch(Exception exp) {
                    ErrorLogger.WithTrace(string.Format("[Error][Server error => OnBroadcast] : exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), GetType());
                }
            }) { Priority = ThreadPriority.Lowest, IsBackground = true }.Start();
        }
        internal protected async System.Threading.Tasks.Task ProcessRequest(TcpClient client)
        {
            try
            {
                Backbone.HttpRequestHeader req = null;
                NetworkStream stream = client.GetStream();
                req = new Backbone.HttpRequestHeader(client, this, true);
                
                

                client.ReceiveTimeout = Settings.ReceiveTimeout;
                client.SendTimeout = Settings.SendTimeout;

                stream.ReadTimeout = Settings.ReceiveTimeout;
                stream.WriteTimeout = Settings.SendTimeout;
                if ((bool)Settings.Current.FilterConnections)
                {
                    if ((int)Settings.Current.MaxClientsCount > 0)
                    {
                        string clientAddress = client.Client.RemoteEndPoint.ToString().Split(':')[0];
                        // just refuse to connect :-)
                        if (RegisteredClients.Count >= (int)Settings.Current.MaxClientsCount && !RegisteredClients.ContainsKey(clientAddress))
                        {
                            ErrorLogger.WithTrace(Settings, string.Format("[Error][Server's Settings violation warning] Connections(Clients) exceeded the limit {0}", (int)Settings.Current.MaxClientsCount), GetType());
                            
                            // read a request with no respond! 
                            req.ProcessRequestHeader(client, 1);
                            if (Port == 80 || Port  <= 1000)
                            {
                                
                                HttpRespondHeader resh = new();
                                resh.SetState(req.HttpVersion, HttpRespondState.MAX_CLIENTS_LIMIT);
                                resh.AddHeader("Content-Type", "text/plain");
                                //resh.AddHeader("Content-Length", );
                                resh.SetBody("Error 401 Max Clients Limit");
                                await stream.WriteAsync(resh.Build());
                            }
                            else
                            {

                                byte[] intBytes = BitConverter.GetBytes(0x0000c2);
                                if (BitConverter.IsLittleEndian)
                                    Array.Reverse(intBytes);
                                await stream.WriteAsync(intBytes);
                            }
                            
                            await stream.DisposeAsync();
                            client.Close();
                            return;
                        }

                        req.ProcessRequestHeader(client);
                        if (string.IsNullOrEmpty(req.RequestedTarget))
                        {
                            ErrorLogger.WithTrace(Settings, "[Warning][Invalid Connection] : this connection has no data", GetType());
                            await stream.DisposeAsync();
                            client.Close();
                            return;
                        }
                        else
                        {
                            
                            lock (@lock)
                            {
                                if (!RegisteredClients.ContainsKey(clientAddress))
                                {
                                    RegisteredClients.Add(clientAddress, Device.GetDeviceByUserAgent(req.HeaderKeys.ContainsKey("User-Agent") ? req.HeaderKeys["User-Agent"] : null));
                                }
                            }
                        }
                    }

                    if (!"Any".Equals((string)Settings.Current.AcceptConnectionsFrom))
                    {
                        req.ProcessRequestHeader(client);
                        if (string.IsNullOrEmpty(req.RequestedTarget))
                        {
                            ErrorLogger.WithTrace(Settings, "[Warning][Invalid Connection] : this connection has no data", GetType());
                            await stream.DisposeAsync();
                            client.Close();
                            return;
                        }
                        DeviceType clientDeviceType = Device.GetDeviceByUserAgent(req.HeaderKeys.ContainsKey("User-Agent") ? req.HeaderKeys["User-Agent"] : null).Type;

                        DeviceType deviceType = Device.GetDevicType((string)Settings.Current.AcceptConnectionsFrom);
                        if ("Both".Equals((string)Settings.Current.AcceptConnectionsFrom) && (clientDeviceType == DeviceType.DESKTOP || clientDeviceType == DeviceType.MOBILE))
                        {/*  تركت فارغة عمدا  */}
                        else if (clientDeviceType != deviceType)
                        {
                            ErrorLogger.WithTrace(Settings, string.Format("[Fatel][Server's Settings violation warning] Connections accepted only from {0}, but. found {1}", (string)Settings.Current.AcceptConnectionsFrom, deviceType), GetType());
                            await stream.DisposeAsync();
                            client.Close(); return;
                        }
                    }
                }


                if (req.State == HttpRequestState.None)
                {
                    req.ProcessRequestHeader(client);
                    if (string.IsNullOrEmpty(req.RequestedTarget))
                    {
                        ErrorLogger.WithTrace(Settings, "[Warning][Invalid Connection] : this connection has no data", GetType());
                        await stream.DisposeAsync();
                        client.Close();
                        return;
                    }
                }

                ///
                /// في حال وقوع اي خطأ أثناء معالجة معلومات الطلب
                ///
                switch (req.State)
                {
                    case HttpRequestState.PARSE_FAILURE:
                        Respond_PARSE_FAILURE(req, stream, client);
                        break;
                    case HttpRequestState.IO_FAILURE:
                        Respond_IO_FAILURE(req, stream, client);
                        break;
                    case HttpRequestState.GENERIC_FAILURE:
                        Respond_GENERIC_FAILURE(req, stream, client);
                        break;
                    case HttpRequestState.PERMISSION_FAILURE:
                        Respond_PERMISSION_FAILURE(req, stream, client);
                        break;
                    case HttpRequestState.SECURITY_FAILURE:
                        Respond_SECURITY_FAILURE(req, stream, client);
                        break;
                    case HttpRequestState.OK:
                        Respond_OK(req, stream, client);
                        break;
                    case HttpRequestState.UNKNOWN_RESOURCE_FAILURE:
                        Respond_UNKNOWN_RESOURCE_FAILURE(req, stream, client);
                        break;
                    case HttpRequestState.PAYLOAD_TOO_LARGE:
                        Respond_PayloodTooLarge(req, stream, client);
                        break;
                }

            }
            catch (TimeoutException exp) {
                ErrorLogger.WithTrace(Settings, string.Format("[Warning][Server error => ProcessRequest()] : type: TimeoutException. exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), GetType());
                client.Close();
            }
            catch(System.IO.IOException exp) {
                ErrorLogger.WithTrace(Settings, string.Format("[Warning][Server error => ProcessRequest()] : type: IOException. exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), GetType());
                client.Close();
            }
            catch(SocketException exp) {
                ErrorLogger.WithTrace(Settings, string.Format("[Warning][Server error => ProcessRequest()] : type: SocketException. exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), GetType());
                client.Close();
            }
            catch (ObjectDisposedException exp) {
                ErrorLogger.WithTrace(Settings, string.Format("[Warning][Server error => ProcessRequest()] : type: OjectDisposedException. exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), GetType());
                client.Close();
            }
            catch (Exception exp)
            {
                ErrorLogger.WithTrace(Settings, string.Format("[Warning][Server error => ProcessRequest()] : type: Exception. exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), GetType());
                client.Close();
            }
            
        }

        

        ~BaseServer()
        {
            Servers.Remove($"{IP}:{Port}");
        }

        #region Event handling
        public delegate void ServerStateEvent(BaseServer server, EventArgs args);
        public delegate void ClientRequestEvent(System.Net.HttpRequestHeader h, EventArgs args);

        protected virtual void OnStarted()
        {
            Started?.Invoke(this, new ServerStateEventArgs(this));
        }
        protected virtual void OnStartupError()
        {
            StartError?.Invoke(this, new ServerStateEventArgs(this));
        }
        protected virtual void OnSuspended()
        {
            Suspended?.Invoke(this, new ServerStateEventArgs(this));
        }

        protected virtual void OnShutteddown()
        {
            Shutteddown?.Invoke(this, new ServerStateEventArgs(this));
        }
        /*-----------------------  CANCELED DUE TO DROPPED PERFORMANCE  --------------------------*/
        protected virtual void OnResumed()
        {
            Resumed?.Invoke(this, new ServerStateEventArgs(this));
        }

        protected virtual void OnSmartSwitchEnd()
        {
            SmartSwitchEnd?.Invoke(this, new ServerStateEventArgs(this));
        }
        #endregion

        public void BlockClient(string client)
        {
            if (IP.Equals(client)) return;
            if (!BlockedClients.Contains(client))
            {
                BlockedClients.Add(client);
            }
        }

        public static string[] GetNeworkIPs()
        {
            List<string> l = new();
            IPAddress[] addr = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
            
            for (int i = 0; i < addr.Length; i++)
            {
                if (addr[i].AddressFamily == AddressFamily.InterNetwork)
                    l.Add(addr[i].ToString());
            }

            if (!l.Contains("127.0.0.1"))
                l.Add("127.0.0.1");

            return l.ToArray();
        }


        #region Properties
        
        public Settings Settings { get; internal set; }
        public string IP { get; private set; }
        public int Port { get; private set; }
        public string Address => $"{IP}:{Port}";
        public string FullIPOnly => $"http://{IP}/";
        public string FullAddress => $"http://{IP}:{Port}/";

        private readonly object @lock = new object();
        internal protected List<string> BlockedClients;
        internal protected Dictionary<string, Device> RegisteredClients;
        public readonly Broadcast BroadcastReceiver;
        public ControlSignal BroadcastReceiverControlSignal;
        internal System.Timers.Timer ServerTimer;
        public int RegisteredClientCount => RegisteredClients.Count;
        public IReadOnlyDictionary<string, Device> GetRegisteredClients() => RegisteredClients;

        public IReadOnlyList<string> GetBlockedClients() => BlockedClients;
        
        public bool ForgiveClient(string client) => BlockedClients.Remove(client);
        public bool IsBlockedClient(string client) => BlockedClients.Contains(client);


        //public event EventHandler Started;StartupError
        public event ServerStateEvent Suspended = delegate { };
        public event ServerStateEvent StartError = delegate { };
        public event ServerStateEvent Shutteddown = delegate { };
        public event ServerStateEvent Started = delegate { };
        public event ServerStateEvent SmartSwitchEnd = delegate { };

        public event ServerStateEvent Resumed = delegate { };

        protected ManualResetEventSlim Signal;
        protected Thread ServerThread;
        protected TcpListener Listener;

        public bool Active { get; private set; }
        public bool IsSuspended { get; private set; }

        public long RequestsCount;
        public bool IsShutdown { get; private set; }
        #endregion

    }


}
