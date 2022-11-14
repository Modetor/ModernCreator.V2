using System.Net.Sockets;

namespace EmbededServerInterface
{
    public enum HeaderMessageType : int
    {
        Put = 0, Get = 1, Remove = 2, End = 3
    }

    /*
     <Header Type="0" FileName="file.txt">
        Move your shit!
    </Header>
    \r\n\r\n

    [payload here]
     */
    public struct Header
    {
        [System.Xml.Serialization.XmlText]
        public string Text;
        [System.Xml.Serialization.XmlAttribute(AttributeName = "Type", Type = typeof(HeaderMessageType))]
        public HeaderMessageType Type;
    }
    public struct Message
    {
        Header Header;
        byte[] Payload;

        private static int I_EndOfHeader(byte[] data)
        {
            int index = -1, found = 0; ;
            char wanted = '\n';

            for(int i = 0; i < data.Length; i++)
            {
                if (data[i] == wanted)
                {
                    wanted = (wanted == '\n') ? '\r' : '\n';
                    found++;
                    if (found == 4)
                    {
                        index = i - 4;
                        break;
                    }
                }
                
            }
            return index;
        }
        public static Message From(byte[] data)
        {

            Message message = new Message();
            message.Header = new Header();

            return message;
        }
    }
    public interface ICommuniationChannel
    {
        void OnBeforeStart();
        void Start();
        void Stop();
        void OnBeforeStop();
        void MessagingLoop();
        void OnMessageReceived(Message message);
        void OnMessageSent(Message message);
        void OnRawDataReceived(byte[] data);
        string GetDefinedRout();
        void Send();
        void SetHeader(Header header);
        void SetPayload(byte[] data);
    }

    public class CommuniationChannel : ICommuniationChannel
    {
        public static readonly string DEFAULT_ROUT = "";
        public CommuniationChannel(Session session)
        {
            Session = session;
            this.tcpClient = Session.Connection;
            this.stream = tcpClient.GetStream();
            CancellationTokenSource = new CancellationTokenSource();    
        }

        public string GetDefinedRout() => DEFAULT_ROUT;
        public void OnRawDataReceived(byte[] data) { }
        public void OnMessageReceived(Header header, byte[] body) { }
        public void OnMessageSent(Header header, byte[] body) { }
        public void MessagingLoop()
        {
            CancellationToken token = CancellationTokenSource.Token;
            Task.Factory.StartNew(() =>
            {
                while(true)
                {
                    if (token.IsCancellationRequested) break;

                }
            }, token);
        }
        public void Start()
        {
            OnStart();
            MessagingLoop();
        }
        public void OnStart() { Console.WriteLine("[abs]Started"); }
        public void ClearBody() {}

        public void ClearBuffer()
        {
            throw new NotImplementedException();
        }

        public void ClearHeader()
        {
            throw new NotImplementedException();
        }

        public void GetBody()
        {
            throw new NotImplementedException();
        }

        public byte[] GetBuffer()
        {
            throw new NotImplementedException();
        }

        public Header GetHeader()
        {
            throw new NotImplementedException();
        }

        public long GetPacketSize()
        {
            throw new NotImplementedException();
        }

        public void Send(TcpClient client)
        {
            throw new NotImplementedException();
        }

        public void SendAsync(TcpClient client)
        {
            throw new NotImplementedException();
        }

        public void SetBody(byte[] body)
        {
            throw new NotImplementedException();
        }

        public void SetBuffer()
        {
            throw new NotImplementedException();
        }

        public void SetHeader(Header header)
        {
            throw new NotImplementedException();
        }

        public void SetPacketSize()
        {
            throw new NotImplementedException();
        }

        public void Send()
        {
        }

        public void OnBeforeStart()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            OnBeforeStop();
            CancellationTokenSource.Cancel();
            CancellationTokenSource.Dispose();
        }

        public void OnBeforeStop()
        {
            throw new NotImplementedException();
        }

        public void OnMessageReceived(Message message)
        {
            throw new NotImplementedException();
        }

        public void OnMessageSent(Message message)
        {
            throw new NotImplementedException();
        }

        public void SetPayload(byte[] data)
        {
            throw new NotImplementedException();
        }

        private readonly TcpClient tcpClient;
        private readonly NetworkStream stream;
        private byte[] buffer;
        public Session Session;
        public CancellationTokenSource CancellationTokenSource;
    }
}