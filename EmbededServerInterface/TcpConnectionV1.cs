using System;
using System.Net.Sockets;

namespace EmbededServerInterface
{
    public static class TcpConnectionV1
    {
        /*public static (MetaInfo, ulong) ReadMetaData(NetworkStream networkStream)
        {
            byte[] data = new byte[12];

            //networkStream.Position = 0;
            networkStream.Read(data, 0, data.Length);


            return (MetaInfo.From(data[0]), BytesToU64(data[4..]));
        }*/

        public static MetaInfo ReadMetaData(NetworkStream networkStream)
        {
            byte[] data = new byte[12];

            //networkStream.Position = 0;
            networkStream.Read(data, 0, data.Length);


            return (MetaInfo.From(data));
        }
        /// <summary>
        ///     Generates a 12 bytes from a MetaInfo(struct) and a payload length(u64/ulong) 
        /// </summary>
        /// <param name="isCompleted"></param>
        /// <param name="streamDataType"></param>
        /// <param name="packetType"></param>
        /// <param name="length"></param>
        /// <returns>A 12 bytes that represents the whole data required to communicat to client/server</returns>
        public static byte[] BuildMetaInfoAndLength(bool isCompleted, StreamDataType streamDataType, PacketType packetType, ulong length)
        {
            byte[] mil = new byte[12];
            mil[0] = MetaInfo.GenerateFlagsByte(isCompleted, streamDataType, packetType);
            U64ToBytes(length).CopyTo(mil, 4);

            return mil;
        }
        public static byte[] U64ToBytes(ulong len)
        {
            return BitConverter.GetBytes(len);
        }
        public static ulong BytesToU64(byte[] buffer) => BitConverter.ToUInt64(buffer, 0);

        public static bool ReadBit(byte b, int n) => ((b >> (n - 1)) & 0x01) == 1;

        
    }
    
    public struct MetaInfo
    {
        // first byte
        public bool IsCompleted;
        public StreamDataType DataType;
        public PacketType PacketType;
        // 3 bytes
        // 8 bytes
        public ulong Length;

        public byte GetFlagsByte()
        {
            uint b = 0; // 99 = 0110 0011
            b |= ((PacketType == PacketType.Handshake ? 1U : 0U) << 0);
            b |= ((IsCompleted ? 1U : 0U) << 1);
            b |= ((DataType == StreamDataType.Stream ? 1U : 0U) << 2);
            return (byte)b;
        }
        public void SetFlagsByte(byte b)
        {
            PacketType = TcpConnectionV1.ReadBit(b, 1) ? PacketType.Handshake : PacketType.Message;
            IsCompleted = TcpConnectionV1.ReadBit(b, 2);
            DataType = TcpConnectionV1.ReadBit(b, 3) ? StreamDataType.Stream : StreamDataType.Text;
        }
        public byte[] ToBytes()
        {
            byte[] mil = new byte[12];
            mil[0] = GetFlagsByte();
            BitConverter.GetBytes(Length).CopyTo(mil, 4);

            return mil;
        }
        public static MetaInfo From(byte[] data)
        {
            MetaInfo instance = new MetaInfo();
            instance.SetFlagsByte(data[0]);
            instance.Length = BitConverter.ToUInt64(data, 4);
            return instance;
        }

        public static byte GenerateFlagsByte(bool isCompleted, StreamDataType streamDataType, PacketType packetType)
        {
            return new MetaInfo
            {
                DataType = streamDataType,
                IsCompleted = isCompleted,
                PacketType = packetType
            }.GetFlagsByte();
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
