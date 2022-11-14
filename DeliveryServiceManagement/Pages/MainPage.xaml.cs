using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Net;
using System.IO;
using EmbededServer;
using System.Diagnostics;
using EmbededServerInterface;
using System.Collections;

namespace DeliveryServiceManagement.Pages
{
    
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : Page
    {
        readonly string handshake = @"<Client Connection=""both | server-talk | client-talk"">
    <ID>489348923</ID>
    <Rout>/Main</Rout>
    <User>Mohammad</User>
    <Application>A</Application>
    <ApplicationVersion>12.6</ApplicationVersion>
    <DeviceInfo>
        <DeviceName>Mohammad</DeviceName>
        <Platform>Windows</Platform>
        <PlatformVersion>11.34.12.2005</PlatformVersion>
        <DeviceModel>Acer Aspire E15</DeviceModel>
        <WorkingDirectory>C://Files</WorkingDirectory>
        <ExecutableName>App.exe</ExecutableName>
        <WindowWidth>600</WindowWidth>
        <WindowHeight>550</WindowHeight>
        <ScreenWidth>1080</ScreenWidth>
        <ScreenHeight>1920</ScreenHeight>
    </DeviceInfo>
    </Client>";

        readonly string message = @"<Header Type='0'>
        Fuck all you nigges
    </Header>";
        readonly TcpClient client = new();
        public MainPage()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(client.Connected)
            {
                SendMessage();
            }
            else
            {
                Connect();
            }
        }
        private void SendMessage() { }
        private void Connect()
        {
            client.Connect(IPAddress.Parse("127.0.0.1"), 5722);
            Debug.WriteLine("Connected");
            NetworkStream stream = client.GetStream();
            byte[] bytes = Encoding.UTF8.GetBytes(handshake);

            byte[] sizeOfBuffer = BitConverter.GetBytes((ulong)bytes.Length);
            //if (BitConverter.IsLittleEndian)
            //    Array.Reverse(sizeOfBuffer);

            byte[] metaInfoAndSize = new byte[12], 
                   oneByte = new byte[1];

            sizeOfBuffer.CopyTo(metaInfoAndSize, 4);

            BitArray bits = new BitArray(new bool[] { true, true, false, false, false, false, false, false });

            bits.CopyTo(oneByte, 0);
            metaInfoAndSize[0] = oneByte[0];
            
            stream.Write(metaInfoAndSize);
            stream.Flush();
            stream.Write(bytes, 0, bytes.Length);

            int response = stream.ReadByte();
            if(response == -1)
            {
                Debug.WriteLine("Errrror");
            }
            //if (string.IsNullOrEmpty(serverDecision)) return;


            Console.WriteLine("Client : \n{0}", response);

            stream.Write(Encoding.UTF8.GetBytes(message));

            client.Close();
        }
    }
}
