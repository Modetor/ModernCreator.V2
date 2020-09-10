using System;
using System.Collections.Generic;
using System.IO;

namespace Modetor.Net.Server
{
    public class ErrorLogger
    {
        internal ErrorLogger()
        {
            try
            {
                if (!File.Exists(FilePath))
                    File.Create(FilePath);
            }
            catch (Exception exp)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.Write("Failed to initialize Error logging. Message : {1}\nStacktrace : {0}\n",exp.StackTrace, exp.Message);
                Console.ResetColor();
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
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.Write("Failed to write to logging file. Message : {1}\nStacktrace : {0}\n", exp.StackTrace, exp.Message);
                Console.ResetColor();
            }
        }
        public void PrintError(string message) => Print(message);
        public static void Print(string message)
        {
            if (!Settings.AllowOutput) return;
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write("{0}\n", message);
            Console.ResetColor();
        }
        public void Log(Exception exp) => Log(exp.Message, exp.StackTrace);
        public void Clear() {
            try
            {
                using (FileStream f = File.OpenWrite(FilePath))
                {
                    lock (f)
                    {
                        f.SetLength(0);
                    }
                }
            }
            catch (Exception exp)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.Write("Failed to clear logging file. Message : {1}\nStacktrace : {0}\n", exp.StackTrace, exp.Message);
                Console.ResetColor();
            }
        }

        internal readonly string FilePath = string.Format(AppDomain.CurrentDomain.BaseDirectory + "base{0}log{0}errors.txt", Path.DirectorySeparatorChar);
    }
}
