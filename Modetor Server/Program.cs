using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Modetor.Net.Server;
namespace Modetor_Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Server server = new Server();
            Welcome();

            string cmd = null;
            do
            {
                Blue("Type command >> ");
                cmd = Console.ReadLine()?.Trim()?.ToLower() ?? "exit";

                if (cmd.Equals("exit"))
                    break;

                if (cmd.StartsWith("run"))
                {
                    if(cmd.Length == 3)
                        Red("Syntax Error. Run command must be like run 127.0.0.1");
                    else
                    {
                        cmd = cmd.Substring(3).Trim();
                        server.SetAddress(cmd);
                    }
                }
            }
            while (true);
            
        }
        static string[] GetNeworkIPs()
        {
            List<string> l = new List<string>();
            IPAddress[] addr = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
            int i = 0;
            for (; i < addr.Length; i++)
            {
                if (addr[i].AddressFamily == AddressFamily.InterNetwork) { l.Add(addr[i].ToString()); }
            }

            if (!l.Contains("127.0.0.1"))
                l.Add("127.0.0.1");
            return l.ToArray();
        }

        static void Welcome()
        {
            Yellow("// ");Blue("Modetor Server\n"); 
            Yellow("// "); Blue("Date : 7-9-2020\n");
            Yellow("// "); Blue("Author : Mohammad S. Albay\n");
            for(int i = 0; ++i < 50;)
                Yellow("-");
            Green("\nAvailable IPs : \n");

            foreach (string item in GetNeworkIPs())
                Console.WriteLine(" - " + item);

            Yellow("\n\nNote: help to show available commands\n");
        }

        static void Blue(string text)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.Write(text);
            Console.ResetColor();
        }
        static void Yellow(string text)
        {
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(text);
            Console.ResetColor();
        }
        static void Red(string text)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write(text);
            Console.ResetColor();
        }
        static void Green(string text)
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write(text);
            Console.ResetColor();
        }

    }
}
