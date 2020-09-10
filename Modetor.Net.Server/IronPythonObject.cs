namespace Modetor.Net.Server
{
    /// <summary>
    /// 
    /// </summary>
    class IronPythonObject
    {
        public static IronPythonObject GetObject(string file) => new IronPythonObject(file);
        internal IronPythonObject(string file)
        {
            scriptfile = file;
            Engine = IronPython.Hosting.Python.CreateEngine();
            Scope = Engine.CreateScope();
        }
        public void SetSearchPaths(string[] p) => Engine.SetSearchPaths(p);
        public void AddPath(string path) => DefaultSearchPaths.Add(path);
        public string Run()
        {
            string result;
            try { Engine.ExecuteFile(scriptfile, Scope); result = null; }
            catch(System.Exception exp) {
                Microsoft.Scripting.Hosting.ExceptionOperations eo = Engine.GetService<Microsoft.Scripting.Hosting.ExceptionOperations>();
                string error = eo.FormatException(exp);
                result = error;
                ErrorLogger.Print("[IronPythonObject] : Error in '"+scriptfile.Substring(scriptfile.IndexOf(System.IO.Path.DirectorySeparatorChar)) + "' message "+error);
            }
            return result;
        }
        public readonly System.Collections.Generic.List<string> DefaultSearchPaths = new System.Collections.Generic.List<string>()
        {
            ".",
            System.AppDomain.CurrentDomain.BaseDirectory+FilePath.Build("res/ipy/Lib"),
            System.AppDomain.CurrentDomain.BaseDirectory+FilePath.Build("res/ipy/DLLs") ,
            System.AppDomain.CurrentDomain.BaseDirectory+FilePath.Build("res/ipy/Lib/site-packages")
        };
        public dynamic Engine { get; private set; }
        public dynamic Scope { get; private set; }
        private string scriptfile = null;










        public static void SetupScope(dynamic Scope, System.Net.Sockets.TcpClient client, HeaderKeys hk)
        {
            Scope.ServerResult = "Server has nothing to say";
            Scope.ServerRequest = hk;
            Scope.Client = client;
            Scope.ClientAddress = client.Client.RemoteEndPoint.ToString();
            Scope.Stream = client.GetStream();

            Scope.Send = new System.Action<string>(async (text) =>
            {
                await client.GetStream().WriteAsync(System.Text.Encoding.UTF8.GetBytes(text));
                await client.GetStream().FlushAsync();
            });
            Scope.SendNow = new System.Action<string>((text) =>
            {
                client.GetStream().Write(System.Text.Encoding.UTF8.GetBytes(text));
                client.GetStream().Flush();
            });
            Scope.Close = new System.Action(() => client.Close());
            Scope.
        }
    }
}
