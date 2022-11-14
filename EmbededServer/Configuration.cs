using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
namespace EmbededServer
{
    public static class XMLDeserializer
    {
        public static T? Deserialize<T>(string xmlString)
        {
            if (xmlString == null) return default;
            System.Xml.Serialization.XmlSerializer? serializer
                        = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using (var reader = new StringReader(xmlString))
            {
                return (T)serializer?.Deserialize(reader);
            }
        }
    }

    public static class ConfigurationLoader
    {
        public static Configuration Load(string config_file)
        {
            Configuration config;

            using(StreamReader reader = new StreamReader(config_file))
                config = XMLDeserializer.Deserialize<Configuration>(reader.ReadToEnd());

            return config;
        }
    }
    public struct Configuration
    {
        public string Address { get; set; }
        public int Port { get; set; }
        public int MaxClientConnectionsAllowed { get; set; }
        public string PlatformsAllowedToConnect { get; set; }
        public DLL[] Components { get; set; }
        [XmlElement("SupportedApplications")]
        public SupportedApplications SupportedApplications { get; set; }
    }
    
    public struct SupportedApplications
    {
        [XmlElement("Application")]
        public SupportedApplication[] Applications { get; set; }
        public bool IsSupportedApplication(string app)
        {
            foreach (SupportedApplication application in Applications)
            {
                if(application.Name.Equals(app))
                        return true;
            }

            return false;
        }
    }

    public struct SupportedApplication
    {
        [XmlAttribute("Name")]
        public string Name { get; set; }
        [XmlAttribute("MinVersion")]
        public string MinVersion { get; set; }
    }
    public struct DLL
    {
        public string EntryPoint { get; set; }
        public string Path { get; set; }
    }
}
