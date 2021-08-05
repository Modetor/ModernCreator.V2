using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Modetor.Net.Server.Core.Backbone
{
    public enum DevicePlatform : int
    {
        IOS = 1,
        ANDROID = 2,
        WIN_PHONE = 3,
        WINDOWS = 4,
        LINUX = 5,
        MAC = 6,
        CHROME = 7,
        UNKNOWN = 8
    }
    public enum ControlSignal : int
    {
        ON = 1,
        OFF = 0
    }
    public enum DeviceType : int
    {
        MOBILE = 1,
        DESKTOP = 2,
        UNKNOWN = 3
    }
    public struct Device
    {
        internal static DeviceType GetDevicType(string type)
        {
            return type.Equals("Mobile") ? 
                DeviceType.MOBILE : type.Equals("Computer") ? 
                DeviceType.DESKTOP : DeviceType.UNKNOWN;
        }
        public static Device GetDeviceByUserAgent(string userAgent)
        {
            Device device = Unknown;

            if (string.IsNullOrEmpty(userAgent))
            {
                return device;
            }

            Index r0 = userAgent.IndexOf('(') + 1;
            Index rx = userAgent.IndexOf(')');

            string deviceInfo = userAgent[r0..rx];
            string[] parts = deviceInfo.Split(';').Select(i => i.Trim()).ToArray();

            if (userAgent.Contains("Mobile"))
            {
                if (parts[0].Equals("iPhone"))
                {
                    device.Platform = DevicePlatform.IOS;
                    device.Brand = "Apple iPhone";
                    device.Version = parts[1].Replace("CPU iPhone OS ", string.Empty).Split(' ')[0];
                    device.Architecture = "x64";
                    device.Type = DeviceType.MOBILE;
                }
                else if (parts[0].Equals("iPhone9,3"))
                {
                    //iPhone9,3; U; CPU iPhone OS 10_0_1 like Mac OS X
                    device.Platform = DevicePlatform.IOS;
                    device.Brand = "Apple iPhone 7";
                    device.Version = parts[2].Replace("CPU iPhone OS ", string.Empty).Split(' ')[0];
                    device.Architecture = "x64";
                    device.Type = DeviceType.MOBILE;
                }
                else if (parts[0].Equals("iPhone9,4"))
                {
                    //iPhone9,4; U; CPU iPhone OS 10_0_1 like Mac OS X
                    device.Platform = DevicePlatform.IOS;
                    device.Brand = "Apple iPhone 7 Plus";
                    device.Version = parts[2].Replace("CPU iPhone OS ", string.Empty).Split(' ')[0];
                    device.Architecture = "x64";
                    device.Type = DeviceType.MOBILE;
                }
                else if (parts[0].Equals("Apple-iPhone7C2/1202.466"))
                {
                    //Apple-iPhone7C2/1202.466; U; CPU like Mac OS X; en
                    device.Platform = DevicePlatform.IOS;
                    device.Brand = "Apple iPhone 6";
                    device.Version = "0.0";
                    device.Architecture = "x64";
                    device.Type = DeviceType.MOBILE;
                }
                else if (parts[0].Contains("iPhone") || parts[0].Contains("CPU like Mac OS") || parts[0].Contains("CPU iPhone OS"))
                {
                    device.Platform = DevicePlatform.IOS;
                    device.Brand = "Apple iPhone";
                    device.Version = "0.0";
                    device.Architecture = "x32";
                    device.Type = DeviceType.MOBILE;
                }
                else if (parts[0].Equals("Linux"))
                {
                    // all android OS devices
                    //Linux; Android 8.0.0; SM-G960F Build/R16NW
                    //Linux; Android 7.0; SM-G892A Build/NRD90M; wv
                    //Linux; Android 7.0; SM-G930VC Build/NRD90M; wv
                    //Linux; Android 6.0.1; SM-G935S Build/MMB29K; wv
                    //Linux; Android 6.0.1; SM-G920V Build/MMB29K
                    //Linux; Android 5.1.1; SM-G928X Build/LMY47X
                    //Linux; Android 6.0.1; Nexus 6P Build/MMB29P
                    //Linux; Android 7.1.1; G8231 Build/41.2.A.0.219; wv
                    //Linux; Android 6.0.1; E6653 Build/32.2.A.0.253
                    //Linux; Android 6.0; HTC One X10 Build/MRA58K; wv
                    //Linux; Android 6.0; HTC One M9 Build/MRA58K
                    //
                    device.Version = parts[1][parts[1].LastIndexOf(' ')..].Trim();
                    device.Platform = parts[1].StartsWith("Android") ? DevicePlatform.ANDROID : DevicePlatform.LINUX;
                    device.Type = DeviceType.MOBILE;
                    device.Architecture = "x86_64";
                    device.Brand = parts[2][..parts[2].LastIndexOf(' ')].Trim();
                }
            }
            else
            {
                if (parts[0].Equals("Windows NT 6.1") || parts[0].Equals("Windows NT 10.0"))
                {
                    device.Version = parts[0][10..].Trim();
                    device.Platform = DevicePlatform.WINDOWS;
                    device.Brand = "Microsoft";
                    device.Architecture = parts[2];
                    device.Type = DeviceType.DESKTOP;
                }
                else if (parts[0].Equals("Macintosh"))
                {
                    device.Brand = "Apple";
                    device.Platform = DevicePlatform.MAC;
                    device.Version = parts[1][parts[1].LastIndexOf(' ')..];
                    device.Architecture = "x86_64";
                    device.Type = DeviceType.DESKTOP;
                }
                else if (parts[0].Equals("X11"))
                {
                    device.Version = "0.0";
                    device.Brand = "Linux";
                    device.Type = DeviceType.DESKTOP;
                    device.Platform = DevicePlatform.LINUX;
                    if (parts[1].Equals("CrOS"))
                    {
                        string[] sysinfo = parts[1].Split(' ').Select(a => a.Trim()).ToArray();
                        device.Brand = "Chrome OS";
                        device.Platform = DevicePlatform.CHROME;
                        device.Architecture = sysinfo[1];
                        device.Version = sysinfo[2];
                    }
                    else
                    {
                        device.Brand = parts[1];
                        device.Architecture = parts[2][5..];
                    }
                }
                //Console.WriteLine(deviceInfo);
            }


            return device;
        }
        public static Device Unknown
        {
            get
            {
                Device d = new();
                d.Brand = "Unknown";
                d.Version = "0.0";
                d.Architecture = "32";
                d.Type = DeviceType.UNKNOWN;
                d.Platform = DevicePlatform.UNKNOWN;
                return d;
            }
        }
        public DevicePlatform Platform;
        public string Version;
        public string Brand;
        public string Architecture;
        public DeviceType Type;
    }

    public enum HttpMethod
    {
        GET, POST, PUT, PATCH, DELETE, QUERY, UNKNOWN
    }
    public enum HttpVersion
    {
        HTTP1_0, HTTP1_1, HTTP2_0, UNKNOWN
    }

    public enum HttpRequestState : int
    {
        PARSE_FAILURE,
        IO_FAILURE,
        GENERIC_FAILURE,
        PERMISSION_FAILURE,
        SECURITY_FAILURE,
        OK,
        UNKNOWN_RESOURCE_FAILURE
    }

    public enum ServerEventMethod : int
    {
        PUSH, LOOP
    }
    public struct ModetorServerVersion
    {
        public static readonly ModetorServerVersion V1_0 = new("Modetor.Net.Server/1.0");
        public static readonly ModetorServerVersion V1_1 = new("Modetor.Net.Server/1.1");
        public static readonly ModetorServerVersion V1_2 = new("Modetor.Net.Server/1.2");
        public static readonly ModetorServerVersion V1_3 = new("Modetor.Net.Server/1.3");
        public static readonly ModetorServerVersion V1_4 = new("Modetor.Net.Server/1.4");

        public ModetorServerVersion(string value)
        {
            Value = value;
        }
        private readonly string Value;

        public override string ToString()
        {
            return Value;
        }
    }






    public static class CoreStuff
    {
        public static bool IsConnected(this System.Net.Sockets.TcpClient client)
        {
            System.Net.NetworkInformation.TcpConnectionInformation tcpConnectionInformation = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpConnections()
                .SingleOrDefault(x => x.RemoteEndPoint.Equals(client.Client.RemoteEndPoint));

            
            return tcpConnectionInformation == null ? false : tcpConnectionInformation.State == System.Net.NetworkInformation.TcpState.Established ? true : false;
        }
        public static string Serialize(this System.Collections.Specialized.NameValueCollection collection)
        {
            if (collection.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder str = new();
            
            str.Append('{');
            foreach(string key in collection.AllKeys)
            {
                bool isArray = false;
                string[] values = collection.GetValues(key).Select(a => {


                    string[] temp = a.Split(',');
                    if (temp.Length > 1)
                    {
                        isArray = true;
                    }

                    for (int i = 0; i < temp.Length; i++)
                    {
                        try
                        {
                            if (int.TryParse(temp[i], out int result))
                            {
                                temp[i] = result.ToString();
                            }
                            else if (double.TryParse(temp[i], out double dresult))
                            {
                                temp[i] = dresult.ToString();
                            }
                            else if (float.TryParse(temp[i], out float fresult))
                            {
                                temp[i] = fresult.ToString();
                            }
                            else if (bool.TryParse(temp[i], out bool bresult))
                            {
                                temp[i] = bresult ? "true" : "false";
                            }
                            else
                            {
                                temp[i] = temp[i][0] == '"' && temp[i][temp[i] .Length-1] == '"' ? temp[i] : '"' + temp[i] + '"';
                            }
                        }
                        catch { }
                    }
                    return string.Join(',', temp);


                }).ToArray();
                if (values.Length == 0)
                {
                    continue;
                }
                else if(isArray)
                {
                    str.Append($"\"{key}\"").Append(':').Append('[')
                    .Append(string.Join(',', values)).Append(']').Append(',');
                }
                else
                {
                    str.Append($"\"{key}\"").Append(':')/*.Append('"')*/.Append(values[0])/*.Append('"')*/.Append(',');
                }
            }
            string w = str.ToString()[..^1]+'}';

            return w;
        }
        public static System.Collections.Generic.IEnumerable<byte[]> SplitInChunks(this byte[] self, int chuckSize)
        {
            int start = 0;
            int total = self.Length;

            while(start+chuckSize < total)
            {
                yield return self[start..(start + chuckSize)];
                start += chuckSize;
            }
            yield return self[start..];






            /*
             int p = 0;
            int l = self.Length;

            while(l-p > chuckSize)
            {
                yield return self[p..chuckSize];
                p += chuckSize;
            }
            yield return self[p..];
             */
        }
    }



    public struct Feature
    {
        public string Name;
        public string Path;
        public string TargetName;
        public string TargetExcutable;
        public bool RequireServerIOFocus,
                    WaitForExit;
        public string[] Excute(string args,string input, bool returnError, Settings settings)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));


            using (Process process = new())
            {
                string[] result = { string.Empty, string.Empty };

                StringBuilder output = new();
                StringBuilder error = new();
                //RequireServerIOFocus
                ProcessStartInfo startInfo = new();
                startInfo.FileName = TargetExcutable;
                startInfo.UseShellExecute = false;
                startInfo.Arguments = args;

                process.StartInfo = startInfo;
                if (RequireServerIOFocus)
                {
                    
                    // Decision made in 31/2/2021 to wrap all input streams to pass arguments
                    startInfo.RedirectStandardOutput = 
                        startInfo.RedirectStandardError =
                            startInfo.RedirectStandardInput = true;

                    if (!process.Start())
                        return null;

                    process.ErrorDataReceived += (sender, e) => error.Append(e.Data);

                    process.StandardInput.WriteLine(input ?? string.Empty);
                    process.StandardInput.Flush();

                    process.BeginErrorReadLine();
                    output.Append(process.StandardOutput.ReadToEnd());
                }
                else
                {
                    if (!process.Start())
                        return null;
                }


                if (WaitForExit)
                {
                    if (!process.WaitForExit(60000))
                    {
                        process.Close();
                        process.Kill(true);
                        
                    }
                    else
                        process.Close();


                    if (error.Length != 0)
                    {
                        if (returnError)
                            result[1] = error.ToString();

                        ErrorLogger.WithTrace(settings, string.Format("[Warning][Server-Features error => (name='{1}')] : exception : {0}\n", error.ToString(), Name), typeof(HttpRespondHeader));
                    }

                    result[0] = output.ToString();
                    return result;
                }
                else
                    return null;

                
            }


        }


    }
}