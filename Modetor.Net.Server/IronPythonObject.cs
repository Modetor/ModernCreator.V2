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
        public System.Collections.Generic.List<string> DefaultSearchPaths = new System.Collections.Generic.List<string>()
        {
            ".", 
            System.AppDomain.CurrentDomain.BaseDirectory+@"res\ipy\Lib" , 
            System.AppDomain.CurrentDomain.BaseDirectory+@"res\ipy\DLLs" , 
            System.AppDomain.CurrentDomain.BaseDirectory+@"res\ipy\Lib\site-packages"

        };
        public dynamic Engine { get; private set; }
        public dynamic Scope { get; private set; }
        private string scriptfile = null;
    }
}
