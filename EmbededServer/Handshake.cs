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



                    MetaInfo metaData = TcpConnectionV1.ReadMetaData(stream);

                    byte[] buffer = new byte[metaData.Length];
                    stream.Read(buffer, 0, buffer.Length);
                    EmbededServerInterface.Client client_t =
                        XMLHelper.Deserialize<Client>(Encoding.UTF8.GetString(buffer));


                    string errMessage = string.Empty;
                    byte[] respondBuffer;

                    if (!configuration.SupportedApplications.IsSupportedApplication(client_t.Application))
                        errMessage = "Unsopported client app";
                    else
                        receivedClient = client_t; // success
                    
                    respondBuffer = TcpConnectionV1.BuildMetaInfoAndLength(
                        true, StreamDataType.Text, PacketType.Handshake, (ulong) errMessage.Length
                    );
                    stream.Write(respondBuffer, 0, 12);
                    
                    if(errMessage.Length != 0)
                    {
                        respondBuffer = Encoding.UTF8.GetBytes(errMessage);
                        stream.Write(respondBuffer, 0, respondBuffer.Length);
                    }

                    stream.Flush();

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
