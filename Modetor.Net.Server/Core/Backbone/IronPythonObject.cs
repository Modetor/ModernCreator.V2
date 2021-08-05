using Modetor.Net.Server.Core.HttpServers;
using System;
using MySql.Data;

namespace Modetor.Net.Server.Core.Backbone
{
    /// <summary>
    /// 
    /// </summary>
    class IronPythonObject
    {




        public static IronPythonObject GetObject(string file) => new IronPythonObject(file);
        private IronPythonObject()
        {
            DefaultSearchPaths = new System.Collections.Generic.List<string>() {
                ".",
                Settings.RootPath+FilePath.Build(FilePath.Build("ipy", "Lib")),
                Settings.RootPath+FilePath.Build(FilePath.Build("ipy", "DLLs")) ,
                Settings.RootPath+FilePath.Build(FilePath.Build("ipy", "Lib", "site-packages"))
            };
            Engine = IronPython.Hosting.Python.CreateEngine()/*.CreateScope()*/;
            Scope = Engine.CreateScope();
        }
        internal IronPythonObject(string file)
        {
            DefaultSearchPaths = new System.Collections.Generic.List<string>() {
                ".",
                Settings.RootPath+FilePath.Build(FilePath.Build("ipy", "Lib")),
                Settings.RootPath+FilePath.Build(FilePath.Build("ipy", "DLLs")) ,
                Settings.RootPath+FilePath.Build(FilePath.Build("ipy", "Lib", "site-packages"))
            };
            scriptfile = file;
            Engine = IronPython.Hosting.Python.CreateEngine();
            Scope = Engine.CreateScope();
        }
        public void SetSearchPaths(string[] p) => Engine.SetSearchPaths(p);
        public void AddPath(string path) => DefaultSearchPaths.Add(path);
        public void Run()
        {
            Engine.ExecuteFile(scriptfile, Scope);
        }
        public readonly System.Collections.Generic.List<string> DefaultSearchPaths;
        public Microsoft.Scripting.Hosting.ScriptEngine Engine { get; private set; }
        public dynamic Scope { get; private set; }
        internal string scriptfile = null;










