/*\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
III
III          بسم الله الرحمن الرحيم
III 
\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\*/






using System;
using System.Linq;
using System.Reflection;

namespace Modetor.Net.Server.Core.Backbone
{
    public class Settings
    {
        public static string SourcePath { get; private set; }
        public static void SetSource(dynamic p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));

            SourcePath = PathResolver.Build((string)p.Source);
            if (!SourcePath[SourcePath.Length - 1].Equals(System.IO.Path.DirectorySeparatorChar))
                SourcePath += System.IO.Path.DirectorySeparatorChar;
            SettingsFilePath = SourcePath + FilePath.Build("base", "settings.json");
            BasePath = SourcePath + $"base{System.IO.Path.DirectorySeparatorChar}";
            RootPath = BasePath + $"root{System.IO.Path.DirectorySeparatorChar}";
            ResourcePath = BasePath + $"res{System.IO.Path.DirectorySeparatorChar}";
            FeatureSetPath = p.FeaturesSet == null ? null : (string)p.FeaturesSet;
            Features = new System.Collections.Generic.Dictionary<string, Feature>();
            if (FeatureSetPath != null)
            {
                ProcessorArchitecture arch = Assembly.GetExecutingAssembly().GetName().ProcessorArchitecture;
                if(arch == ProcessorArchitecture.MSIL)
                {
                    System.Runtime.InteropServices.Architecture osarch = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture;
                    arch = osarch == System.Runtime.InteropServices.Architecture.Arm ? ProcessorArchitecture.Arm :
                           osarch == System.Runtime.InteropServices.Architecture.X64 ? ProcessorArchitecture.Amd64 :
                           osarch == System.Runtime.InteropServices.Architecture.X86 ? ProcessorArchitecture.X86 : ProcessorArchitecture.X86;
                }
                dynamic features = Newtonsoft.Json.JsonConvert.DeserializeObject(System.IO.File.ReadAllText(FeatureSetPath + "config.json"));
                foreach(dynamic feature in features)
                {
                    ProcessorArchitecture featureArch = GetArchitecture(((string)feature.Architecture).Trim(), arch);
                    if (!arch.Equals(featureArch))
                    {
                        if (arch == ProcessorArchitecture.Amd64 && featureArch == ProcessorArchitecture.X86);
                        else if ((arch == ProcessorArchitecture.Amd64 || arch == ProcessorArchitecture.X86) && Environment.Is64BitProcess);
                        else
                        {
                            ErrorLogger.Warn("[Settings] : Feature '" + (string)feature.Name + "' Unknown architecture '" + arch + "', feature dropped >> "+featureArch.ToString()  );
                            continue;
                        }
                    }
                    

                    Feature f = new()
                    {
                        Name = (string)feature.Name,
                        Path = FeatureSetPath + (string)feature.Name + System.IO.Path.DirectorySeparatorChar + (  
                                                    featureArch == ProcessorArchitecture.Amd64 ? "x64"+ System.IO.Path.DirectorySeparatorChar : 
                                                    featureArch == ProcessorArchitecture.X86 ? "x86"+ System.IO.Path.DirectorySeparatorChar : 
                                                    featureArch == ProcessorArchitecture.Arm ? "arm"+ System.IO.Path.DirectorySeparatorChar : null
                                                ),
                        TargetName = (string)feature.Target,
                        RequireServerIOFocus = (bool)feature.RequireServerIOFocus,
                        WaitForExit = (bool)feature.WaitForExit
                    };

                    f.TargetExcutable = f.Path + f.TargetName;

                    if(!System.IO.File.Exists(f.TargetExcutable) || f.Path == null)
                    {
                        ErrorLogger.Warn("[Settings] : Feature '" + (string)feature.Name + "' Target file not found, won't be available");
                        continue;
                    }
                    if (!(bool)feature.Enabled) continue;
                    if (Features.ContainsKey(f.Name))
                    {
                        ErrorLogger.Warn("[Settings] : Feature '" + (string)feature.Name + "' already defined. duplicates ignored");
                        continue;
                    }

                    Features.Add(f.Name, f);
                }

                if(!Features.ContainsKey("cpython"))
                {
                    throw new Exception("Cannot initialize feature-set without cpython");
                }
            }
        }

        private static ProcessorArchitecture GetArchitecture(string arc, ProcessorArchitecture hostArch)
        {
            return arc.Equals("x64") ? ProcessorArchitecture.Amd64 : 
                   arc.Equals("x86") ? ProcessorArchitecture.X86 : 
                   arc.Equals("x86_64") ?
                            System.Runtime.InteropServices.RuntimeInformation.OSArchitecture == System.Runtime.InteropServices.Architecture.X64 
                            && hostArch == ProcessorArchitecture.Amd64 ? ProcessorArchitecture.Amd64 : ProcessorArchitecture.X86 :
                   arc.Equals("arm") ? 
                   ProcessorArchitecture.Arm : ProcessorArchitecture.None;

        }

        /// <summary>
        ///     Reads the settings.ini file located in subfolder /base
        /// </summary>
        /// <returns>true : succeeded, false : failed</returns>
        /// <exception cref="InitializationException">thrown if .ini file contains invalid properies</exception>
        /// <exception cref="InvalidPropertyValue">throw if a property's value isn't valid</exception>
        /// <example>try{Settings.Read()}catch(Exception exp){/*...*/}</example>
        public Settings()
        {
            LoadSettings();
        }
        public void Update() => LoadSettings();
        private void LoadSettings()
        {
            try
            {
                string lines = System.IO.File.ReadAllText(SettingsFilePath, System.Text.Encoding.UTF8);
                if (string.IsNullOrEmpty(lines))
                {
                    throw new System.IO.IOException("failed to read file:" + SettingsFilePath);
                }

                Current = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(lines);

                if (Current.MainPage == null)
                {
                    throw new InvalidPropertyValue("[Settings] : settings.json must contains main-page property");
                }

                if (Current.FileNotFoundPage == null)
                {
                    throw new InvalidPropertyValue("[Settings] : settings.json must contains fnf-page property");
                }

                if (Current.WebSocketIdelChances == null || Current.WebSocketIdelChances == null)
                {
                    throw new InvalidPropertyValue("[Settings] : settings.json must contains WebSocketIdleTimeout and WebSocketIdelChances");
                }

                Current.MainPage = BasePath + ((string)Current.MainPage).Replace('/', System.IO.Path.DirectorySeparatorChar);
                Current.FileNotFoundPage = BasePath + ((string)Current.FileNotFoundPage).Replace('/', System.IO.Path.DirectorySeparatorChar);
                RepositoriesRules?.Clear();

                foreach (string repositoryRule in Current.Repos)
                {
                    string ruleFile = ResourcePath + $"{repositoryRule}{System.IO.Path.DirectorySeparatorChar}.rules";
                    if (!System.IO.File.Exists(ruleFile))
                    {
                        ErrorLogger.Warn("[Settings] : Repository '" + repositoryRule + "' has no .rules file which might cause issues, won't be available");
                        continue;
                    }
                    AppendRule(repositoryRule, ruleFile);
                }

                

                if((bool)Current.AllowSocketControlFlow && Current.SocketControlFlow != null)
                {
                    if (SocketControlFlow.Count > 0)
                    {
                        SocketControlFlow.Clear();
                    }

                    foreach (dynamic item in Current.SocketControlFlow)
                    {
                        if (!(bool)item.Enabled)
                        {
                            continue;
                        }

                        SocketControlFlow controlFlow = new SocketControlFlow();
                        if (controlFlow.Setup((string)item.Filter, BasePath + ((string)item.Target).Replace('/', System.IO.Path.DirectorySeparatorChar)))
                        {
                            SocketControlFlow.Add(controlFlow);
                        }
                    }
                }

                ReceiveTimeout = (Current?.ReceiveTimeout != null) ? (int)Current?.ReceiveTimeout : System.Threading.Timeout.Infinite;
                SendTimeout = (Current.SendTimeout != null) ? (int)Current.SendTimeout : System.Threading.Timeout.Infinite;



                IsReady = true;
            }
            catch (Exception exp)
            {
                if (Current == null)
                {
                    ErrorLogger.WithTrace(string.Format("[Warning][Server error => LoadSettings()] : exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), GetType());
                }
                else
                {
                    ErrorLogger.WithTrace(this, string.Format("[Warning][Server error => LoadSettings()] : exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), GetType());
                }

                IsReady = false;
            }
        }

        private void AppendRule(string repositoryRule, string ruleFile)
        {
            Rule rule = new Rule(repositoryRule);
            rule.SetPath(ResourcePath + $"{repositoryRule}{System.IO.Path.DirectorySeparatorChar}");
            foreach (string line in System.IO.File.ReadLines(ruleFile))
            {
                if (string.IsNullOrEmpty(line) || !line.Contains(':') || line.StartsWith("--") || line.StartsWith('#'))
                {
                    continue;
                }

                string[] parts = line.Split(':', 2, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToArray();
                if(parts.Length != 2)
                {
                    if ((bool)Current.AllowOutput)
                    {
                        ErrorLogger.Error($"[Settings] : Property error in repository({repositoryRule}) rule file at line '{line}'.\nProperty ignored but it might causes bugs");
                    }

                    continue;
                }

                if(parts[0].StartsWith("home"))
                {
                    rule.SetHome(parts[1]);
                }
                else if (parts[0].StartsWith("connection-handler"))
                {
                    rule.SetConnectionHandler(parts[1]);
                }
                else if(parts[0].StartsWith("upload-directory"))
                {
                    rule.SetUploadDirectory(parts[1]);
                }
                else if (parts[0].StartsWith("shared-resources"))
                {
                    rule.SetAsSharedResources("true".Equals(parts[1]));
                }
                else if (parts[0].StartsWith("available"))
                {
                    rule.SetAvailable("true".Equals(parts[1]));
                }
                else if (parts[0].StartsWith("private-directories"))
                {
                    rule.SetPrivateDirectories(parts[1].Split(',').Select(a => a.Trim()).ToArray());
                }
                else if (parts[0].StartsWith("exclusive-for-server"))
                {
                    rule.SetExclusiveForServer(parts[1]);
                }
                else if (parts[0].StartsWith("allow-websocket-protocol"))
                {
                    rule.SetAllowWebsocket(parts[1].Equals("true"));
                }
                else if (parts[0].StartsWith("websocket-idle-timeout"))
                {
                    rule.SetWebSocketIdelTimeout(int.Parse(parts[1]));
                }
                else if (parts[0].StartsWith("websocket-idle-chances"))
                {
                    rule.SetWebSocketIdelChances(int.Parse(parts[1]));
                }
                else if (parts[0].StartsWith("allow-server-event"))
                {
                    rule.SetAllowServerEvent(parts[1].Equals("true"));
                }
                else if (parts[0].StartsWith("server-event-method"))
                {
                    rule.SetServerEventMethod(parts[1].ToLower().Equals("push") ? ServerEventMethod.PUSH : ServerEventMethod.LOOP);
                }
                else if (parts[0].StartsWith("startup"))
                {
                    rule.SetStartupFile(parts[1]);
                }
                else if (parts[0].StartsWith("allow-cross-repo-requests"))
                {
                    rule.SetAllowCrossRepositoriesRequests(parts[1].Equals("true"));
                }
            }

            try
            {
                rule.Verify();
                if (rule.WebSocketIdelChances == -1)
                {
                    rule.SetWebSocketIdelChances((int)Current.WebSocketIdelChances);
                }

                if (rule.WebSocketIdelTimeout == -1)
                {
                    rule.SetWebSocketIdelTimeout((int)Current.WebSocketIdelChances);
                }

                RepositoriesRules.Add(repositoryRule, rule);
            }
            catch (Exception exp)
            {
                ErrorLogger.WithTrace(string.Format("[Warning][Server error => Verify()] : exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), typeof(Settings));
            }
        }


        /**\
        **** 
        ****  Static members
        ****
        \**/
        public int ReceiveTimeout = 15000; // 15s
        public int SendTimeout = 10000; // 10s
        public bool IsReady = false;
        public dynamic Current = null;
        public string[] Repositories => RepositoriesRules.Keys.ToArray();
        public System.Collections.Generic.Dictionary<string, Rule> RepositoriesRules = new System.Collections.Generic.Dictionary<string, Rule>();
        public bool GetRepositoryByPath(string path, out Rule rule)
        {
            
            foreach (System.Collections.Generic.KeyValuePair< string, Rule> item in RepositoriesRules)
            {
                if (path.StartsWith(item.Value.Path))
                {
                    rule = item.Value;
                    return true;
                }
            }

            rule = Rule.Corrupted;
            return false;
        }
        public System.Collections.Generic.List<SocketControlFlow> SocketControlFlow = new System.Collections.Generic.List<SocketControlFlow>();
        //public 
        /**\
        **** 
        ****  Readonly Static members
        ****
        \**/

        public static string SettingsFilePath;// = SourcePath + FilePath.Build("base", "settings.json");
        public static string BasePath;// = SourcePath + $"base{System.IO.Path.DirectorySeparatorChar}";
        public static string RootPath;// = BasePath + $"root{System.IO.Path.DirectorySeparatorChar}";
        public static string ResourcePath;// = BasePath + $"res{System.IO.Path.DirectorySeparatorChar}";
        public static string FeatureSetPath;
        public static System.Collections.Generic.Dictionary<string, Feature> Features;
    }



    public struct SocketControlFlow
    {
        private const string chr = "*";

        //public bool Enabled { get; private set; }
        public bool AnyIP { get; private set; }
        public bool AnyPort { get; private set; }
        public string IP { get; private set; }
        public string Target { get; private set; }
        public int Port { get; private set; }
        public bool Setup(string filter, string target)
        {
            Target = target;
            string[] p = filter.Split(':', 2, StringSplitOptions.RemoveEmptyEntries).Select( a => a.Trim()).ToArray();

            if (p.Length != 2)
            {
                return false;
            }

            if (p[0].Equals(chr) && p[1].Equals(chr))
            {
                throw new NotSupportedException("[SocketControlFlow] cannot have both AnyIP and AnyPort set to true");
            }
            else if (p[0].Equals(chr))
            {
                IP = chr;
                Port = int.Parse(p[1]);
                AnyIP = true;
            }
            else if (p[1].Equals(chr))
            {
                IP = p[0];
                Port = -1;
                AnyPort = true;
            }

            return true;
        }

        internal bool IsMatch(string address, int port)
        {
            if (AnyIP && port == Port)
            {
                return true;
            }
            else if(AnyPort && IP.Equals(address))
            {
                return true;
            }

            return false;
        }
    }
    
}
