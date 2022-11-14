using EmbededServerInterface;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace EmbededServer
{
    internal class Handshake
    {
        public static class Responses
        {
            public static readonly byte CLOSE = 0;
            public static readonly byte NOT_SUPPORTED_APP = 30;
            public static readonly byte NOT_SUPPORTED_VERSION = 31;
            public static readonly byte NOT_SUPPORTED_IP = 32;
            public static readonly byte NOT_SUPPORTED_DEVICE = 33;
            public static readonly byte MAX_CLIENTS_CAPACITY = 34;
            public static readonly byte NOT_SUPPORTED_CONNECTION = 35;
            public static readonly byte OK = 255;



        }
        public class ClientBuilder
        {
            public ClientBuilder(Configuration configuration,System.Net.Sockets.TcpClient client)
            {
                client = client ?? throw new ArgumentNullException(nameof(client));
                //stream.Write() 
                //System.Runtime.InteropServices.OSPlatform.Windows
                //try
                {
                    NetworkStream stream = client.GetStream();
                    //StreamReader reader = new(stream, Encoding.UTF8, false, -1, true);
                    //StreamWriter writer = new(stream,Encoding.UTF8,-1, true);

                    /**/

                    (MetaInfo, ulong) metaData = TcpConnectionV1.ReadMetaData(stream);

                    byte[] buffer = new byte[metaData.Item2];
                    stream.Read(buffer, 0, buffer.Length);
                    EmbededServerInterface.Client client_t =
                        XMLDeserializer.Deserialize<EmbededServerInterface.Client>(Encoding.UTF8.GetString(buffer));
                    Console.WriteLine("");

                    /*byte[] bufferSizeBytes = new byte[5];

                    stream.Read(bufferSizeBytes, 0, bufferSizeBytes.Length);
                    UInt32 num = BitConverter.ToUInt32(bufferSizeBytes[1..4], 0);*/

                    // 
                    //string text = reader.ReadToEnd();
                    byte response = 0;
                    /*EmbededServerInterface.Client client_t = 
                        XMLDeserializer.Deserialize<EmbededServerInterface.Client>(text);
                    reader.Close();*/

                    if (!configuration.SupportedApplications.IsSupportedApplication(client_t.Application))
                    
                    stream.WriteByte(response);

                }

            }


            public EmbededServerInterface.Client Build()
            {
                return receivedClient;
            }

            private EmbededServerInterface.Client receivedClient;
        }
    }
}
