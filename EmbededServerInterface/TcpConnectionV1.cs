using System.Net.Sockets;

namespace EmbededServerInterface
{
    public static class TcpConnectionV1
    {
        public static (MetaInfo, ulong) ReadMetaData(NetworkStream networkStream)
        {
            byte[] data = new byte[12];

            //networkStream.Position = 0;
            networkStream.Read(data, 0, data.Length);


            return (MetaInfo.From(data[0]), BytesToU64(data[4..]));
        }

        public static ulong BytesToU64(byte[] buffer) => BitConverter.ToUInt64(buffer, 0);

        public static bool ReadBit(byte b, int n) => ((b >> (n - 1)) & 0x01) == 1;
    }
    
    public struct MetaInfo
    {
        public bool IsCompleted;
        public StreamDataType DataType;
        public PacketType PacketType;

        internal static MetaInfo From(byte b)
        {
            MetaInfo metaInfo = new()
            {
                PacketType = TcpConnectionV1.ReadBit(b, 1) ? PacketType.Handshake : PacketType.Message,
                IsCompleted = TcpConnectionV1.ReadBit(b, 2),
                DataType = TcpConnectionV1.ReadBit(b, 3) ? StreamDataType.Stream : StreamDataType.Text
            };
            return metaInfo;
        }
    }

    public enum PacketType 
    { 
        Handshake, Message 
    }
    public enum StreamDataType
    {
        Stream, Text
    }
}
