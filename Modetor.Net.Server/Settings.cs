using System;
namespace Modetor.Net.Server
{
    class Settings
    {
        public static readonly string Path = string.Format(AppDomain.CurrentDomain.BaseDirectory + "base{0}settings.ini", System.IO.Path.DirectorySeparatorChar);

        public static bool Read()
        {
            try
            {
                string[] lines = System.IO.File.ReadAllLines(Path, System.Text.Encoding.UTF8);
                if (lines == null) throw new System.IO.IOException("failed to read file:" + Path);

                foreach (string line in lines)
                {
                    if (line.StartsWith("#")) continue;
                    string[] parts = line.Split("=");
                    if (parts.Length != 2) continue; /*Ignore invalid syntax*/
                    parts[0] = parts[0].Trim();
                    parts[1] = parts[1].Trim();
                    if (string.Empty.Equals(parts[1]) || string.Empty.Equals(parts[0])) continue;
                    if (parts[0].StartsWith("repo"))
                        Repositories = parts[1].Split(',');
                    else if (parts[0].StartsWith("allow-output"))
                        AllowOutput = "true".Equals(parts[1]);
                    else if (parts[0].StartsWith("thread-mechanism"))
                    {
                        int val = int.TryParse(parts[1], out val) ? val : 2;
                        
                        if (val < 0 || val > 2)
                        {
                            Server.GetServer().Logger.Print("[Settings] : Value error in settings.ini at line '"+line+"'. value expected to be 0/1/2");
                            return false;
                        }
                        ThreadMechanism = val; 
                    }
                }

                
                return true;
            }
            catch (Exception exp)
            {
                Server.GetServer().Logger.Log(exp);
                Server.GetServer().Logger.Print("[Settings] : Exception thrown while reading settings.ini");
                return false;
            }
        }


        public static string[] Repositories { get; private set; } = null;
        public static int ThreadMechanism { get; private set; } = 2;
        public static bool AllowOutput { get; private set; } = true;
    }
}
