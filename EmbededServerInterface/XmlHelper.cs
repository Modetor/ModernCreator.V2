using System.Text;

namespace EmbededServerInterface
{
    public static class XMLHelper
    {
        public static T? Deserialize<T>(string xmlString)
        {
            if (xmlString == null) return default;
            System.Xml.Serialization.XmlSerializer? serializer
                        = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using (var reader = new StringReader(xmlString))
            {
                return (T)serializer.Deserialize(reader);
            }
        }

        public static string Serialize<T>(T o)
        {
            System.Xml.Serialization.XmlSerializer? xmlSerializer = new(typeof(T));
            using StringWriter sw = new();
            xmlSerializer.Serialize(sw, o);
            return sw.ToString();
        }

        public static byte[] SerializeToByteArray<T>(T o)
        {
            System.Xml.Serialization.XmlSerializer? xmlSerializer = new(typeof(T));
            using StringWriter sw = new();
            xmlSerializer.Serialize(sw, o);
            return Encoding.UTF8.GetBytes(Serialize<T>(o));
        }
    }
}
