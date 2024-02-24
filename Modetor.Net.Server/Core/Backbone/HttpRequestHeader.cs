/*\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\
III
III          بسم الله الرحمن الرحيم
III 
\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\\*/




using HttpMultipartParser;
using Newtonsoft.Json;
using Renci.SshNet.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Modetor.Net.Server.Core.Backbone
{
    public class HttpRequestHeader
    {
        public static byte[] ContentSplitter = new byte[] { 13, 10, 13, 10 };

        private static readonly string TempExtinsion = ".temp";
        private static readonly string MODETOR_SERVER_BOUNDARY = "Modetor-Server-Boundary";
        private static readonly string PLAIN_REQUEST_BOUNDARY = "text/plain;charset=";
#pragma warning disable CS0414
        private static readonly string MODETOR_CLIENT_BOUNDARY = "bytes/plain";
        private static readonly string CONTENT_DISPOSITION = "Content-Disposition:";
#pragma warning restore CS0414
        private static readonly string CONTENT_TYPE = "Content-Type";
        private static readonly string FORMDATA_BOUNDARY = "multipart/form-data; boundary=";
        private static readonly string FORMDATA_POST_ENCODED = "application/x-www-form-urlencoded";
        private static readonly string FORMDATA_POST_JSON = "application/json";


        public HttpRequestHeader(HttpRequestHeader req, string file = null)
        {
            Tag = req.Tag;
            AbsoluteFilePath = file ?? req.AbsoluteFilePath;
            RequestedTarget = AbsoluteFilePath.Replace(Settings.ResourcePath, string.Empty).Replace("\\", "/");
            Client = req.Client;
            System.Net.IPEndPoint remoteEndPoint = ((System.Net.IPEndPoint)Client.Client.RemoteEndPoint);
            ClientAddress = remoteEndPoint.ToString();
            ClientIP = remoteEndPoint.Address.ToString();
            ClientPort = remoteEndPoint.Port;
            Server = req.Server;
            HeaderKeys = req.HeaderKeys;
            P_UploadFilePaths = req.P_UploadFilePaths;
            Parameters = req.Parameters;
            Server.Settings.GetRepositoryByPath(req.AbsoluteFilePath, out Rule rule);
            Repository = rule;
        }
        public HttpRequestHeader(TcpClient client, HttpServers.BaseServer server, bool autoRead = true)
        {
            Client = client;
            System.Net.IPEndPoint remoteEndPoint = ((System.Net.IPEndPoint)Client.Client.RemoteEndPoint);
            ClientAddress = remoteEndPoint.ToString();
            ClientIP = remoteEndPoint.Address.ToString();
            ClientPort = remoteEndPoint.Port;
            Server = server;
            HeaderKeys = new Dictionary<string, string>();
            P_UploadFilePaths = new List<string>();
            Parameters = new System.Collections.Specialized.NameValueCollection();

            if (autoRead)
                ProcessRequestHeader(client);

        }

        public void ProcessRequestHeader(TcpClient client, int max_header = -1)
        {
            // don't re-read!
            if (State != HttpRequestState.None)
                return;
            try
            {
                State = ReadContentHeader(client.GetStream(), max_header).Result;
            }
            catch (Exception exp)
            {
                State = HttpRequestState.GENERIC_FAILURE;
                ErrorLogger.WithTrace(Server.Settings, string.Format("[Error][Server request handler => ProcessRequestHeader()] : exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), GetType());
            }
        }

        private async System.Threading.Tasks.Task<HttpRequestState> ReadContentHeader(NetworkStream stream, int max_header = -1)
        {
            
            MemoryStream memory = new MemoryStream();

            if(stream.CanRead)
            {
                #region Draft 1
                //// Algorithm 1.1
                //long maxBytes = (int)Server.Settings.Current.MaxHttpRequestSize * 1024 * 1024;
                //long length = 0;
                //int readingCounts = 5;
                //byte[] buffer;
                //while(readingCounts >= 0)
                //{
                //    if(!stream.DataAvailable)
                //    {
                //        System.Threading.SpinWait.SpinUntil(() => stream.DataAvailable && Client.Available != 0, 2000);
                //        readingCounts--;
                //    }

                //    if (Client.Available > maxBytes) return HttpRequestState.IO_FAILURE;

                //    int r = 0;
                //    buffer = new byte[Client.Available];
                //    if ((r = await stream.ReadAsync(buffer, 0, Client.Available)) == 0)
                //        continue;
                //    else
                //    {
                //        length += r;
                //        await memory.WriteAsync(buffer, 0, r);
                //    }
                #endregion
                #region Draft 0
                // Algorithm 1
                //long maxBytes = (int)Server.Settings.Current.MaxHttpRequestSize * 1024 * 1024;
                //long length = 0;
                //int readingCounts = 5;
                //byte[] b = new byte[1024];
                //do
                //{
                //    if (stream.DataAvailable)
                //    {
                //        if (length > maxBytes) break;


                //        int read = await stream.ReadAsync(b, 0, b.Length);
                //        if (read <= 0) break;
                //        length += read;

                //        await memory.WriteAsync(b, 0, read);
                //    }
                //    else
                //    {
                //        if (readingCounts-- <= 0) break;
                //        System.Threading.Thread.Sleep(50);
                //        //System.Threading.SpinWait.SpinUntil(() => stream.DataAvailable && Client.Available > 0, 2000);
                //        //;
                //    }

                //}
                //while (true);
                #endregion
                // Algorithm 2
                long maxBytes = (max_header <= 0 ? (int)Server.Settings.Current.MaxHttpRequestSize : max_header) * 1024 * 1024;
                long length = 0;
                int readingCounts = 5;
                DateTime now = DateTime.Now;
                byte[] b;

                int state = 0;
                do
                {
                    if ((DateTime.Now - now).Seconds > ((int)Server.Settings.Current.ReceiveTimeout / 1000))
                    {
                        state = -1;
                        break;
                    }
                    if (stream.DataAvailable && Client.Available > 0)
                    {
                        if (length > maxBytes || Client.Available > maxBytes)
                        {
                            state = -2;
                            break;
                        }

                        b = new byte[Client.Available];
                        int read = await stream.ReadAsync(b, 0, b.Length);
                        if (read <= 0)
                        {
                            break;
                        }

                        length += read;

                        await memory.WriteAsync(b, 0, read);
                    }
                    else
                    {
                        if (readingCounts-- <= 0)
                            break;

                        System.Threading.Thread.Sleep(5);
                    }

                }
                while (true);

                if(state == -1)
                    return HttpRequestState.IO_FAILURE;
                if (state == -2)
                    return HttpRequestState.PAYLOAD_TOO_LARGE;
            }
            else
            {
                return HttpRequestState.IO_FAILURE;
            }

            memory.Seek(0, SeekOrigin.Begin);

            if (memory.Length == 0) return HttpRequestState.IO_FAILURE;

            byte[] mem = memory.ToArray();

            if((bool)Server.Settings.Current.DebugMode && (bool)Server.Settings.Current.PrintRequestData)
            {
                Console.WriteLine("\nReuqest : \n\"{0}\"\r\n", Encoding.UTF8.GetString(memory.ToArray()));
            }
            
            byte[] data;
            byte[] header;
            bool test = mem.Contains(ContentSplitter, out int pushback);
            if (test)
            {
                pushback += ContentSplitter.Length;
                header = mem[..pushback];
                data = mem[pushback..];
                RequestBody = data;
                string strheader = Encoding.UTF8.GetString(header);

                ParseHeader(strheader.Split("\r\n"));

                if (HeaderKeys.ContainsKey("Resolved-Location"))
                    return HttpRequestState.OK;

                //
                /// IF NULL, JUST ABORT EVERYTHING(INCLUDING PARAMETERS..) AND RESPOND WITH 404 ERROR
                //
                if (AbsoluteFilePath != null && Repository != Rule.Corrupted)
                {

                    if (!Repository.AllowCrossRepositoriesRequests && HeaderKeys.ContainsKey("R-Referer"))
                    {
                        Tuple<bool, string> t = PathResolver.Resolve(Server.Settings, HeaderKeys["R-Referer"], string.Empty);

                        if (t.Item1)
                        {
                            if (Server.Settings.GetRepositoryByPath(t.Item2, out Rule previuosRepo))
                            {
                                if (!previuosRepo.Equals(Repository))
                                {
                                    ErrorLogger.WithTrace(Server.Settings, string.Format("[Warning][Server error => ReadContentHeader()] : cross-repo-requests flag is set to 'false'. request denied"), GetType());
                                    return HttpRequestState.PERMISSION_FAILURE;
                                }
                            }
                        }
                    }
                    //
                    /// SUPPORT PRIVATE DIRECTORIES
                    //
                    else if (Repository.PrivateDirectories.Length != 0 && Repository.PrivateDirectories.Where(source => AbsoluteFilePath.StartsWith(source)).Count() != 0)
                    {
                        ErrorLogger.WithTrace(Server.Settings, string.Format("[Warning][Server error => ReadContentHeader()] : cannot access a private directories"), GetType());
                        return HttpRequestState.PERMISSION_FAILURE;
                    }
                    else if (AbsoluteFilePath.EndsWith(".rules") || AbsoluteFilePath.EndsWith(".exe") || AbsoluteFilePath.EndsWith("settings.json") || AbsoluteFilePath.EndsWith(".ini"))
                    {
                        ErrorLogger.WithTrace(Server.Settings, string.Format("[Warning][Server error => ReadContentHeader()] : cannot access this file. permission denied"), GetType());
                        return HttpRequestState.SECURITY_FAILURE;
                    }


                    if (HeaderKeys.ContainsKey(MODETOR_SERVER_BOUNDARY))
                    {
                        if (HeaderKeys[MODETOR_SERVER_BOUNDARY].StartsWith("----")
                            || HeaderKeys[MODETOR_SERVER_BOUNDARY].Equals(FORMDATA_POST_ENCODED)
                            || HeaderKeys[MODETOR_SERVER_BOUNDARY].Equals(FORMDATA_BOUNDARY)
                            || HeaderKeys[MODETOR_SERVER_BOUNDARY].Equals(FORMDATA_POST_JSON))
                        {
                            if(HeaderKeys[MODETOR_SERVER_BOUNDARY].Equals(FORMDATA_POST_JSON))
                            {
                                string json = Encoding.UTF8.GetString(data);
                                Dictionary<string, string> dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                                foreach(string key in dic.Keys)
                                {
                                    if(!Parameters.AllKeys.Contains(key))
                                        Parameters.Add(key, dic[key]);
                                }
                                Console.WriteLine(json);
                            }
                            else if (Repository.SupportUploads)
                            {
                                MultipartFormDataParser parser;
                                try { parser = await MultipartFormDataParser.ParseAsync(new MemoryStream(data)); }
                                catch(Exception exp)
                                {
                                    ErrorLogger.WithTrace(Server.Settings, string.Format("[Error][Server request handler => ReadContentHeader()] : exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), GetType());
                                    return HttpRequestState.PARSE_FAILURE;
                                }
                                
                                foreach (FilePart file in parser.Files)
                                {
                                RETRY:
                                    string filepath = FilePath.Build(Repository.UploadDirectory, FilePath.GenerateRandomFilename() + file.FileName + TempExtinsion);
                                    if (File.Exists(filepath))
                                    {
                                        goto RETRY;
                                    }

                                    File.WriteAllBytes(filepath, ((MemoryStream)file.Data).ToArray());
                                    Parameters.Add(file.Name, string.Join(';', file.FileName, filepath));
                                    P_UploadFilePaths.Add(filepath);
                                }

                                foreach (ParameterPart item in parser.Parameters)
                                {
                                    if (!Parameters.AllKeys.Contains(item.Name))
                                        Parameters.Add(item.Name, item.Data);
                                }
                            }
                            else
                            {
                                ErrorLogger.WithTrace(Server.Settings, "uploaded data(usually it is a form data) just dropped due to Repository.SupportUploads is set to false", GetType());
                            }

                        }
                        else if (HeaderKeys[MODETOR_SERVER_BOUNDARY].Equals(PLAIN_REQUEST_BOUNDARY))
                        {
                            System.Collections.Specialized.NameValueCollection item = System.Web.HttpUtility.ParseQueryString(Encoding.UTF8.GetString(data));

                            foreach (string key in item.AllKeys)
                                Parameters.Add(key, item.Get(key));
                        }
                        //else if(false/*HeaderKeys[MODETOR_SERVER_BOUNDARY].Equals(FORMDATA_POST_ENCODED)*/)
                        //{
                        //    System.Collections.Specialized.NameValueCollection item = System.Web.HttpUtility.ParseQueryString(Encoding.UTF8.GetString(data));

                        //    foreach (string key in item.AllKeys)
                        //        Parameters.Add(key, item.Get(key));
                        //}
                        //else
                        //{
                            
                        //}
                        //Console.WriteLine("\n\n\nServer's Unknown data : {0}\n\n",Encoding.UTF8.GetString(data));

                    }
                    else if(data != null && data.Length > 0)
                    {
                        int prelength = Parameters.Count;
                        try {
                            System.Collections.Specialized.NameValueCollection item = System.Web.HttpUtility.ParseQueryString(Encoding.UTF8.GetString(RequestBody));
                            if(item.Count != 0)
                            {
                                foreach (string key in item.AllKeys)
                                    Parameters.Add(key, item.Get(key));
                            }
                        }
                        catch { }
                    }
                    return HttpRequestState.OK;
                }
                else
                {
                    return HttpRequestState.UNKNOWN_RESOURCE_FAILURE;
                }
            }

            return HttpRequestState.PARSE_FAILURE;
        }
        private void ParseHeader(string[] headers)
        {
            foreach (string header in headers[1..])
            {
                if (string.IsNullOrEmpty(header))
                {
                    continue;
                }

                if (header.Contains(':'))
                {
                    string[] parts = header.Split(':',2).Select(a => a.Trim()).ToArray();
                    if (!HeaderKeys.ContainsKey(parts[0]))
                    {
                        if (CONTENT_TYPE.Equals(parts[0]))
                        {
                            if (parts[1].StartsWith(FORMDATA_BOUNDARY))
                            {
                                HeaderKeys.Add(MODETOR_SERVER_BOUNDARY, parts[1].Replace(FORMDATA_BOUNDARY, string.Empty));
                            }
                            else if(parts[1].StartsWith(PLAIN_REQUEST_BOUNDARY))
                            {
                                HeaderKeys.Add(MODETOR_SERVER_BOUNDARY, PLAIN_REQUEST_BOUNDARY);
                            }
                            else if(parts[1].StartsWith(FORMDATA_POST_ENCODED))
                            {
                                HeaderKeys.Add(MODETOR_SERVER_BOUNDARY, FORMDATA_POST_ENCODED);
                            }
                            else if (parts[1].StartsWith(FORMDATA_POST_JSON))
                            {
                                HeaderKeys.Add(MODETOR_SERVER_BOUNDARY, FORMDATA_POST_JSON);
                            }
                        }
                        HeaderKeys.Add(parts[0], parts[1]);
                    }
                }
                    
            }

            Index r0 = headers[0].IndexOf(' ', 0);
            Index rx = headers[0].LastIndexOf(' ');
            string requestType = headers[0][0..r0];
            string httpVersion = headers[0][rx..].Trim();

            Internal_SetRequestType(requestType);
            Internal_SetRequestVersion(httpVersion);

            if(HeaderKeys.ContainsKey("Referer"))
            {
                string temp = HeaderKeys["Referer"].Replace(HeaderKeys["Referer"].StartsWith("https://") ? "https://" : "http://", string.Empty);
                HeaderKeys.Add("R-Referer", temp[(temp.IndexOf('/') + 1)..]);
            }
            string fileNameAndQueryString = System.Web.HttpUtility.UrlDecode(headers[0][r0..rx].Trim());
            RequestedTarget = fileNameAndQueryString;
            string filename;
            string query;
            int rxx = fileNameAndQueryString.IndexOf('?');
            if (rxx == -1)
            {
                filename = fileNameAndQueryString;
                query = string.Empty;
            }
            else
            {
                filename = fileNameAndQueryString[0..rxx];
                query = fileNameAndQueryString[(rxx + 1)..];
                System.Collections.Specialized.NameValueCollection item = System.Web.HttpUtility.ParseQueryString(query);

                foreach (string key in item.AllKeys)
                    Parameters.Add(key, item.Get(key));
            }
            if((bool)Server.Settings.Current.AllowVirtualExtensions && filename.IndexOf('.') != -1)
            {
                string ext = filename[(1 + filename.LastIndexOf('.'))..];

                if (ext.Equals((string)Server.Settings.Current.VirtualNativeExtension))
                    filename = filename.Replace(ext, "exe");
                else if (ext.Equals((string)Server.Settings.Current.VirtualPython3Extension))
                    filename = filename.Replace(ext, "py3");
                else if (ext.Equals((string)Server.Settings.Current.VirtualPythonExtension))
                    filename = filename.Replace(ext, "py");
            }

            RequestedFileName = filename;
            bool isUsingVirtualLink = false;
            if(Server.Settings.VirtualLinks.Count > 0)
            {
                if(Server.Settings.VirtualLinks.ContainsKey(filename))
                {
                    VirtualLinks vlink = Server.Settings.VirtualLinks[filename];
                    if(vlink.Enabled)
                    {
                        isUsingVirtualLink = true;
                        HeaderKeys.Remove("Resolved-Location");
                        if(vlink.Redirect)
                            HeaderKeys.Add("Resolved-Location", vlink.Target + "?" + query);
                        else
                        {
                            isUsingVirtualLink = false;
                            filename = vlink.Target;
                        }
                    }
                }
            }
            
            // at this point, we need no file-path-check things :) 
            if (isUsingVirtualLink)
                return;

            Tuple<bool, string> t = PathResolver.Resolve(Server.Settings, filename, HeaderKeys.ContainsKey("R-Referer") ? HeaderKeys["R-Referer"] : string.Empty);

            if (t.Item1)
            {
                AbsoluteFilePath = t.Item2;

                if (Server.Settings.GetRepositoryByPath(t.Item2, out Rule r))
                    Repository = r;
            }

        }

        

        private void Internal_SetRequestVersion(string httpVersion)
        {
            HttpVersion = httpVersion switch {
                "HTTP/1.0" => HttpVersion.HTTP1_0,
                "HTTP/1.1" => HttpVersion.HTTP1_1,
                "HTTP/2.0" => HttpVersion.HTTP2_0,
                _ => HttpVersion.UNKNOWN,
            };
        }
        private void Internal_SetRequestType(string requestMethod)
        {
            HttpMethod = Enum.IsDefined(typeof(HttpMethod), requestMethod) ? (HttpMethod)Enum.Parse(typeof(HttpMethod), requestMethod) : HttpMethod.UNKNOWN;
        }
        public string GetHeaderValue(string key) => HeaderKeys.ContainsKey(key) ? HeaderKeys[key] : null;
        public bool HasHeaderKey(string key) => HeaderKeys.ContainsKey(key);
        public void RemoveHeaderValue(string key) => HeaderKeys.Remove(key);
        public string GetRequestParameters() => Parameters.Serialize();
        public void AddParameters(string parameters)
        {
            System.Collections.Specialized.NameValueCollection item = System.Web.HttpUtility.ParseQueryString(parameters);
            
            foreach (string key in item.AllKeys)
                Parameters.Add(key, item.Get(key));
        }
        public void ClearParameters() => Parameters.Clear();
        public bool HasParameters => Parameters.Count > 0;
        public bool ContainParameter(string p) => Parameters.AllKeys?.Contains(p) ?? false;
        public string GetParameter(string p) => Parameters.Get(p) ?? null;
        internal void DeleteUploadFiles()
        {
            try
            {
                foreach (string file in P_UploadFilePaths)
                {
                    if(File.Exists(file))
                        File.Delete(file);
                }
            }
            catch (Exception exp)
            {
                ErrorLogger.WithTrace(Server.Settings, string.Format("[Warning][Server error => DeleteUploadFiles()] : exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), GetType());
            }

            P_UploadFilePaths.Clear();
        }

        internal bool IsWebsocketUpgradRequest()
        {
            //ErrorLogger.WithTrace($"HttpMethod = {HttpMethod}", GetType());
            //ErrorLogger.WithTrace($"HeaderKeys.ContainsKey('Upgrade') = {HeaderKeys.ContainsKey("Upgrade")}", GetType());
            //ErrorLogger.WithTrace($"HeaderKeys['Upgrade'] = {HeaderKeys["Upgrade"]}", GetType());
            //ErrorLogger.WithTrace($"HeaderKeys.ContainsKey('Connection') = {HeaderKeys["Connection"]}", GetType());
            return HttpMethod == HttpMethod.GET && HeaderKeys.ContainsKey("Upgrade") && HeaderKeys["Upgrade"].Equals("websocket")
                 && HeaderKeys.ContainsKey("Connection") && HeaderKeys["Connection"].Equals("Upgrade");
        }
        internal bool IsServerSentEventRequest()
        { 
            return HttpMethod == HttpMethod.GET && HeaderKeys.ContainsKey("Connection") && HeaderKeys["Connection"].Equals("keep-alive")
                 && HeaderKeys.ContainsKey("Accept") && HeaderKeys["Accept"].Equals("text/event-stream");
        }
        #region Properties
        private readonly List<string> P_UploadFilePaths;
        public readonly TcpClient Client;
        public byte[] RequestBody;
        public readonly HttpServers.BaseServer Server;
        public HttpRequestState State { get; private set; } = HttpRequestState.None;
        public readonly string ClientAddress;
        public readonly string ClientIP;
        public readonly int ClientPort;
        public Dictionary<string, string> HeaderKeys;
        public System.Collections.Specialized.NameValueCollection Parameters;
        public HttpMethod HttpMethod { get; private set; }
        public HttpVersion HttpVersion { get; private set; }
        public string RequestedTarget { get; private set; }
        public string RequestedFileName { get; private set; }
        public string AbsoluteFilePath { get; set; } = null;
        public Rule Repository { get; private set; } = Rule.Corrupted;
        public string[] UploadFilePaths => P_UploadFilePaths.ToArray();
        public bool HasUploadedFiles => P_UploadFilePaths.Count > 0;
        public dynamic Tag, /* Tag is passed from one to another */
                       /* these properties works as a dynamic temporarely memory for the RequestHandler */
                       Reg, Reg1, Reg2;
        #endregion


        [Obsolete("Cannot be used anymore", true)]
        public struct HttpRequestItems
        {
            public string Name { get; set; }
            public string Filename { get; set; }
            public string MimeType { get; set; }
            public dynamic Value { get; set; }
            public bool IsFile { get; set; }
        }

        
    }











    static class ByteArrayRocks
    {

        public static bool Contains(this byte[] self, byte[] candidate, out int pb)
        {
            pb = 0;
            if (IsEmptyLocate(self, candidate))
            {
                return false;
            }

            for (int i = 0; i < self.Length; i++)
            {
                if (IsMatch(self, i, candidate))
                {
                    pb = i;
                    return true;
                }
            }

            return false;
        }

        static bool IsMatch(byte[] array, int position, byte[] candidate)
        {
            if (candidate.Length > (array.Length - position))
            {
                return false;
            }

            for (int i = 0; i < candidate.Length; i++)
            {
                if (array[position + i] != candidate[i])
                {
                    return false;
                }
            }

            return true;
        }

        static bool IsEmptyLocate(byte[] array, byte[] candidate)
        {
            return array == null
                    || candidate == null
                    || array.Length == 0
                    || candidate.Length == 0
                    || candidate.Length > array.Length;
        }
    }


}
