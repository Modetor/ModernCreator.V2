using System.Net.Sockets;
using System.Text;
using System.Xml.Serialization;

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
        public Header Header;
        public byte[] Payload;

        private static int I_EndOfHeader(ref byte[] data)
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
                if(found > 0 && data[i] != wanted)
                {
                    wanted = '\n';
                    found = 0;
                }
                
            }
            return index;
        }
        public static Message From(byte[] data)
        {

            Message message = new();
            int headerLastChar = I_EndOfHeader(ref data);
            message.Header = XMLHelper.Deserialize<Header>(Encoding.UTF8.GetString(data[0..headerLastChar]));
            message.Payload = data[(headerLastChar + 4)..];
            
            return message;
        }

        public string SerializeHeader()
        {
            return XMLHelper.Serialize(Header);
        }

        public byte[] ToByteArray()
        {
            byte[] headerBytes = Encoding.UTF8.GetBytes(SerializeHeader() + "\n\r\n\r");
            byte[] buffer = new byte[headerBytes.Length + Payload.Length];
            headerBytes.CopyTo(buffer, 0);
            Payload.CopyTo(buffer, headerBytes.Length);
            return buffer;
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
            messagesQueue = new();
            tcpClient = Session.Connection;
            stream = tcpClient.GetStream();
            CancellationTokenSource = new CancellationTokenSource();    
        }

        public string GetDefinedRout() => DEFAULT_ROUT;
        public void OnRawDataReceived(byte[] data) { }
        public void OnMessageReceived(Header header, byte[] body) { }
        public void OnMessageSent(Header header, byte[] body) { }
        public void MessagingLoop()
        {
            CancellationToken token = CancellationTokenSource.Token;
            
            if(Session.Connection.Equals("Both"))
            {
                while (true)
                {
                    if(SpinWait.SpinUntil(() => stream.DataAvailable, 100))
                    {
                        // read operation
                        MetaInfo info = TcpConnectionV1.ReadMetaData(stream);
                        if(info.Length == 0)
                        {

                        }
                        else
                        {
                            byte[] buffer = new byte[info.Length];
                            stream.Read(buffer, 0, buffer.Length);
                            Message message = Message.From(buffer);
                        }
                        

                    }
                    else if(SpinWait.SpinUntil(() => messagesQueue.Count > 0, 100))
                    {
                        // send operation
                    }
                    else
                        Thread.Sleep(0);
                }
            }
            else if(Session.Connection.Equals("Server-Client"))
            {

            }
            else if (Session.Connection.Equals("Client-Server"))
            {

            }

            
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


        internal bool DataToBeRecieved => stream.DataAvailable;
        internal bool DataToBeSent => false;


        private readonly Dictionary<int, Message> messagesQueue;
        private readonly TcpClient tcpClient;
        private readonly NetworkStream stream;
        private byte[] buffer;
        public Session Session;
        public CancellationTokenSource CancellationTokenSource;






        public static SendMessage(Message message)
        {
            
        }
    }
}