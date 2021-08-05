using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Modetor_Server
{
    internal partial class Program
    {
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
        private static void SetupServerEvent(Modetor.Net.Server.Core.HttpServers.BaseServer server)
        {
            server.Started += (sender, args) =>
            {
                Green($"Server started {sender.IP}:{sender.Port}\n");
            };
            server.Suspended += (sender, args) =>
            {
                Green($"Server suspended {sender.IP}:{sender.Port}\n");
            };
            //server.Resumed += (sender, args) =>
            //{
            //    Green($"Server resumed {sender.IP}:{sender.Port}\n");
            //};
            server.Shutteddown += (sender, args) =>
            {
                Green($"Server shutteddown {sender.IP}:{sender.Port}\n");
            };
            server.SmartSwitchEnd += (sender, args) =>
            {
                Green($"Server smartswitched {sender.IP}:{sender.Port}\n");
            };
        }


        static void Welcome()
        {
            Yellow("// "); Blue("Modetor Server\n");
            Yellow("// "); Blue("Date : 7-9-2020\n");
            Yellow("// "); Blue("Author : Mohammad S. Albay\n");
            for (int i = 0; ++i < 50;)
            {
                Yellow("-");
            }

            Console.Write('\n');
            ShowHelpCommands();

            PrintAvailableIPs();

            AGAIN:
            Console.Write("Modetor.Server >> ");
            string command = Console.ReadLine()?.Trim() ?? null;
            if ("quite".Equals(command))
            {
                return;
            }

            Initial(command);
            goto AGAIN;
        }

        private static void PrintAvailableIPs()
        {
            Green("\nAvailable IPs : \n");

            foreach (string item in GetNeworkIPs())
            {
                Console.WriteLine(" - " + item);
            }
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
            {
                l.Add("127.0.0.1");
            }

            return l.ToArray();
        }

    }
}
