using System;
using System.IO;
using System.Net.Sockets;
using IronPython.Runtime;
namespace Modetor.Net.Server
{
    public class Server
    {
        public Server()
        {
            Logger = new ErrorLogger();
            //Console.WriteLine(path);
            //Console.WriteLine(File.Exists(path));
            //mTcpListener = new TcpListener(,);
        }
        public void Shutdown()
        {
            IsRunning = false;
        }
        public void SetAddress(string ip, int port)
        {
            if (ip == null || port < 0) throw new ArgumentNullException("address is null");
            if (IsRunning)
                Shutdown();
            Address = ip+":"+port;
        } 


        public bool IsRunning { get; private set; }
        public string Address { get; private set; }
        public string InternetProtocol { get; private set; }
        public int Port { get; private set; }

        private TcpListener mTcpListener;
        public ErrorLogger Logger;
    }
}
