using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using EmbededServer;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Net.WebSockets;
using System.Collections.Specialized;
using System.Collections;

namespace DeliveryServiceManagement
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            /*            byte b = new byte();
                        b = (byte)((b << 0) & 1);

                        System.Collections.BitArray bitArray = new System.Collections.BitArray(new bool[]
                        {
                            false, true, false, false, false, false, false , false
                        });

                        byte[] temp = new byte[1];
                        //bytes[0]
                        bitArray.CopyTo(temp, 0);*/

            BitArray x = new(new byte[] { 4 });

            // (200)n =  (1100 1000)b
            /*


            6 bytes > states ( is shake hand, is completed, message type, .. last 1 for future) 
            4 bytes > size of the coming content!

             */

            
            uint b = 0; // 99 = 0110 0011
            b |= (1U << 0);
            b |= (0U << 2);
            b |= (1U << 7);
            for(int i = 1; i < 9; i++)
                System.Diagnostics.Debug.WriteLine("Value {0}", EmbededServerInterface.TcpConnectionV1.ReadBit((byte)b, i));


            Server = Server.GetServer(ConfigurationLoader.Load("server/configuration/config.xml"));
            Server.Start();



        }



        public static Server? Server { get; private set; }
    }
}


/*
 
  is handshake 0 / or message 1
  is completed 1 / no 0
  is stream 0 / text 1
  the first 5 bits reserved for future development
 [0     0      0      0      0      0      0     0]
 
 

 if client going to send file (let say size is 20MB)
 our algorihm would split the file in chunks of 5MB ber packet

    so the scenario would be like this :
    
    1 byte (00000001) // message, not compeleted
    next 4 bytes for the size of conetent as In32 (the byte[] to allocate)
    then next is the actual content, the 5MB byte[]

    the server here would save the content in TempContent(byte[][] - limit is set for each component)
 */