        internal static void SetupScope(dynamic Scope, System.Net.Sockets.TcpClient client, Settings Settings, HttpRequestHeader req = null)
        {
            System.Diagnostics.Contracts.Contract.Requires(Settings != null);

            if(client != null && client.Connected && client.Client != null)
            {
                //System.Net.Sockets.TransmitFileOptions.UseSystemThread
                //client.Client.SendFile(,,, System.Net.Sockets.TransmitFileOptions.UseKernelApc)
                Scope.Client = client;
                try { Scope.Stream = client?.GetStream(); }
                catch (Exception exp) {
                    ErrorLogger.WithTrace(Settings, string.Format("[Fatel][Backend error => SetupScope()] : exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), typeof(PythonRunner));
                }
                finally
                {
                    Scope.Stream = new System.Net.Sockets.NetworkStream(client.Client); //new System.IO.MemoryStream(new byte[2048], true);
                }
                Scope.SendAsync = new Action<byte[]>(async (bytes) =>
                {
                    try
                    {
                        if (!Scope.Stream.CanWrite) return;
                        await Scope.Stream.WriteAsync(bytes);
                        await Scope.Stream?.FlushAsync();

                        if ((bool)Scope.AutoClose)
                            client.Close();

                    }
                    catch (Exception exp)
                    {
                        ErrorLogger.WithTrace(Settings, string.Format("[Fatel][Backend error => SendAsync()] : exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), typeof(IronPythonObject));
                    }
                });
                Scope.SendTextAsync = new Action<string>(async (text) =>
                {
                    try
                    {
                        if (!Scope.Stream.CanWrite) return;
                        await Scope.Stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(text));
                        await Scope.Stream?.FlushAsync();

                        if ((bool)Scope.AutoClose)
                            client.Close();
                    }
                    catch (Exception exp)
                    {
                        ErrorLogger.WithTrace(Settings, string.Format("[Fatel][Backend error => SendTextAsync()] : exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), typeof(IronPythonObject));
                    }
                });
                Scope.SendFile = new Action<string, bool>((file, isBigFile) => { client.Client.SendFile(file, null, null, isBigFile ? System.Net.Sockets.TransmitFileOptions.UseSystemThread : System.Net.Sockets.TransmitFileOptions.UseDefaultWorkerThread); });
                Scope.SendText = new Action<string>((text) =>
                {
                    try
                    {
                        if (!Scope.Stream.CanWrite) return;
                        Scope.Stream.Write(System.Text.Encoding.UTF8.GetBytes(text));
                        Scope.Stream.Flush();

                        if ((bool)Scope.AutoClose)
                            client.Close();
                    }
                    catch (Exception exp)
                    {
                        ErrorLogger.WithTrace(Settings, string.Format("[Fatel][Backend error => SendText()] : exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), typeof(IronPythonObject));
                    }
                });
                Scope.Send = new Action<byte[]>(bytes =>
                {
                    try
                    {
                        Scope.Stream.Write(bytes);
                        Scope.Stream.Flush();

                        if ((bool)Scope.AutoClose)
                            client.Close();
                    }
                    catch (Exception exp)
                    {
                        ErrorLogger.WithTrace(Settings, string.Format("[Fatel][Backend error => Send()] : exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), typeof(IronPythonObject));
                    }
                });
                Scope.ReadAsync = new Action<byte[], int, int>
                    (async (buffer, offest, length) => await Scope.Stream?.ReadAsync(buffer, offest, length));
                Scope.Read = new Action<byte[], int, int>(
                    (buffer, offest, length) => Scope.Stream?.Read(buffer, offest, length));
                Scope.Close = new Action(() => client?.Close());
                Scope.IsClientConnected = new Func<bool>(() => client.IsConnected());

            }

            Scope.AutoClose = false;
            Scope.ReadFileBytes = new Func<string, byte[]>(f => {
                try
                {
                    return System.IO.File.ReadAllBytes(f);
                }
                catch (Exception exp)
                {
                    ErrorLogger.WithTrace(Settings, string.Format("[Fatel][Backend error => ReadFileBytes()] : exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), typeof(IronPythonObject));
                    return new byte[] { 0 };
                }

            });
            Scope.ReadFileBytesAction = new Action<string, Action<byte[]>>((f, work) => {
                try
                {
                    using System.IO.FileStream stream = new System.IO.FileStream(f, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
                    byte[] buffer = new byte[2048];
                    int read = 0;
                    while ((read = stream.Read(buffer, 0, buffer.Length)) != 0)
                        work?.Invoke(buffer[..read]);
                }
                catch (Exception exp)
                {
                    ErrorLogger.WithTrace(Settings, string.Format("[Fatel][Backend error => ReadFileBytesAction()] : exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), typeof(IronPythonObject));
                }
            });
            Scope.ReadFileBytesActionAsync = new Action<string, Action<byte[]>>(async (f, work) => {
                try
                {
                    using System.IO.FileStream stream = new(f, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
                    byte[] buffer = new byte[2048];
                    int read = 0;
                    while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length))) != 0)
                        work?.Invoke(buffer[..read]);
                }
                catch (Exception exp)
                {
                    ErrorLogger.WithTrace(Settings, string.Format("[Fatel][Backend error => ReadFileBytesActionAsync()] : exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), typeof(IronPythonObject));
                }
            });
            Scope.GetFileSize = new Func<string, long>((s) => new System.IO.FileInfo(s).Length);
            Scope.ToUTF8Bytes = new Func<string,byte[]>( t => System.Text.Encoding.UTF8.GetBytes(t));
            Scope.ToUTF8String = new Func<byte[], string>(t => System.Text.Encoding.UTF8.GetString(t));
            
            
            Scope.IsConnected = new Func<System.Net.Sockets.TcpClient, bool>(t => t.IsConnected());


            Scope.MySqlOpenConnection = new Func<string,MySql.Data.MySqlClient.MySqlConnection>((conn_str) =>
            {
                MySql.Data.MySqlClient.MySqlConnection c = new MySql.Data.MySqlClient.MySqlConnection(conn_str);
                c.Open();
                return c;
            });
            Scope.MySqlQueryReader = new Func<string,MySql.Data.MySqlClient.MySqlConnection, MySql.Data.MySqlClient.MySqlDataReader>((query,c) =>
            {
                MySql.Data.MySqlClient.MySqlCommand cmd = c.CreateCommand();
                cmd.CommandText = query;
                return cmd.ExecuteReader();
            });
            Scope.MySqlNonQuery = new Func<string, MySql.Data.MySqlClient.MySqlConnection, int>((query, c) =>
            {
                MySql.Data.MySqlClient.MySqlCommand cmd = c.CreateCommand();
                cmd.CommandText = query;
                return cmd.ExecuteNonQuery();
            });
            
            Scope.IntentForResult = new Func<string, string, string>((relative_path, parameters) =>
            {
                string referer = string.Empty;

                if(req == null)
                    return string.Empty;

                if (req.HeaderKeys.ContainsKey("Referer"))
                {
                    string temp = req.HeaderKeys["Referer"].Replace(req.HeaderKeys["Referer"].StartsWith("https://") ? "https://" : "http://", string.Empty);
                    referer = temp[(temp.IndexOf('/') + 1)..];
                }

                HttpRequestHeader r;
                Tuple<bool, string> tuple = PathResolver.Resolve(Settings, relative_path, referer);
                if(tuple.Item1)
                {
                    r = new(req, tuple.Item2);
                    r.ClearParameters();
                    if(!string.IsNullOrEmpty(parameters))
                        r.AddParameters(parameters);
                    return System.Text.Encoding.UTF8.GetString(PythonRunner.Run(r, null));
                }
                else
                    ErrorLogger.WithTrace(Settings, string.Format("[Warn][Backend error => SetupScope()] : file specified doesn't exsist"), typeof(PythonRunner));
                return null;
            });
            Scope.Intent = new Action<string, string>((relative_path, parameters) => Scope.IntentForResult(relative_path, parameters));
            Scope.URLEncode = new Func<string, string>((s) => System.Web.HttpUtility.UrlEncode(s));
            Scope.URLDecode = new Func<string, string>((s) => System.Web.HttpUtility.UrlDecode(s));

            Action<string, string, bool> action = new((relative_path, parameters, removeAble) =>
            {
                string referer = string.Empty;

                if (req == null)
                    return;

                if (req.HeaderKeys.ContainsKey("Referer"))
                {
                    string temp = req.HeaderKeys["Referer"].Replace(req.HeaderKeys["Referer"].StartsWith("https://") ? "https://" : "http://", string.Empty);
                    referer = temp[(temp.IndexOf('/') + 1)..];
                }

                HttpRequestHeader r;
                Tuple<bool, string> tuple = PathResolver.Resolve(Settings, relative_path, referer);
                if (tuple.Item1)
                {
                    r = new(req, tuple.Item2);
                    r.ClearParameters();
                    if (!string.IsNullOrEmpty(parameters))
                        r.AddParameters(parameters);

                    req.Server.BroadcastReceiver.Register(Broadcast.PUBLIC_TIME_CHANNEL, (x) => {
                        req.Server.BroadcastReceiver.RemoveSignal = removeAble ? ControlSignal.ON : ControlSignal.OFF;
                        // IN FUTURE UPDATES, I CAN USE THE OUTPUT OF THIS OPERATION TO SAVE PROCESSOR TIME BY SOME HAND-WRITTEN AI(optimization algorithm) 
                        PythonRunner.Run(r, null);
                    });
                    req.Server.BroadcastReceiverControlSignal = ControlSignal.ON;
                }
                else
                    ErrorLogger.WithTrace(Settings, string.Format("[Warn][Backend error => SetupScope()] : file specified doesn't exsist"), typeof(PythonRunner));
            });

            Scope.PushToWorkLoop = new Action<string, string>((relative_path, parameters) => action(relative_path, parameters, false));
            Scope.PushToWorkOnce = new Action<string, string>((relative_path, parameters) => action(relative_path, parameters, true));


        }





        internal void AddAndSetCurrentPath(string fullName)
        {
            DefaultSearchPaths[0] = fullName;
            Engine.SetSearchPaths(DefaultSearchPaths);
        }
    }



    public static class PythonRunner
    {
        private static readonly System.Reflection.Assembly CurrentAssembly = System.Reflection.Assembly.GetExecutingAssembly(),
                                                           MySqlAssembly = System.Reflection.Assembly.GetAssembly(typeof(MySql.Data.MySqlClient.MySqlConnection));

        public static byte[] Run(HttpRequestHeader reqh, HttpRespondHeader resh, bool isConnectionHandler = false)
        {
            IronPythonObject py = new(isConnectionHandler ? reqh.Repository.ConnectionHandler : reqh.AbsoluteFilePath);
            string parentFolder = System.IO.Directory.GetParent(reqh.AbsoluteFilePath).FullName;

            if(resh == null)
                resh = new HttpRespondHeader();

            py.Engine.Runtime.Globals.SetVariable("Server", reqh.Server);
            py.Engine.Runtime.Globals.SetVariable("Purpose", 0); // base
            py.Engine.Runtime.Globals.SetVariable("RequestHandler", reqh);
            py.Engine.Runtime.Globals.SetVariable("RespondHandler", resh);
            py.Engine.Runtime.Globals.SetVariable("Parameters", reqh.Parameters);
            py.Engine.Runtime.Globals.SetVariable("JSONParameters", reqh.Parameters.Serialize());
            py.Engine.Runtime.Globals.SetVariable("CurrentDirectory", parentFolder);
            py.Engine.Runtime.Globals.SetVariable("Repository", reqh.Repository);
            py.Engine.Runtime.LoadAssembly(CurrentAssembly);
            py.Engine.Runtime.LoadAssembly(MySqlAssembly);

            py.AddAndSetCurrentPath(parentFolder);

            py.Scope.SetVariable("RequestResult", string.Empty);
            py.Scope.SetVariable("TerminatorSignal", HttpRequestHeader.ContentSplitter);
            
            IronPythonObject.SetupScope(py.Scope, reqh.Client, reqh.Server.Settings,reqh);

            try { py.Run(); }
            catch(Exception exp)
            {
                Microsoft.Scripting.Hosting.ExceptionOperations eo = py.Engine.GetService<Microsoft.Scripting.Hosting.ExceptionOperations>();
                string error = eo.FormatException(exp);

                py.Scope.RequestResult = (bool)reqh.Server.Settings.Current.RedirectErrors ? error : py.Scope.RequestResult;

                ErrorLogger.WithTrace(reqh.Server.Settings, string.Format("[Fatel][Backend error => Run()] : hosted-script-message : {2}.\nexception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace, error), typeof(PythonRunner));
            }

            if (!isConnectionHandler)
            {
                if (resh.DidRespond())
                    return HttpRequestHeader.ContentSplitter;
                else
                {
                    Type type = py.Scope.RequestResult == null ? null : py.Scope.RequestResult?.GetType();
                    byte[] result;
                    if (type == null)
                        result = System.Text.Encoding.UTF8.GetBytes("Server has nothing to say");
                    else if (type == typeof(byte[]))
                        result = (byte[])py.Scope.RequestResult;
                    else
                        result = System.Text.Encoding.UTF8.GetBytes(py.Scope.RequestResult.ToString());

                    return result;
                }
                
            }
            else
                return null;
        }

        public static void SpecialRun(HttpRequestHeader reqh, HttpRespondHeader resh, string file)
        {
            IronPythonObject py = new(file);

            string parentFolder = System.IO.Directory.GetParent(file).FullName;

            py.Engine.Runtime.Globals.SetVariable("Server", reqh.Server);
            py.Engine.Runtime.Globals.SetVariable("Purpose", 1); // special run
            py.Engine.Runtime.Globals.SetVariable("RequestHandler", reqh);
            py.Engine.Runtime.Globals.SetVariable("RespondHandler", resh ?? new HttpRespondHeader());
            py.Engine.Runtime.Globals.SetVariable("IronPythonEngine", py.Engine);
            py.Engine.Runtime.Globals.SetVariable("CurrentDirectory", parentFolder);
            py.Engine.Runtime.Globals.SetVariable("Repository", reqh.Repository);
            py.Engine.Runtime.LoadAssembly(CurrentAssembly);

            py.AddAndSetCurrentPath(parentFolder);


            IronPythonObject.SetupScope(py.Scope, reqh.Client, reqh.Server.Settings,reqh);

            try { py.Run(); }
            catch (Exception exp)
            {
                Microsoft.Scripting.Hosting.ExceptionOperations eo = py.Engine.GetService<Microsoft.Scripting.Hosting.ExceptionOperations>();
                string error = eo.FormatException(exp);

                py.Scope.RequestResult = (bool)reqh.Server.Settings.Current.RedirectErrors ? error : py.Scope.RequestResult;

                ErrorLogger.WithTrace(reqh.Server.Settings, string.Format("[Fatel][Backend error => SpecialRun()] : hosted-script-message : {2}.\nexception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace, error), typeof(PythonRunner));
            }

        }

        internal static IronPythonObject InitializeForWebsocket(HttpRequestHeader req)
        {
            IronPythonObject py = new(req.AbsoluteFilePath);

            string parentFolder = System.IO.Directory.GetParent(req.AbsoluteFilePath).FullName;

            py.Engine.Runtime.Globals.SetVariable("Server", req.Server);
            //py.Engine.Runtime.Globals.SetVariable("Client", req.Client);
            py.Engine.Runtime.Globals.SetVariable("Purpose", 2); // websocket
            py.Engine.Runtime.Globals.SetVariable("RequestHandler", req);
            py.Engine.Runtime.Globals.SetVariable("RespondHandler", new HttpRespondHeader());
            py.Engine.Runtime.Globals.SetVariable("IronPythonEngine", py.Engine);
            py.Engine.Runtime.Globals.SetVariable("CurrentDirectory", parentFolder); 
            py.Engine.Runtime.Globals.SetVariable("Repository", req.Repository);
            py.Engine.Runtime.LoadAssembly(CurrentAssembly);

            

            py.AddAndSetCurrentPath(parentFolder);


            IronPythonObject.SetupScope(py.Scope, req.Client, req.Server.Settings,req);

            

            return py;
        }
        internal static void WebsocketRun(IronPythonObject py, byte[] message, int opcode, string lastMessage)
        {
            py.Scope.SetVariable("Opcode", opcode);
            py.Scope.SetVariable("Message", message);
            py.Scope.SetVariable("LastMessage", lastMessage);

            try { py.Run(); }
            catch (Exception exp)
            {
                Microsoft.Scripting.Hosting.ExceptionOperations eo = py.Engine.GetService<Microsoft.Scripting.Hosting.ExceptionOperations>();
                string error = eo.FormatException(exp);

                ErrorLogger.WithTrace(py.Engine.Runtime.Globals.GetVariable("RequestHandler").Server.Settings, string.Format("[Fatel][Backend error => WebsocketRun()] : hosted-script-message : {2}.\nexception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace, error), typeof(PythonRunner));
            }
        }

        public static void ServerEventRun(HttpRequestHeader reqh)
        {
            IronPythonObject py = new(reqh.AbsoluteFilePath);

            string parentFolder = System.IO.Directory.GetParent(reqh.AbsoluteFilePath).FullName;

            py.Engine.Runtime.Globals.SetVariable("Server", reqh.Server);
            py.Engine.Runtime.Globals.SetVariable("Purpose", 3); // server-event run
            py.Engine.Runtime.Globals.SetVariable("RequestHandler", reqh);
            py.Engine.Runtime.Globals.SetVariable("RespondHandler", new HttpRespondHeader());
            py.Engine.Runtime.Globals.SetVariable("IronPythonEngine", py.Engine);
            py.Engine.Runtime.Globals.SetVariable("CurrentDirectory", parentFolder);
            py.Engine.Runtime.Globals.SetVariable("Repository", reqh.Repository);
            py.Engine.Runtime.LoadAssembly(CurrentAssembly);

            py.AddAndSetCurrentPath(parentFolder);


            IronPythonObject.SetupScope(py.Scope, reqh.Client, reqh.Server.Settings,reqh);

            try { py.Run(); }
            catch (Exception exp)
            {
                Microsoft.Scripting.Hosting.ExceptionOperations eo = py.Engine.GetService<Microsoft.Scripting.Hosting.ExceptionOperations>();
                string error = eo.FormatException(exp);

                py.Scope.RequestResult = (bool)reqh.Server.Settings.Current.RedirectErrors ? error : py.Scope.RequestResult;

                ErrorLogger.WithTrace(reqh.Server.Settings, string.Format("[Fatel][Backend error => SpecialRun()] : hosted-script-message : {2}.\nexception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace, error), typeof(PythonRunner));
            }

        }

        internal static void RunStartups(BaseServer baseServer, Rule repo)
        {
            IronPythonObject py = new(repo.StartupFile);

            string parentFolder = System.IO.Directory.GetParent(repo.StartupFile).FullName;

            py.Engine.Runtime.Globals.SetVariable("Server", baseServer);
            py.Engine.Runtime.Globals.SetVariable("Purpose", 3); // server-startup run
            py.Engine.Runtime.Globals.SetVariable("RequestHandler", new object());
            py.Engine.Runtime.Globals.SetVariable("RespondHandler", new object());
            py.Engine.Runtime.Globals.SetVariable("IronPythonEngine", py.Engine);
            py.Engine.Runtime.Globals.SetVariable("CurrentDirectory", parentFolder);
            py.Engine.Runtime.Globals.SetVariable("Repository", repo);
            py.Engine.Runtime.LoadAssembly(CurrentAssembly);

            py.AddAndSetCurrentPath(parentFolder);


            IronPythonObject.SetupScope(py.Scope, null, baseServer.Settings);

            try { py.Run(); }
            catch (Exception exp)
            {
                Microsoft.Scripting.Hosting.ExceptionOperations eo = py.Engine.GetService<Microsoft.Scripting.Hosting.ExceptionOperations>();
                string error = eo.FormatException(exp);

                py.Scope.RequestResult = (bool)baseServer.Settings.Current.RedirectErrors ? error : py.Scope.RequestResult;

                ErrorLogger.WithTrace(baseServer.Settings, string.Format("[Fatel][Backend error => SpecialRun()] : hosted-script-message : {2}.\nexception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace, error), typeof(PythonRunner));
            }
        }
    }
}


/*  The base imp
 

public static byte[] Run(HttpRequestHeader reqh, HttpRespondHeader resh, bool isConnectionHandler = false)
        {

            IronPythonObject py = new IronPythonObject(isConnectionHandler ? reqh.Repository.ConnectionHandler : reqh.AbsoluteFilePath);

            string parentFolder = System.IO.Directory.GetParent(reqh.AbsoluteFilePath).FullName;

            py.Engine.Runtime.Globals.SetVariable("Server", reqh.Server);
            py.Engine.Runtime.Globals.SetVariable("RequestHandler", reqh);
            py.Engine.Runtime.Globals.SetVariable("RespondHandler", resh ?? new HttpRespondHeader());
            py.Engine.Runtime.Globals.SetVariable("Parameters", reqh.Parameters);
            py.Engine.Runtime.Globals.SetVariable("JSONParameters", reqh.Parameters.Serialize());
            py.Engine.Runtime.Globals.SetVariable("CurrentDirectory", parentFolder);
            py.Engine.Runtime.LoadAssembly(CurrentAssembly);

            py.AddAndSetCurrentPath(parentFolder);

            py.Scope.SetVariable("RequestResult", string.Empty);
            py.Scope.SetVariable("TerminatorSignal", HttpRequestHeader.ContentSplitter);

            IronPythonObject.SetupScope(py.Scope, reqh.Client, reqh.Server.Settings);

            try { py.Run(); }
            catch(Exception exp)
            {
                Microsoft.Scripting.Hosting.ExceptionOperations eo = py.Engine.GetService<Microsoft.Scripting.Hosting.ExceptionOperations>();
                string error = eo.FormatException(exp);

                py.Scope.RequestResult = (bool)reqh.Server.Settings.Current.RedirectErrors ? error : py.Scope.RequestResult;

                ErrorLogger.WithTrace(reqh.Server.Settings, string.Format("[Fatel][Backend error => Run()] : hosted-script-message : {2}.\nexception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace, error), typeof(PythonRunner));
            }

            if (!isConnectionHandler)
            {
                if (resh.DidRespond()) return HttpRequestHeader.ContentSplitter;
                else
                {
                    //py.Scope.RequestResult
                    Type type = py.Scope.RequestResult == null ? null : py.Scope.RequestResult?.GetType();
                    byte[] result;
                    if (type == null)
                    {
                        result = System.Text.Encoding.UTF8.GetBytes("Server has nothing to say");
                    }
                    else if (type == typeof(byte[]))
                        result = (byte[])py.Scope.RequestResult;
                    else
                        result = System.Text.Encoding.UTF8.GetBytes(py.Scope.RequestResult.ToString());

                    return result;
                }
                
            }
            else
                return null;
            //Console.WriteLine("RequestResult-Type is '{0}'", );
            //Console.WriteLine(py.Scope.GetVariable("RequestResult"));
        }
 
 */