using System.Net.Sockets;

namespace EmbededServerInterface
{
    [System.Xml.Serialization.XmlRootAttribute(ElementName = "DeviceInfo")]
    public struct DeviceInfo
    {
        public string DeviceName;
        public string Platform;
        public string PlatformVersion;
        public string DeviceModel;
        public string WorkingDirectory;
        public string ExecutableName;
        public uint WindowWidth, WindowHeight;
        public uint ScreenWidth, ScreenHeight;
    }

    [System.Xml.Serialization.XmlRootAttribute(ElementName = "Client")]
    public struct Client
    {
        [System.Xml.Serialization.XmlAttribute("Connection")]
        public string Connection { get; set; }
        [System.Xml.Serialization.XmlElement(ElementName = "ID", Type = typeof(int))]
        public int ID { get; set; }
        [System.Xml.Serialization.XmlElement(ElementName = "Rout", Type = typeof(string))]
        public string Rout { get; set; }
        [System.Xml.Serialization.XmlElement(ElementName = "User", Type = typeof(string))]
        public string User { get; set; }
        [System.Xml.Serialization.XmlElement(ElementName = "Application", Type = typeof(string))]
        public string Application { get; set; }
        [System.Xml.Serialization.XmlElement(ElementName = "ApplicationVersion")]
        public string ApplicationVersion { get; set; }
        [System.Xml.Serialization.XmlElement(ElementName = "DeviceInfo", Type = typeof(DeviceInfo))]
        public DeviceInfo DeviceInfo { get; set; }

        public static Client InvalidClient()
        {
            return new Client()
            {
                Rout = string.Empty
            };
        }
    }
    public struct Session
    {
        public Session(Client client, TcpClient tcpClient)
        {
            Client = client;
            Connection = tcpClient;
        }
        public Client Client { get; set; }
        public TcpClient Connection { get; set; }
    }
}
