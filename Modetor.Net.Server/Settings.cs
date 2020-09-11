using System;
using System.Linq;

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
                    {
                        Repositories = parts[1].Split(',');
                        foreach (string repositoryRule in Repositories)
                        {
                            string ruleFile = ResourcePath+ $"{repositoryRule}{System.IO.Path.DirectorySeparatorChar}.rules";
                            if (!System.IO.File.Exists(ruleFile))
                            {
                                ErrorLogger.Print("[Settings] : Repository '"+repositoryRule+"' has no .rules file which might cause issues");
                                continue;
                            }
                            AppendRule(repositoryRule, ruleFile);
                        }
                    }
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
                    else if (parts[0].StartsWith("connections-handler-repos"))
                    {
                        if (parts[1].Equals("*"))
                            ConnectionsHandlerRepositories = Repositories;
                        else if(parts[1].Equals(string.Empty) || !Repositories.Contains(parts[1]))
                        {
                            ErrorLogger.Print("[Settings] : Value error in settings.ini at line '" + line + "'. value expected to be repo,repo1,repo2... or *");
                            return false;
                        }
                        else
                        {
                            ConnectionsHandlerRepositories = parts[1].Split(',');
                            for (int i = ConnectionsHandlerRepositories.Length - 1; i > -1; i--)
                                ConnectionsHandlerRepositories[i] = ConnectionsHandlerRepositories[i].Trim();
                        }
                    }
                    else if (parts[0].StartsWith("connections-handler"))
                        ConnectionsHandler = BasePath + FilePath.Build(parts[1]);
                    else if (parts[0].StartsWith("main-page"))
                        MainPage = BasePath + FilePath.Build(parts[1]);

                    //
                }
                if(MainPage == null)
                {
                    ErrorLogger.Print("[Settings] : settings.ini must contains main-page property");
                    return false;
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

        private static void AppendRule(string repositoryRule, string ruleFile)
        {
            Rule rule = new Rule();
            foreach (string line in System.IO.File.ReadLines(ruleFile))
            {
                if (line == null || line.Equals(string.Empty) || !line.Contains(':')) continue;
                string[] parts = line.Split(':');
                if(parts.Length != 2)
                {
                    ErrorLogger.Print($"[Settings] : Property error in repository({repositoryRule}) rule file at line '{line}'.\nProperty ignored but it might causes bugs");
                    continue;
                }
                parts[0] = parts[0].Trim();
                parts[1] = parts[1].Trim();

                if(parts[0].StartsWith("home"))
                    rule.SetHome(FilePath.Build(parts[1]));
                else if (parts[0].StartsWith("fnf"))
                    rule.SetFNF(FilePath.Build(parts[1]));

            }

            RepositoriesRules.Add(repositoryRule, rule);
        }


        /**\
        **** 
        ****  Static members
        ****
        \**/

        public static string[] Repositories { get; private set; } = null;
        public static System.Collections.Generic.Dictionary<string, Rule> RepositoriesRules = new System.Collections.Generic.Dictionary<string, Rule>();
        public static int ThreadMechanism { get; private set; } = 2;
        public static bool AllowOutput { get; private set; } = true;
        public static bool AllowNetworkConnections { get; private set; } = false;
        public static bool AllowSmartSwitch { get; private set; } = false;
        public static string ConnectionsHandler { get; private set; } = null;
        public static string[] ConnectionsHandlerRepositories { get; private set; } = null;
        public static string MainPage { get; private set; } = null;

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


    public struct Rule
    {
        public void SetName(string n) => Name = n;
        public void SetHome(string h) => HomeFile = h;
        public void SetFNF(string fnf) => FNFFile = fnf;
        public string Name { get; private set; } 
        public string HomeFile { get; private set; } 
        public string FNFFile { get; private set; }
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
