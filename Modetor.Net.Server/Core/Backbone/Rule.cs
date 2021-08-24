/*\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
III
III          بسم الله الرحمن الرحيم
III 
\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\*/



using System;
namespace Modetor.Net.Server.Core.Backbone
{
    public class Rule
    {
        private static string[] Empty = new string[] { };
        public static Rule Corrupted
        {
            get
            {
                Rule r = new Rule("Corrupted");
                r.Path = r.HomeFile = string.Empty;
                r.Available = false;
                return r;
            }
        }
        public static Rule Root
        {
            get
            {
                Rule r = new Rule("Root");
                r.Path = r.HomeFile = Settings.RootPath;
                r.Available = false;
                return r;
            }
        }


        public Rule(string name)
        {
            AllowCrossRepositoriesRequests = Available = AllowWebSocket  = AllowServerEvent = true;SharedResources = false; 
            Name = name; HomeFile = ExclusiveForServer = string.Empty;
            Path = UploadDirectory = ConnectionHandler = string.Empty; PrivateDirectories = Empty;
            WebSocketIdelTimeout = WebSocketIdelChances = -1; ServerEventMethod = ServerEventMethod.PUSH;
            Registry = new System.Collections.Generic.Dictionary<string, dynamic>(); StartupFile = null;
        }

        public void SetName(string n) => Name = n;
        public void SetHome(string h) => HomeFile = Path+ FilePath.Build(h);
        public void SetAvailable(bool state) => Available = state;
        public void SetPath(string path) => Path = path;
        public void SetUploadDirectory(string uploadDirectory) => UploadDirectory = Path+ FilePath.Build(uploadDirectory);
        public void SetConnectionHandler(string connHandler) => ConnectionHandler = Path+ FilePath.Build(connHandler);
        public void SetAsSharedResources(bool r) => SharedResources = r;
        public void SetPrivateDirectories(string[] directories)
        {
            if (directories == null || directories.Length == 0)
            {
                return;
            }

            PrivateDirectories = new string[directories.Length];
            for (int i = directories.Length-1; i > -1; i--)
            {
                PrivateDirectories[i] = Path + FilePath.Build(directories[i]);
            }
        }
        public void SetExclusiveForServer(string r) => ExclusiveForServer = r;
        public void SetAllowWebsocket(bool v) => AllowWebSocket = v;
        public void SetWebSocketIdelTimeout(int v) => WebSocketIdelTimeout = v;
        public void SetWebSocketIdelChances(int v) => WebSocketIdelChances = v;
        public void SetAllowServerEvent(bool v) => AllowServerEvent = v;
        public void SetServerEventMethod(ServerEventMethod serverEventMethod) => ServerEventMethod = serverEventMethod;
        public void SetStartupFile(string file) => StartupFile = Path + FilePath.Build(file);
        public void SetAllowCrossRepositoriesRequests(bool v) => AllowCrossRepositoriesRequests = v;
        internal void Verify()
        {
            if(!SharedResources)
            {
                if (!System.IO.File.Exists(HomeFile))
                    throw new System.IO.FileNotFoundException(string.Format("[{0}].HomeFile file not found", Name));

                if (!string.IsNullOrEmpty(UploadDirectory) && !System.IO.Directory.Exists(UploadDirectory))
                    throw new System.IO.DirectoryNotFoundException(string.Format("[{0}].UploadDirectory directory not found", Name));

                if (!string.IsNullOrEmpty(ConnectionHandler) && !System.IO.File.Exists(ConnectionHandler))
                    throw new System.IO.FileNotFoundException(string.Format("[{0}].ConnectionHandler file not found", Name));

                if (HasStartupFile)
                {
                    if(!System.IO.File.Exists(StartupFile))
                        throw new System.IO.FileNotFoundException(string.Format("[{0}].StartupFile file not found", Name));
                    if (!StartupFile.EndsWith(".py"))
                    {
                        StartupFile = null;
                        throw new System.IO.FileNotFoundException(string.Format("[{0}].StartupFile must be a python file (*.py)", Name));
                    }
                }
            }

            foreach (string dir in PrivateDirectories)
            {
                if (!System.IO.Directory.Exists(dir))
                    throw new System.IO.DirectoryNotFoundException(string.Format("[{0}].PrivateDirectories({1}) directory not found", Name, dir));
            }

            if (SharedResources && (HasConnectionHandler || SupportUploads || HasStartupFile || !AllowCrossRepositoriesRequests))
            {
                throw new NotSupportedException(string.Format("[{0}].SharedResources cannot be valid with [HasConnectionHandler, SupportUploads, HasStartupFile, AllowCrossRepositoriesRequests] = true", Name));
            }
        }

