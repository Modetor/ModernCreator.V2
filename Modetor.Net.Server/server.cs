using System;
using System.Net.Sockets;
using IronPython.Runtime;
namespace Modetor.Net.Server
{
    public class Server
    {
        internal static Server instance = null; 
        public static Server GetServer()
        {
            if (null == instance)
                instance = new Server();
            return instance;
        }
        private Server()
        {
            Logger = new ErrorLogger();
            if (!Settings.Read())
                mServerReady = false;

        }
        public void Shutdown()
        {
            IsRunning = false;
        }
        public void SetAddress(string ip, int port)
        {
            if (!mServerReady) return;
            if (ip == null || port < 0) throw new ArgumentNullException("address is null");
            if (IsRunning)
                Shutdown();
            Address = ip+":"+port;
            try { mTcpListener = new TcpListener(System.Net.IPAddress.Parse(ip), port); }
            catch(Exception exp) { Logger.Log(exp); Logger.Print("[Server] : Failed to initialize the server with the specified ip and/or port"); }
        } 


        public bool IsRunning { get; private set; }
        public string Address { get; private set; }
        public string InternetProtocol { get; private set; }
        public int Port { get; private set; }

        private bool mServerReady = false;
        private TcpListener mTcpListener;
        public ErrorLogger Logger;
    }
}
