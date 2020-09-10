using System;

namespace Modetor.Net.Server
{
    class Settings
    {
        

        /// <summary>
        ///     Reads the settings.ini file located in subfolder /base
        /// </summary>
        /// <returns>true:succeeded, false:failed</returns>
        public static bool Read()
        {
            try
            {
                string[] lines = System.IO.File.ReadAllLines(SettingsFilePath, System.Text.Encoding.UTF8);
                if (lines == null) throw new System.IO.IOException("failed to read file:" + SettingsFilePath);

                foreach (string line in lines)
                {
                    if (line.StartsWith("#")) continue;
                    string[] parts = line.Split("=");
                    if (parts.Length != 2) continue; /*Ignore invalid syntax*/
                    parts[0] = parts[0].Trim();
                    parts[1] = parts[1].Trim();
                    if (string.Empty.Equals(parts[1]) || string.Empty.Equals(parts[0])) continue;
                    if (parts[0].StartsWith("repos"))
                        Repositories = parts[1].Split(',');
                    else if (parts[0].StartsWith("allow-output"))
                        AllowOutput = "true".Equals(parts[1]);
                    else if (parts[0].StartsWith("thread-mechanism"))
                    {
                        int val = 2;
                        int.TryParse(parts[1], out val);

                        if (val < 0 || val > 2)
                        {
                            ErrorLogger.Print("[Settings] : Value error in settings.ini at line '" + line + "'. value expected to be 0/1/2");
                            return false;
                        }
                        ThreadMechanism = val;
                    }

                    else if (parts[0].StartsWith("connections-handler"))
                        ConnectionsHandler = BasePath + FilePath.Build(parts[1]);
                    else if (parts[0].StartsWith("connections-handler-repos"))
                    {
                        if (parts[1].Equals("*"))
                            ConnectionsHandlerRepositories = Repositories;
                        else if(parts[1].Contains(',')) {
                            ConnectionsHandlerRepositories = parts[1].Split(',');
                            for (int i = ConnectionsHandlerRepositories.Length - 1; i > -1; i--)
                                ConnectionsHandlerRepositories[i] = ConnectionsHandlerRepositories[i].Trim();
                        }
                        else {
                            ErrorLogger.Print("[Settings] : Value error in settings.ini at line '" + line + "'. value expected to be repo,repo1,repo2... or *");
                            return false;
                        }
                    }
                }

                //Console.WriteLine(System.IO.File.Exists(ConnectionsHandler));
                return true;
            }
            catch (Exception exp)
            {
                Server.GetServer().Logger.Log(exp);
                ErrorLogger.Print("[Settings] : Exception thrown while reading settings.ini");
                return false;
            }
        }


        /**\
        **** 
        ****  Static members
        ****
        \**/

        public static string[] Repositories { get; private set; } = null;
        public static int ThreadMechanism { get; private set; } = 2;
        public static bool AllowOutput { get; private set; } = true;
        public static bool AllowNetworkConnections { get; private set; } = false;
        public static bool AllowSmartSwitch { get; private set; } = false;
        public static string ConnectionsHandler { get; private set; } = null;
        public static string[] ConnectionsHandlerRepositories { get; private set; } = null;


        /**\
        **** 
        ****  Readonly Static members
        ****
        \**/

        public static readonly string SettingsFilePath = string.Format(AppDomain.CurrentDomain.BaseDirectory + "base{0}settings.ini", System.IO.Path.DirectorySeparatorChar);
        public static readonly string BasePath = AppDomain.CurrentDomain.BaseDirectory + $"base{System.IO.Path.DirectorySeparatorChar}";
        public static readonly string RootPath = BasePath + $"root{System.IO.Path.DirectorySeparatorChar}";
        public static readonly string ResourcePath = BasePath + $"res{System.IO.Path.DirectorySeparatorChar}";

    }



    public static class FilePath
    {
        /// <summary>
        ///  Use '/' as you default dir-separator and it will replaced with platform's one
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string Build(string text) => text.Replace('/', System.IO.Path.DirectorySeparatorChar);
        /// <summary>
        /// put the path as Build("Parent","Son Folder","A") 
        /// to generate something "Parent/Son Folder/A" or "Parent\Son Folder\A"
        /// based on the plateform
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string Build(params string[] text)
        {
            string path = "";
            foreach (string part in text)
                path += $"{part}{System.IO.Path.DirectorySeparatorChar}";

            return path.Substring(0, path.Length-1);
        }
    }
}