        /*\ Register FAQ
        >>> Last night. i've implemented such a beautifull feature called 'Registry' 
        >>> Q.1 : What is Registry ? 
        >>> A.1 : It's a modern way to store data in server(in RAM) while it retain dynamic binding, it also works much better than servers' 'cookies'.
        >>>
        >>> Q.2 : Does it cost any overhead or extra memory allocations ?
        >>> A.2 : Overhead ? No. Memory allocations depends on what data it stores!
        \*/
        public void Register(string key, dynamic obj)
        {
            if(!Registry.ContainsKey(key)) Registry.Add(key, obj);
        }
        public void Unregister(string key)
        {
            if (Registry.ContainsKey(key)) Registry.Remove(key);
        }

        /*\
        ### High level methods to work with Registry
        \*/

        public void RegisterConnection(string key, System.Net.Sockets.TcpClient client) => Register(key, client);
        public void CreateConnectionStack(string key, System.Collections.Generic.List<System.Net.Sockets.TcpClient> stack) => Register(key, stack);
        public System.Collections.Generic.List<System.Net.Sockets.TcpClient> GetConnectionStack(string key)
        {
            if (Registry.ContainsKey(key) && Registry[key].GetType() == typeof(System.Collections.Generic.List<System.Net.Sockets.TcpClient>))
                return (System.Collections.Generic.List < System.Net.Sockets.TcpClient >)Registry[key];
            else
                return null;
        }
        public async void RemoveConnectionStack(string key)
        {
            if (!Registry.ContainsKey(key)) return;

            System.Collections.Generic.List<System.Net.Sockets.TcpClient> clients = Registry[key];
            int count = clients.Count;
            for (int i = 0; i < count; i++)
            {
                try
                {
                    await clients[i].GetStream().DisposeAsync();
                    clients[i].Close();
                    clients.RemoveAt(i);
                    count--;
                }
                catch {}
            }

            Registry.Remove(key);
        }
        public void RegisterDynamicCollection(string key, System.Collections.Generic.Dictionary<string, dynamic> dyndic) => Registry.Add(key, dyndic);
        public void ClearRegistry()
        {
            Registry.Clear();
            //string[] keys = Registry.Keys.ToArray();

            //for(int i = 0; i < keys.Length; i++)
            //{
            //    Registry.Remove(keys[i]);

            //}
        }


        #region Properties
        public string Name { get; private set; } 
        public string HomeFile { get; private set; }
        public bool Available { get; private set; }

        // added in 18.1.2021
        public string Path { get; private set; }

        // added in 19.1.2021
        public string UploadDirectory { get; private set; }
        public string ConnectionHandler { get; private set; }
        public bool SupportUploads => !string.IsNullOrEmpty(UploadDirectory);
        public bool HasConnectionHandler => !string.IsNullOrEmpty(ConnectionHandler);
        public bool SharedResources { get; private set; }

        // added in 31.1.2021
        public string[] PrivateDirectories { get; private set; }
        
        // added in 1.2.2021
        public string ExclusiveForServer { get; private set; }

        // added in 7.2.2021
        public bool AllowWebSocket { get; private set; }

        // added in 11.2.2021
        public int WebSocketIdelTimeout { get; private set; }
        public int WebSocketIdelChances { get; private set; }

        // added in 12.2.2021 [Dropped] -- Moved to Server's Settings
        ///public string[] VirtualExtensions { get; private set; }
        ///public System.Collections.Generic.Dictionary<string, string> VirtualExtensionsDictionary { get; private set; }
        
        // added in 14.2.2021
        public bool AllowServerEvent { get; private set; }
        public System.Collections.Generic.Dictionary<string, dynamic> Registry; // POWERFUL 🔥
        public ServerEventMethod ServerEventMethod { get; private set; }
        
        // added in 16.4.2021
        public string StartupFile { get; private set; }
        public bool HasStartupFile => !string.IsNullOrEmpty(StartupFile);

        // added in 31.7.2021
        public bool AllowCrossRepositoriesRequests { get; private set; }
        #endregion
    }





}
