using System;
using Modetor.Net.Server.Core.HttpServers;
using Logger = Modetor.Net.Server.Core.Backbone.ErrorLogger;
using Settings = Modetor.Net.Server.Core.Backbone.Settings;

namespace Modetor_Server
{
    internal partial class Program
    {
        private const char PaddingChar = ' ';

        static void Main(string[] args)
        {

            servers = new();
            Logger.Initialize();

            dynamic l = null;
            try { if (System.IO.File.Exists(".workplace")) l = Newtonsoft.Json.JsonConvert.DeserializeObject(System.IO.File.ReadAllText(".workplace")); }
            catch(Exception exp) { Red("[ServerInitialization] : failed to resolve '.workplace' file.\n");
                Logger.WithTrace(string.Format("[Fatel][Backend error => ReadFileBytesAction()] : exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), typeof(Program));
            }
            
            if(l == null)
            {
                Red("[ServerInitialization] : FATEL error. server must be maintained.\n");
                return;
            }

            Settings.SetSource(l);
            
            

            if (args != null && args.Length != 0)
            {
                foreach (string cmd in args)
                    Initial(cmd);

            }

            Welcome();
        }

        private static string _selected_server = string.Empty;
        private static System.Collections.Generic.List<string> servers;
        private static void Initial(string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                return;
            }

            if (command.Equals("help"))
            {
                ShowHelpCommands();
            }
            else if (command.StartsWith("ls "))
            {
                HandleListCommand(command[3..].Trim());
            }
            else if (command.StartsWith("run "))
            {
                if (command.Length == 4 || (!command.Contains(':') && !command.Contains("http")))
                {
                    Red("Syntax Error. Run command must be like run 127.0.0.1:80\n");
                }
                else
                {
                    HandleRunCommand(command[4..].Trim());
                }
            }
            else if (command.Equals("stop"))
            {
                if (_selected_server.Equals(string.Empty))
                {
                    Yellow("Select a server first\n");
                    return;
                }
                else
                {
                    if (!BaseServer.Servers[_selected_server].IsSuspended)
                        BaseServer.Servers[_selected_server].Suspend();
                    else
                        Red("Unknown server. Stop comman can only stop initialized and activated servers");

                }
            }
            else if (command.Equals("kill"))
            {
                if (_selected_server.Equals(string.Empty))
                {
                    Yellow("Select a server first\n");
                    return;
                }
                else
                {
                    BaseServer server = BaseServer.Servers[_selected_server];
                    if (server.Active)
                        server.Shutdown();
                    else
                        BaseServer.Servers.Remove(_selected_server);

                    servers.Remove(_selected_server);
                    _selected_server = string.Empty;
                }
            }
            else if (command.StartsWith("kill "))
            {
                HandleKillCommand(command[5..].Trim());
            }
            else if (command.Equals("clr"))
            {
                Console.Clear();
            }
            else if (command.StartsWith("clr "))
            {
                HandleClearCommand(command[4..].Trim());
            }
            else if (command.Equals("ssyncall"))
            {
                foreach (System.Collections.Generic.KeyValuePair<string, BaseServer> server in BaseServer.Servers)
                {
                    server.Value.Settings.Update();
                }
            }
            else if(command.StartsWith("ssync="))
            {
                string address = command.Split('=')[1].Trim();

                if (BaseServer.Servers.ContainsKey(address))
                {
                    BaseServer server = BaseServer.Servers[address];

                    server.Settings.Update();
                }
                else
                {
                    Red("there's no server listining at address : " + address);
                }
            }
            else if(command.Equals("sshow"))
            {
                foreach(System.Collections.Generic.KeyValuePair<string, BaseServer> server in BaseServer.Servers)
                {
                    Console.WriteLine("Server {0} \n{1}" , server.Key, server.Value.Settings.Serialize());
                }
            }

            else if (command.StartsWith("ccl="))
            {
                string address = command.Split('=')[1].Trim();

                if (BaseServer.Servers.ContainsKey(address))
                {
                    BaseServer server = BaseServer.Servers[address];
                    if ((int)server.Settings.Current.MaxClientsCount <= 0)
                    {
                        Red("this server do not record incoming connections. MaxClientsCount <= 0");
                    }
                    else
                    {
                        Green($"Cleared {server.RegisteredClientCount} clients\n");
                        server.ClearRegisteredClients();
                    }
                }
                else
                {
                    Red("there's no server listining at address : " + address);
                }
            }
            else if (command.StartsWith("vbl="))
            {
                string address = command.Split('=')[1].Trim();

                if (BaseServer.Servers.ContainsKey(address))
                {
                    BaseServer server = BaseServer.Servers[address];
                    if ((int)server.Settings.Current.MaxClientsCount <= 0)
                    {
                        Red("this server do not record incoming connections");
                    }
                    else
                    {
                        Console.WriteLine("[#Num - #Address{0} - #OS{0} - #Version{0}     - #Architecture        - #Brand]", "".PadLeft(10, PaddingChar));
                        int count = 1;
                        foreach (System.Collections.Generic.KeyValuePair<string, Modetor.Net.Server.Core.Backbone.Device> client in server.GetRegisteredClients())
                        {
                            Console.WriteLine(" {0}{7}- {1} - {2}{8} - {3}{6} - {4}{6} - {5}",
                                count++, client.Key.PadRight(18, PaddingChar),
                                client.Value.Platform.ToString(),
                                client.Value.Version,
                                client.Value.Architecture,
                                client.Value.Brand, "".PadRight(18, PaddingChar),
                                "".PadRight(4, PaddingChar),
                                "".PadRight(6, PaddingChar));
                        }

                    }
                }
                else
                {
                    Red("there's no server listining at address : " + address);
                }
            }
            else if (command.StartsWith("select "))
            {
                command = command[command.IndexOf(' ')..].Trim();
                if(command.Length == 0)
                {
                    Red("Syntax Error. Select command must be like select [address/index]\n");
                    return;
                }
                string server = string.Empty;
                if (int.TryParse(command, out int index))
                {
                    server = BaseServer.Servers[servers[index]].Address;
                    _selected_server = BaseServer.Servers[servers[index]].Address;
                }
                else
                {
                    if(!servers.Contains(command))
                    {
                        Red("Command.Select error : server '" + command + "' not found\n");
                        return;
                    }
                    server = command;
                    _selected_server = command;
                }

                Green("Server '"+server+"' selected\n");
            }
        }

        private static void HandleClearCommand(string command)
        {
            if (_selected_server.Equals(string.Empty))
            {
                Yellow("Select a server first\n");
                return;
            }

            if (command.Equals("-clients blocked"))
            {
                System.Collections.Generic.IReadOnlyList<string> blockList = BaseServer.Servers[_selected_server].GetBlockedClients();
                for (int i = blockList.Count - 1; i > -1; i--)
                    BaseServer.Servers[_selected_server].ForgiveClient(blockList[i]);
            }
            else if (command.StartsWith("-clients -blocked "))
            {
                string client = command[18..].Trim();
                if(client.Equals(string.Empty))
                {
                    Red("Syntax Error. clr -clients -blocked command must be like : clr -clients -blocked [ip]\n");
                    return;
                }
                BaseServer.Servers[_selected_server].ForgiveClient(client);
            }
            else if (command.Equals("-clients")) {
                BaseServer.Servers[_selected_server].ClearRegisteredClients();
            }
        }

        private static void HandleRunCommand(string command)
        {
            if(command.StartsWith("http ")) {
                HandleRunCommand(command[4..].Trim()+":80");
                return;
            }
            string[] address = command.Split(':');
            int port;
            if (!int.TryParse(address[1], out port))
            {
                port = 80;
                Red("[Command.Run] : Port value must be a valid integer. fallback port(80) will be used\n");
            }
            if (address[0].Equals("*"))
            {
                foreach (string addr in BaseServer.GetNeworkIPs())
                {
                    BaseServer server;
                    server = BaseServer.InitializeServer(addr, port);
                    if (server == null)
                        return;

                    if (server.IsSuspended)
                    {
                        server.Resume();
                        return;
                    }
                    else if (server.Active)
                        return;

                    SetupServerEvent(server);
                    server.Start();
                    servers.Add($"{addr}:{port}");
                }
            }
            else
            {
                BaseServer server;
                server = BaseServer.InitializeServer(address[0], port);
                if (server == null)
                    return;

                if (server.IsSuspended)
                {
                    server.Resume();
                    return;
                }
                else if (server.Active)
                    return;

                SetupServerEvent(server);
                server.Start();
                servers.Add($"{address[0]}:{port}");
            }
        }

        private static void HandleKillCommand(string command)
        {

            switch (command)
            {
                case "all":
                    for (int i = servers.Count - 1; i > -1; i--)
                    {
                        BaseServer.Servers[servers[i]].Shutdown();
                    }

                    break;
                case "active":
                    for (int i = servers.Count - 1; i > -1; i--)
                    {
                        if (BaseServer.Servers[servers[i]].Active && !BaseServer.Servers[servers[i]].IsSuspended)
                        {
                            BaseServer.Servers[servers[i]].Shutdown();
                        }
                    }
                    break;
                case "suspended":
                    for (int i = servers.Count - 1; i > -1; i--)
                    {

                        if (BaseServer.Servers[servers[i]].Active && BaseServer.Servers[servers[i]].IsSuspended)
                        {
                            BaseServer.Servers[servers[i]].Shutdown();
                        }
                    }
                    break;
                default:
                    for (int i = servers.Count - 1; i > -1; i--)
                    {
                        BaseServer.Servers[servers[i]].Shutdown();
                    }

                    break;
            }
        }
        private static void HandleListCommand(string command)
        {
            if (command.Equals("-ips"))
                PrintAvailableIPs();
            else if (command.Equals("-servers"))
                PrintServersList(string.Empty);
            else if (command.StartsWith("-servers "))
                PrintServersList(command[8..].Trim());
            else if (command.Equals("-clients"))
            {
                HandleClientViewCommand();
            }
            else if (command.StartsWith("-clients "))
            {
                HandleClientOptionViewCommand(command[9..]);
            }
        }

        private static void HandleClientOptionViewCommand(string command)
        {
            if (_selected_server.Equals(string.Empty))
            {
                Yellow("Select a server first\n");
            }
            else
            {
                BaseServer server = BaseServer.Servers[_selected_server];
                
                if(command.Equals("blocked"))
                {
                    System.Collections.Generic.IReadOnlyList<string> blockedClients = server.GetBlockedClients();
                    if (blockedClients.Count <= 0)
                    {
                        Yellow("this server has no blocked IPs\n");
                    }
                    else
                    {
                        Console.WriteLine("[#Num - #IP{0}]", "".PadLeft(10, PaddingChar));
                        int count = 1;
                        foreach (string client in blockedClients)
                        {
                            Console.WriteLine(" {0} - {1}", count++, client);
                        }
                    }
                }
            }
        }

        private static void HandleClientViewCommand()
        {
            if (_selected_server.Equals(string.Empty))
            {
                Yellow("Select a server first\n");
            }
            else
            {
                BaseServer server = BaseServer.Servers[_selected_server];
                if ((int)server.Settings.Current.MaxClientsCount <= 0)
                {
                    Yellow("this server do not record incoming connections\n");
                }
                else
                {
                    Console.WriteLine("[#Num - #Address{0} - #OS{0} - #Version{0}     - #Architecture        - #Brand]", "".PadLeft(10, PaddingChar));
                    int count = 1;
                    foreach (System.Collections.Generic.KeyValuePair<string, Modetor.Net.Server.Core.Backbone.Device> client in server.GetRegisteredClients())
                    {
                        Console.WriteLine(" {0}{7}- {1} - {2}{8} - {3}{6} - {4}{6} - {5}",
                            count++, client.Key.PadRight(18, PaddingChar),
                            client.Value.Platform.ToString(),
                            client.Value.Version,
                            client.Value.Architecture,
                            client.Value.Brand, "".PadRight(18, PaddingChar),
                            "".PadRight(4, PaddingChar),
                            "".PadRight(6, PaddingChar));
                    }
                }
            }
        }

        private static void PrintServersList(string command)
        {
            switch (command)
            {
                case "all":
                    foreach (System.Collections.Generic.KeyValuePair<string, BaseServer> server in BaseServer.Servers)
                    {
                        Console.WriteLine(server.Key);
                    }
                    break;
                case "active":
                    foreach (System.Collections.Generic.KeyValuePair<string, BaseServer> server in BaseServer.Servers)
                    {
                        if (server.Value.Active && !server.Value.IsSuspended)
                        {
                            Console.WriteLine(server.Key);
                        }
                    }
                    break;
                case "suspended":
                    foreach (System.Collections.Generic.KeyValuePair<string, BaseServer> server in BaseServer.Servers)
                    {
                        if (server.Value.Active && server.Value.IsSuspended)
                        {
                            Console.WriteLine(server.Key);
                        }
                    }
                    break;
                default:

                    if (BaseServer.Servers.Count == 0)
                        Yellow("there're no server(s) yet\n");
                    else
                    {
                        int index = 0;

                        Console.WriteLine("[#Idx - #IP{0} - #Port{0} - #State]", "".PadLeft(15, PaddingChar));
                        foreach (System.Collections.Generic.KeyValuePair<string, BaseServer> server in BaseServer.Servers)
                        {
                             Console.WriteLine(" {0}{5}- {1} - {2}{4} - {3}", 
                                                    index++, 
                                                    server.Value.IP.PadRight(18, PaddingChar), 
                                                    server.Value.Port.ToString(), 
                                                    (server.Value.Active ? server.Value.IsSuspended ? "Suspended" : "Working" : "Not Active"), 
                                                    "".PadRight(18, PaddingChar), 
                                                    "".PadRight(4, PaddingChar)
                             );
                        }
                    }
                    break;
            }
        }

        private static void ShowHelpCommands()
        {
            Blue("All  available commands { \n");
            Green(string.Empty.PadLeft(4, PaddingChar) +"help".PadRight(12, PaddingChar) +" - "); Console.WriteLine(" prints all commands");
            Green(string.Empty.PadLeft(4, PaddingChar) + "run".PadRight(12, PaddingChar) + " - "); Console.WriteLine(" used to start the server. e.g: run [address] | [cmd = {http}]");
            Green(string.Empty.PadLeft(4, PaddingChar) +"stop".PadRight(12, PaddingChar) + " - "); Console.WriteLine(" used to suspend the server(not killing it). requires selecting a server");
            Green(string.Empty.PadLeft(4, PaddingChar) + "kill".PadRight(12, PaddingChar) + " - "); Console.WriteLine(" used to kill the server. e.g: kill | kill [option]");
            Green(string.Empty.PadLeft(4, PaddingChar) + "vcl".PadRight(12, PaddingChar) + " - "); Console.WriteLine(" lists all registered clients");
            Green(string.Empty.PadLeft(4, PaddingChar) + "ccl".PadRight(12, PaddingChar) + " - "); Console.WriteLine(" clears all registered clients");
            Green(string.Empty.PadLeft(4, PaddingChar) + "ssync".PadRight(12, PaddingChar) + " - "); Console.WriteLine(" refresh settings in particular server");
            Green(string.Empty.PadLeft(4, PaddingChar) + "ssyncall".PadRight(12, PaddingChar) + " - "); Console.WriteLine(" refresh settings in all servers");
            Green(string.Empty.PadLeft(4, PaddingChar) + "sshow".PadRight(12, PaddingChar) + " - "); Console.WriteLine(" prints the settings that applys to servers");
            Green(string.Empty.PadLeft(4, PaddingChar) + "lsserv".PadRight(12, PaddingChar) + " - "); Console.WriteLine(" list all servers");
            Green(string.Empty.PadLeft(4, PaddingChar) + "lsips".PadRight(12, PaddingChar) + " - "); Console.WriteLine(" list available ips");
            Green(string.Empty.PadLeft(4, PaddingChar) + "kill".PadRight(12, PaddingChar) + " - "); Console.WriteLine(" Kills the specified servers"); 
            Green(string.Empty.PadLeft(4, PaddingChar) + "clr".PadRight(12, PaddingChar) + " - "); Console.WriteLine(" clear console's content");
            Blue("}\n");
        }




        private static int GetKeyFromString()
        {
            const string key = "aQ09-XmAqPtLsaSmr";
            int result = key.Length * key.Length;

            foreach (char c in key)
            {
                if (c % 2 == 0)
                {
                    result += c*2;
                }
                else
                {
                    result -= c;
                }
            }
            Console.WriteLine("--------- The Key : {0} -----------", result);
            return result;
        }
        private static void EncryptFile()
        {
            int key = GetKeyFromString();
            byte[] bytes = System.IO.File.ReadAllBytes(@"C:\Users\Mohammad\Desktop\vlc-3.0.6-win32.exe");
            System.IO.BufferedStream buf = new System.IO.BufferedStream(System.IO.File.Create(@"C:\Users\Mohammad\Desktop\vlc-3.0.6-win32.xxx"));
            foreach (byte item in bytes)
            {
                buf.WriteByte((byte)(item + key));
                //buf.Flush();
            }
            buf.Flush();
            buf.Dispose();
        }
        private static void DecryptFile()
        {
            int key = GetKeyFromString();
            byte[] bytes = System.IO.File.ReadAllBytes(@"C:\Users\Mohammad\Desktop\vlc-3.0.6-win32.xxx");
            System.IO.BufferedStream buf = new System.IO.BufferedStream(System.IO.File.Create(@"C:\Users\Mohammad\Desktop\New vlc-3.0.6-win32.exe"));
            foreach (byte item in bytes)
            {
                buf.WriteByte((byte)(item - key));
            }
            buf.Flush();
            buf.Dispose();
        }

    }


    
}
