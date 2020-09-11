using System;
using System.IO;
using System.Linq;

namespace Modetor.Net.Server
{
    public static class PathResolver
    {
        public static bool Resolve(string target, string referrer, out string resultfile)
        {
            //if(HeaderKeys.DEBUG_MODE) Console.WriteLine("D [{2}] : target({0}) - referrer({1})", target, referrer, nameof(PathResolver));
            Console.WriteLine("Target : {0}",target);
            if(target == null)
            {
                target = Settings.MainPage;
                ErrorLogger.Print("[PathResolver] : arg0(target) is null, used Settings.MainPage as a fallback target");
            }

            string file = string.Empty;

            string[] names = Settings.Repositories;
            char separator = Path.DirectorySeparatorChar;
            //target = target.Replace('/', separator);

            if (target.Equals(separator))
                target = string.Empty;

            if (target.StartsWith(separator))
                target = target.Substring(1);

            if (target.EndsWith(separator))
                target = target.Substring(0, target.Length - 1);

            if (referrer.Equals(string.Empty))
            {
                if (target.Equals(string.Empty))
                    file = Settings.MainPage;
                else if (names.Contains(target))
                    file = Settings.ResourcePath + target + separator.ToString() + Settings.RepositoriesRules[target].HomeFile;
                else if (names.Contains(target.Split(separator)[0]))
                    file = Settings.ResourcePath + target;
                else if(File.Exists(Settings.ResourcePath + target))
                    file = Settings.ResourcePath + target;
            }
            else
            {
                if (names.Contains(target))
                    file = Settings.ResourcePath + target + separator.ToString() + Settings.RepositoriesRules[target].HomeFile;
                else
                {
                    referrer = referrer.Replace('/', separator);
                    if (referrer.StartsWith(separator))
                        referrer = referrer.Substring(1);

                    if (referrer.EndsWith(separator))
                        referrer = referrer.Substring(0, referrer.Length - 1);

                    if (File.Exists(Settings.ResourcePath + target))
                    {
                        file = Settings.ResourcePath + target;
                    }

                    else if (string.Equals(referrer.Split(separator)[0], target.Split(separator)[0]))
                    {
                        if (names.Contains(target.Split(separator)[0]))
                            file = Settings.ResourcePath + target;
                    }
                    else
                    {
                        if (names.Contains(target.Split(separator)[0]))
                        {
                            file = Settings.ResourcePath + target;
                        }
                        else if (names.Contains(referrer.Split(separator)[0]))
                            file = Settings.ResourcePath + FilePath.Build(referrer, target);
                    }
                }
                
            }

            if(Settings.AllowOutput)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine("[PathResolver] : target({0}), referrer({1}) - available = ", target, referrer, (File.Exists(path: file) ? "Yes" : "No"));
                Console.ResetColor();
            }
            resultfile = file;
            return File.Exists(path: resultfile);
        }
        public static string GetRuleFromHeaderKeys(HeaderKeys keys)
        {/*
            //__get_rule__(file ?? keys.GetValue("target") ?? keys.GetValue("Referer"));
            string rule = string.Empty;
            string[] names = conf.GetRules();
            string target = keys.GetValue("target") ?? string.Empty, referrer = keys.GetValue("Referer") ?? string.Empty;
            target = target.Replace('/', '\\');

            if (target.Equals("\\"))
                target = string.Empty;

            if (target.StartsWith("\\"))
                target = target.Substring(1);

            if (target.EndsWith("\\"))
                target = target.Substring(0, target.Length - 1);

            if(names.Contains(target.Split('\\')[0])) {
                rule = target.Split('\\')[0];
                if (!conf.Rules[rule].Available) rule = string.Empty;
            }
            referrer = referrer.Replace("http://"+keys.GetValue("Host"), string.Empty);
            referrer = referrer.Replace('/', '\\');
            if (referrer.StartsWith("\\"))
                referrer = referrer.Substring(1);

            if (referrer.EndsWith("\\"))
                referrer = referrer.Substring(0, referrer.Length - 1);

            if(names.Contains(referrer.Split('\\')[0])) {
                rule = referrer.Split('\\')[0];
                if (!conf.Repositories[rule].Available) rule = string.Empty;
            }
            return rule ?? string.Empty;*/
            return null;
        }
    }
}
