using System;
using System.Collections.Generic;
using System.Text;

namespace Modetor.Net.Server
{
    class ServerObject
    {
        public ServerObject(System.Net.Sockets.TcpClient tcpClient) => SetClient(tcpClient);
        public ServerObject(HeaderKeys header) => SetKeys(header);
        public void Close()
        {
            TcpClient.Close();
        }
        public void SetKeys(HeaderKeys header) => Keys = header;
        public void SetClient(System.Net.Sockets.TcpClient tcpClient)
        {
            TcpClient = tcpClient;
            ClientAddress = TcpClient.Client.RemoteEndPoint.ToString();

            string[] parts = ClientAddress.Split(':');
            ClientIP = parts[0]; ClientPort = int.Parse(parts[1]);

            Stream = TcpClient.GetStream();

        }
        public string ClientIP { get; private set; }
        public int ClientPort { get; private set; }
        public string ClientAddress { get; private set; }


        public HeaderKeys Keys { get; private set; }
        public System.Net.Sockets.TcpClient TcpClient { get; private set; }
        public System.Net.Sockets.NetworkStream Stream { get; private set; }
        public async void Send(string text)
        {
            await Stream.WriteAsync(Encoding.UTF8.GetBytes(text));
        }
        public async void SendNow(string text)
        {
            await Stream.WriteAsync(Encoding.UTF8.GetBytes(text));
            await Stream.FlushAsync();
        }
        public string Result = "Server has nothing to say";
        
    }
}
