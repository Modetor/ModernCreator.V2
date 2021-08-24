using System;
using System.Diagnostics;
using System.IO;
namespace Modetor_MCC.Core
{
    public class ErrorLogger
    {
        private static TextWriterTraceListener textWriter;
        private static bool P_Initialized = false;
        public static string LogFile { get; private set; }
        public static void Initialize()
        {
            if (P_Initialized)
                return;
            if (!Directory.Exists("logs"))
                Directory.CreateDirectory("logs");

            LogFile = Core.FilePath.Build("logs", "L" + DateTime.Now.Ticks.ToString() + ".txt");
            textWriter = new TextWriterTraceListener(File.Open(LogFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite), "Trace -Output");
            Trace.Listeners.Add(textWriter);
            Trace.AutoFlush = true;
            P_Initialized = true;

        }
        public static void WithTrace(string text, Type t)
        {
            Trace.WriteLine($"D=[{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}] M={text}", t.Name);
        }
        public static void WithTrace(Exception exp, Type t)
        {
            Trace.WriteLine($"D=[{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}] T={exp.TargetSite} M={exp.Message} S={exp.StackTrace}", t.Name);
        }
        //public static void WidthTrace(Settings Settings, Exception exp, Type t)
        //{
        //    WithTrace(Settings, string.Format("[Warning][Server error => SetupServerProcedure()] : exception-message : {0}.\nstacktrace : {1}", exp.Message, exp.StackTrace), t);
        //}

        //internal ErrorLogger()
        //{
        //    try
        //    {

        //        if (!File.Exists(FilePath))
        //        {
        //            File.Create(FilePath);
        //        }
        //    }
        //    catch (Exception exp)
        //    {
        //        Trace.WriteLine($"Failed to initialize Error logging. Message : {exp.StackTrace}\nStacktrace : {exp.Message}\n");
        //    }
        //}
        //public void Log(string message, string stacktrace)
        //{
        //    string text = $"\nAt:{DateTime.Now.ToShortDateString()}" +" {\n";
        //    text += $"Message:{message},\n";
        //    text += $"Stacktrace:{stacktrace}"+"\n}";

        //    try { File.WriteAllText(FilePath, text); }
        //    catch(Exception exp)
        //    {
        //        Trace.WriteLine($"Failed to write to logging file. Message : {exp.StackTrace}\nStacktrace : {exp.Message}\n");
        //    }
        //}
        //public void PrintError(string message) => Error(message);
        //public void PrintWarn(string message) => Warn(message);
        public static void Error(string message)
        {
            Trace.WriteLine($"{message}\n");
        }
        public static void Warn(string message)
        {
            Trace.WriteLine($"{message}\n");
        }
        //public void Log(Exception exp) => Log(exp.Message, exp.StackTrace);
        //public void Clear() {
        //    try
        //    {
        //        using FileStream f = File.OpenWrite(FilePath);
        //        lock (f)
        //        {
        //            f.SetLength(0);
        //        }
        //    }
        //    catch (Exception exp)
        //    {
        //        //ErrorLogger.WithTrace(Settings, string.Format("[Warning][Server error => Start()] : exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), GetType());
        //        Trace.WriteLine($"Failed to clear logging file. Message : {exp.StackTrace}\nStacktrace : {exp.Message}\n");
        //    }
        //}

        //internal readonly string FilePath = string.Format(AppDomain.CurrentDomain.BaseDirectory + "base{0}log{0}errors.txt", Path.DirectorySeparatorChar);
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
