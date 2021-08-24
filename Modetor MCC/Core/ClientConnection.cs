using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Modetor_MCC.Core
{
    class ClientConnection
    {
        public string ServerIP;
        public string ErrorMessage { get; private set; } = string.Empty;
        private ClientConnection()
        {
            if (!Properties.Settings.Default.IsSolderedMode &&
                !System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                ErrorMessage = "We depend on the network. So please connect to the network and restart the app";
                return;
            }


            if (!Properties.Settings.Default.IsSolderedMode && string.Empty.Equals(Properties.Settings.Default.ServerIP))
            {
                ErrorMessage = "Ops! it seems that we've been miscofigured!";
                return;
            }
            else if (Properties.Settings.Default.IsSolderedMode)
                ServerIP = "127.0.0.1";
            else
                ServerIP = Properties.Settings.Default.ServerIP;
        }


        public async Task<(bool, byte[])> Send(int task, string message, string tag)
        {
            byte[] data = null;


            // Task<(bool, byte[])> t = await
            bool state;
            try
            {
                string requestData = $"TaskCode={0}&" +
                                $"DeviceName={Properties.Settings.Default.DeviceName}&" +
                                $"Repository={Properties.Settings.Default.Repository}&" +
                                $"={Properties.Settings.Default.ControllerVersion}&" +
                                $"Message={message}&" +
                                $"Tag={tag}";
                WebClient wc = new WebClient();
                data = await wc.DownloadDataTaskAsync($"http://{ServerIP}:{Properties.Settings.Default.ServerPort}/backdoor?{requestData}");
                state = true;
            }
            catch (Exception exp)
            {
                ErrorLogger.WithTrace(exp, typeof(ClientConnection));
                state = false;
            }

            return (state, data);
        }
        
        public async Task<(bool, string)> SendStringAsync(int task, string message, string tag)
        {
            string result = null;

            bool state;
            try
            {
                string requestData = $"TaskCode={0}&" +
                                $"DeviceName={Properties.Settings.Default.DeviceName}&" +
                                $"Repository={Properties.Settings.Default.Repository}&" +
                                $"ControllerVersion={Properties.Settings.Default.ControllerVersion}&" +
                                $"Message={message}&" +
                                $"Tag={tag}";
                WebClient wc = new();
                string link = $"http://{ServerIP}:{Properties.Settings.Default.ServerPort}/management/manage-clients/handle_connections.py?{Uri.EscapeDataString(requestData)}";
                byte[] data = await wc.DownloadDataTaskAsync(link);
                if (data != null)
                    result = Encoding.UTF8.GetString(data);
                state = true;
            }
            catch (Exception exp)
            {
                ErrorLogger.WithTrace(exp, typeof(ClientConnection));
                state = false;
            }

            return (state, result);
        }

        public (bool, string) SendString(int task, string message, string tag)
        {
            string result = null;

            bool state;
            try
            {
                string requestData = $"TaskCode={0}&" +
                                $"DeviceName={Properties.Settings.Default.DeviceName}&" +
                                $"Repository={Properties.Settings.Default.Repository}&" +
                                $"ControllerVersion={Properties.Settings.Default.ControllerVersion}&" +
                                $"Message={message}&" +
                                $"Tag={tag}";
                WebClient wc = new();
                string link = $"http://{ServerIP}:{Properties.Settings.Default.ServerPort}/management/manage-clients/handle_connections.py?{Uri.EscapeDataString(requestData)}";
                byte[] data = wc.DownloadData(link);
                if (data != null)
                    result = Encoding.UTF8.GetString(data);
                state = true;
            }
            catch (Exception exp)
            {
                ErrorLogger.WithTrace(exp, typeof(ClientConnection));
                state = false;
            }

            return (state, result);
        }


        private static ClientConnection Current;

        public static void Initialize()
        {
            if (Current == null)
                Current = new ClientConnection();
            
            if(!Current.ErrorMessage.Equals(string.Empty))
                ErrorLogger.WithTrace(Current.ErrorMessage, Current.GetType());
        }
        public static ClientConnection Reference => Current;

        public static IPAddress GetLocalIPAddress()
        {
            foreach (IPAddress ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip;
            }

            return IPAddress.None;
        }
    }
}
