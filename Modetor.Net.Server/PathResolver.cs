using System;
using System.IO;
using System.Linq;
using conf = Modetor.Net.Server.Settings;

namespace Modetor.Net.Server
{
    public static class PathResolver
    {/*
        public static bool Resolve(string target, string referrer, out string resultfile)
        {
            //if(HeaderKeys.DEBUG_MODE) Console.WriteLine("D [{2}] : target({0}) - referrer({1})", target, referrer, nameof(PathResolver));
            System.Diagnostics.Debug.Assert(target != null, "path is null");

            string file = string.Empty;
            
            string[] names = conf.Repositories

            target = target.Replace('/', '\\');

            if (target.Equals("\\"))
                target = string.Empty;

            if (target.StartsWith("\\"))
                target = target.Substring(1);

            if (target.EndsWith("\\"))
                target = target.Substring(0, target.Length - 1);

            if (referrer.Equals(string.Empty))
            {
                if (target.Equals(string.Empty))
                    file = conf.MainPage;
                else if (names.Contains(target))
                    file = conf.MainSource + target + "\\" + conf.Rules[target].Home;
                else if (names.Contains(target.Split('\\')[0]))
                    file = conf.MainSource + target;
                else if(File.Exists(conf.MainSource+target))
                    file = conf.MainSource + target;
            }
            else
            {
                if (names.Contains(target))
                    file = conf.MainSource + target + "\\" + conf.Rules[target].Home;
                else
                {
                    referrer = referrer.Replace('/', '\\');
                    if (referrer.StartsWith("\\"))
                        referrer = referrer.Substring(1);

                    if (referrer.EndsWith("\\"))
                        referrer = referrer.Substring(0, referrer.Length - 1);

                    if (File.Exists(conf.MainSource + target))
                    {
                        file = conf.MainSource + target;
                    }

                    else if (string.Equals(referrer.Split('\\')[0], target.Split('\\')[0]))
                    {
                        if (names.Contains(target.Split('\\')[0]))
                            file = conf.MainSource + target;
                    }
                    else
                    {
                        if (names.Contains(target.Split('\\')[0]))
                        {
                            file = conf.MainSource + target;
                            //Console.WriteLine("RN: {0}, {1}", file, File.Exists(file));
                        }
                        else if (names.Contains(referrer.Split('\\')[0]))
                        {

                            file = conf.MainSource + referrer + '\\' + target;
                            //Console.WriteLine("RX: {0}, {1}", file, File.Exists(file));
                        }
                    }
                }
                
            }

            if (HeaderKeys.DEBUG_MODE) Console.WriteLine("D [{3}] : target({1}), referrer({2}) - available = {0}", File.Exists(path: file) ? "Yes" : "No",  target, referrer, nameof(PathResolver));

            resultfile = file;
            return File.Exists(path: resultfile);
        }
        public static string GetRuleFromHeaderKeys(HeaderKeys keys)
        {
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
                if (!conf.Rules[rule].Available) rule = string.Empty;
            }
            return rule ?? string.Empty;
        }
    */}
}
