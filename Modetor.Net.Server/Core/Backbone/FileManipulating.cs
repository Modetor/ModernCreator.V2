using System;
using System.IO;
using System.Linq;

namespace Modetor.Net.Server.Core.Backbone
{
    public static class PathResolver
    {
        private const char Slash = '/';
        private static readonly string EXE_PATH = System.Reflection.Assembly.GetEntryAssembly().Location;

        public static Tuple<bool, string> Resolve(Settings Settings, string target, string referrer)
        {
            System.Diagnostics.Contracts.Contract.Requires(target != null, "[PathResolver].Resolve(..,target,..) is null");

            bool state = Resolve(Settings,target, referrer, out string result);
            Tuple<bool, string> tuple = new(state, result);
            return tuple;
        }

        public static string Build(string v)
        {
            if (File.Exists(v) || Directory.Exists(v)) return v.Contains(Slash) ? v.Replace(Slash, Path.DirectorySeparatorChar) : v;

            string[] parts = v.Split(Slash);
            string p = new DirectoryInfo(EXE_PATH).Parent.FullName;
            string r = string.Empty;
            for(int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Equals(".."))
                    p = new DirectoryInfo(p).Parent.FullName;
                else
                    r += parts[i] + Slash;
            }
            p = p + Slash + r;
            p = p[0..p.LastIndexOf(Slash)].Replace(Slash, Path.DirectorySeparatorChar);// .Substring(0, p.Length - 1)
            return ( Directory.Exists(p) || File.Exists(p) ) ? p : null;
        }

        private static void FixHttpUriError(ref string target)
        {
            string[] temp = target.Split(Slash);
            System.Collections.Generic.List<string> ls = new();

            for(int i = 0; i < temp.Length; i++)
            {
                if (SearchDuplicates(temp, temp[i], i + 1))
                    continue;
                else
                    ls.Add(temp[i]);
            }

            target = string.Join(Slash, ls.ToArray());
        }

        private static bool SearchDuplicates(string[] temp, string sample, int pos)
        {
            return pos < temp.Length && sample.Equals(temp[pos]);
        }

        public static bool Resolve(Settings Settings,string target, string referrer, out string resultfile)
        {
            target = target.Replace(Slash, Path.DirectorySeparatorChar);
            referrer = referrer.Replace(Slash, Path.DirectorySeparatorChar);
            
            //Console.WriteLine("Target : {0}",target);
            //if(target == null)
            //{
            //    target = Settings.Current.MainPage;
            //    ErrorLogger.Error("[PathResolver] : arg0(target) is null, used Settings.MainPage as a fallback target");
            //}

            string file = string.Empty;

            string[] names = Settings.Repositories;
            char separator = Path.DirectorySeparatorChar;
            //target = target.Replace('/', separator);

            if (target.Equals(separator))
            {
                target = string.Empty;
            }

            if (target.StartsWith(separator))
            {
                target = target[1..];
            }

            if (target.EndsWith(separator))
            {
                target = target[0..^1];
            }

            if (referrer.Equals(string.Empty))
            {
                if (target.Equals(string.Empty))
                {
                    file = Settings.Current.MainPage;
                }
                else if (names.Contains(target))
                {
                    file = Settings.RepositoriesRules[target].HomeFile;
                }
                else if (names.Contains(target.Split(separator)[0]))
                {
                    file = Settings.ResourcePath + target;
                }
                else if (File.Exists(Settings.ResourcePath + target))
                {
                    file = Settings.ResourcePath + target;
                }
                else if (File.Exists(Settings.RootPath + target))
                {
                    file = Settings.RootPath + target;
                }
            }
            else
            {
                if (names.Contains(target))
                {
                    file = Settings.RepositoriesRules[target].HomeFile;
                }
                else
                {
                    if (referrer.StartsWith(separator))
                    {
                        referrer = referrer[1..];
                    }

                    if (referrer.EndsWith(separator))
                    {
                        referrer = referrer[0..^1];
                    }

                    //System.Diagnostics.Trace.WriteLine(Settings.ResourcePath + target, "Debug");

                    if (File.Exists(Settings.ResourcePath + target))
                    {
                        file = Settings.ResourcePath + target;
                    }

                    else if (string.Equals(referrer.Split(separator)[0], target.Split(separator)[0]))
                    {
                        if (names.Contains(target.Split(separator)[0]))
                        {
                            file = Settings.ResourcePath + target;
                        }
                    }
                    else
                    {
                        if (names.Contains(target.Split(separator)[0]))
                        {
                            file = Settings.ResourcePath + target;
                        }
                        else if (names.Contains(referrer.Split(separator)[0]))
                        {
                            file = Settings.ResourcePath + FilePath.Build(referrer, target);
                        }
                    }
                }
                
            }

            ErrorLogger.WithTrace(Settings, string.Format("[Debug][File Resolve] : target({0}), referrer({1}) - available = {2}, path = '{3}'", target, referrer, (File.Exists(path: file) ? "Yes" : "No"), file), typeof(PathResolver));

            resultfile = file;
            return File.Exists(path: resultfile);
        }
        
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
            {
                path += $"{part}{Path.DirectorySeparatorChar}";
            }

            //path = path.Replace(string.Format("{0}{0}", System.IO.Path.DirectorySeparatorChar), System.IO.Path.DirectorySeparatorChar.ToString());
            return path[0..^1];
        }

        public static string GenerateRandomFilename()
        {
            string coll = "";
            for (int i = 10; i < 100; i += i % 2 == 0 ? 10 : 15)
            {
                if (coll.Length >= 16)
                {
                    break;
                }

                coll += new Random().Next(i, 100).ToString();
            }
            if (coll.Length > 16)
            {
                coll = coll.Substring(0, 16);
            }

            return coll;
        }
    }
}
