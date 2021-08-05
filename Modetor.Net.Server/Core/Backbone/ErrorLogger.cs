using System;
using System.Diagnostics;
using System.IO;
namespace Modetor.Net.Server.Core.Backbone
{
    public class ErrorLogger
    {
        private static TextWriterTraceListener textWriter;
        private static bool P_Initialized = false;
        public static void Initialize()
        {
            if (P_Initialized)
                return;

            textWriter = new TextWriterTraceListener(File.Open(Backbone.FilePath.Build("logs", "L" + DateTime.Now.Ticks.ToString() + ".txt"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite), "Trace -Output"); ;

            Trace.Listeners.Add(textWriter);
            Trace.AutoFlush = true;
            P_Initialized = true;

        }
        public static void WithTrace(Settings Settings, string text, Type t)
        {
            if ((bool)Settings.Current.AllowOutput && (bool)Settings.Current.DebugMode)
            {
                WithTrace(text, t);
            }
        }
        public static void WithTrace(string text, Type t)
        {
            
            Trace.WriteLine(text, t.Name);
        }
        //public static void WidthTrace(Settings Settings, Exception exp, Type t)
        //{
        //    WithTrace(Settings, string.Format("[Warning][Server error => SetupServerProcedure()] : exception-message : {0}.\nstacktrace : {1}", exp.Message, exp.StackTrace), t);
        //}

        internal ErrorLogger()
        {
            try
            {

                if (!File.Exists(FilePath))
                {
                    File.Create(FilePath);
                }
            }
            catch (Exception exp)
            {
                Trace.WriteLine($"Failed to initialize Error logging. Message : {exp.StackTrace}\nStacktrace : {exp.Message}\n");
            }
        }
        public void Log(string message, string stacktrace)
        {
            string text = $"\nAt:{DateTime.Now.ToShortDateString()}" +" {\n";
            text += $"Message:{message},\n";
            text += $"Stacktrace:{stacktrace}"+"\n}";
            
            try { File.WriteAllText(FilePath, text); }
            catch(Exception exp)
            {
                Trace.WriteLine($"Failed to write to logging file. Message : {exp.StackTrace}\nStacktrace : {exp.Message}\n");
            }
        }
        public void PrintError(string message) => Error(message);
        public void PrintWarn(string message) => Warn(message);
        public static void Error(string message)
        {
            Trace.WriteLine($"{message}\n");
        }
        public static void Warn(string message)
        {
            Trace.WriteLine($"{message}\n");
        }
        public void Log(Exception exp) => Log(exp.Message, exp.StackTrace);
        public void Clear() {
            try
            {
                using FileStream f = File.OpenWrite(FilePath);
                lock (f)
                {
                    f.SetLength(0);
                }
            }
            catch (Exception exp)
            {
                //ErrorLogger.WithTrace(Settings, string.Format("[Warning][Server error => Start()] : exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), GetType());
                Trace.WriteLine($"Failed to clear logging file. Message : {exp.StackTrace}\nStacktrace : {exp.Message}\n");
            }
        }

        internal readonly string FilePath = string.Format(AppDomain.CurrentDomain.BaseDirectory + "base{0}log{0}errors.txt", Path.DirectorySeparatorChar);
    }
}
