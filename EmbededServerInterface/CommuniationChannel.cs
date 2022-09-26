namespace EmbededServerInterface
{
    public enum HeaderMessageType
    {
        Put, Get, Remove, End
    }
    public struct Header
    {
        public string Message;
        public HeaderMessageType Type;
    }
    public interface CommuniationChannel
    {
        byte[] GetBuffer();
        Header GetHeader();
        long GetPacketSize();
        void SetHeader(Header header);
        void SetBuffer();
        void ClearBuffer();
        void ClearHeader();
        void SetPacketSize();
        void GetBody();
        void SetBody(byte[] body);
        void ClearBody();
        void SendAsync(System.Net.Sockets.TcpClient client);
        void Send(System.Net.Sockets.TcpClient client);
    }
}