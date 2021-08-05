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
            else if (command.Equals("lsips"))
            {
                PrintAvailableIPs();
            }
            else if (command.StartsWith("run="))
            {
                if (command.Length == 4 || !command.Contains(':'))
                {
                    Red("Syntax Error. Run command must be like run=127.0.0.1:80");
                }
                else
                {
                    string[] address = command.Split('=')[1].Trim().Split(':');
                    int port;
                    if (!int.TryParse(address[1], out port))
                    {
                        port = 80;
                        Red("[Command.Run] : Port value must be a valid integer. fallback port(80) will be used");
                    }

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
                }
            }
            else if (command.StartsWith("stop="))
            {
                if (command.Length == 5 || !command.Contains(':'))
                {
                    Red("Syntax Error. Stop command must be like stop=127.0.0.1:80");
                }
                else
                {
                    string address = command.Split('=')[1].Trim();

                    if (BaseServer.Servers.ContainsKey(address))
                    {
                        BaseServer.Servers[address].Suspend();
                        //new System.Threading.Thread(() => BaseServer.Servers[address].Suspend())
                        //{ IsBackground = true, Priority = System.Threading.ThreadPriority.Lowest }.Start();
                    }
                    else
                        Red("Unknown server. Stop comman can only stop initialized and activated servers");

                }
            }
            else if (command.StartsWith("kill="))
            {
                if (command.Length == 4 || !command.Contains(':'))
                {
                    Red("Syntax Error. Kill command must be like run=127.0.0.1:80");
                }
                else
                {
                    string[] address = command.Split('=')[1].Trim().Split(':');
                    int port;
                    if (!int.TryParse(address[1], out port))
                    {
                        port = 80;
                        Red("[Command.Kill] : Port value must be a valid integer. fallback port(80) will be used");
                    }

                    BaseServer server;
                    server = BaseServer.InitializeServer(address[0], port);
                    if (server.Active)
                    {
                        server.Shutdown();
                    }


                    //else if (server.IsSuspended)
                    //{
                    //    server.Resume();
                    //    return;
                    //}

                    //SetupServerEvent(server);
                    //server.Start();
                }
            }
            else if (command.StartsWith("killserv"))
            {
                command = command[8..].Trim();

                string[] a = new string[BaseServer.Servers.Keys.Count];
                BaseServer.Servers.Keys.CopyTo(a, 0);

                switch (command)
                {
                    case "all":
                        for (int i = a.Length - 1; i > -1; i--)
                        {
                            BaseServer.Servers[a[i]].Shutdown();
                        }

                        break;
                    case "active":
                        for (int i = a.Length - 1; i > -1; i--)
                        {
                            if (BaseServer.Servers[a[i]].Active && !BaseServer.Servers[a[i]].IsSuspended)
                            {
                                BaseServer.Servers[a[i]].Shutdown();
                            }
                        }
                        break;
                    case "suspended":
                        for (int i = a.Length - 1; i > -1; i--)
                        {

                            if (BaseServer.Servers[a[i]].Active && BaseServer.Servers[a[i]].IsSuspended)
                            {
                                BaseServer.Servers[a[i]].Shutdown();
                            }
                        }
                        break;
                    default:
                        for (int i = a.Length - 1; i > -1; i--)
                        {
                            BaseServer.Servers[a[i]].Shutdown();
                        }

                        break;
                }
            }
            else if (command.StartsWith("lsserv"))
            {
                command = command[6..].Trim();

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
                            int count = 1;

                            Console.WriteLine("[#Num - #IP{0} - #Port{0} - #State]", "".PadLeft(15, PaddingChar));
                            foreach (System.Collections.Generic.KeyValuePair<string, BaseServer> server in BaseServer.Servers)
                            {
                                Console.WriteLine(" {0}{5}- {1} - {2}{4} - {3}", count, server.Value.IP.PadRight(18, PaddingChar), server.Value.Port.ToString(), (server.Value.Active ? server.Value.IsSuspended ? "Suspended" : "Working" : "Not Active"), "".PadRight(18, PaddingChar), "".PadRight(4, PaddingChar));
                                //Console.WriteLine(server.Key);
                            }
                        }
                        break;
                }

            }
            else if (command.Equals("clr"))
            {
                Console.Clear();
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
            else if (command.StartsWith("vcl=")) {
                string address = command.Split('=')[1].Trim();

                if (BaseServer.Servers.ContainsKey(address))
                {
                    BaseServer server = BaseServer.Servers[address];
                    if((int)server.Settings.Current.MaxClientsCount <= 0)
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
        }

        private static void ShowHelpCommands()
        {
            Blue("All  available commands { \n");
            Green(string.Empty.PadLeft(4, PaddingChar) +"help".PadRight(12, PaddingChar) +" - "); Console.WriteLine(" prints all commands");
            Green(string.Empty.PadLeft(4, PaddingChar) + "run".PadRight(12, PaddingChar) + " - "); Console.WriteLine(" used to start the server. e.g: run=127.0.0.1:80");
            Green(string.Empty.PadLeft(4, PaddingChar) +"stop".PadRight(12, PaddingChar) + " - "); Console.WriteLine(" used to suspend the server(not killing it). e.g: stop=127.0.0.1:80");
            Green(string.Empty.PadLeft(4, PaddingChar) + "kill".PadRight(12, PaddingChar) + " - "); Console.WriteLine(" used to kill the server. e.g: kill=127.0.0.1:80");
            Green(string.Empty.PadLeft(4, PaddingChar) + "vcl".PadRight(12, PaddingChar) + " - "); Console.WriteLine(" lists all registered clients");
            Green(string.Empty.PadLeft(4, PaddingChar) + "ssync".PadRight(12, PaddingChar) + " - "); Console.WriteLine(" refresh settings in particular server");
            Green(string.Empty.PadLeft(4, PaddingChar) + "ssyncall".PadRight(12, PaddingChar) + " - "); Console.WriteLine(" refresh settings in all servers");
            Green(string.Empty.PadLeft(4, PaddingChar) + "lsserv".PadRight(12, PaddingChar) + " - "); Console.WriteLine(" list all servers");
            Green(string.Empty.PadLeft(4, PaddingChar) + "lsips".PadRight(12, PaddingChar) + " - "); Console.WriteLine(" list available ips");
            Green(string.Empty.PadLeft(4, PaddingChar) + "killserv".PadRight(12, PaddingChar) + " - "); Console.WriteLine(" Kills the specified servers"); 
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
