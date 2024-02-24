using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Modetor.Net.Server.Core.Backbone
{
    public class HttpRespondHeader
    {
        private static readonly string LineTerminator = "\r\n";
        private static readonly string EXT_JAVASCRIPT = ".js";
        private static readonly string EXT_CSS = ".css";
        private static readonly string EXT_HTML = ".html";
        private static string CPYTHON_PATH => Settings.Features["cpython"].TargetExcutable;
        private static readonly string ERR_MESSAGE = "Error occurred while processing request";

        public static readonly ModetorServerVersion CurrentServerVersion = ModetorServerVersion.V1_0;

        public static HttpRespondHeader GenerateRespond(HttpRequestHeader requestHeader, HttpServers.BaseServer server)
        {
            if (Rule.Corrupted.Equals(requestHeader.Repository))
            {
                throw new NotSupportedException(string.Format("[HttpRequestHeader].Repository is Corrupted!"));
            }

            HttpRespondHeader respond = new();
             
            if(requestHeader.HeaderKeys.ContainsKey("Resolved-Location")) /* [DUPRECATED SCOPE] */
            {
                respond.SetState(requestHeader.HttpVersion, HttpRespondState.MOVED_PERMANENTLY);
                respond.AddHeader("Location", requestHeader.HeaderKeys["Resolved-Location"]);
            }
            else
            {
                bool headerState = string.IsNullOrEmpty(requestHeader.AbsoluteFilePath);
                string mimeType = headerState ? "plain/text" : MimeType.GetMimeTypeFromFile(requestHeader.AbsoluteFilePath);
                
                bool canICacheThisResource = false;
                


                if (headerState)
                {
                    respond.SetState(requestHeader.HttpVersion, HttpRespondState.NOT_FOUND);
                    respond.AddHeader("Server", CurrentServerVersion.ToString());
                }
                else
                {
                    byte[] respondBody = headerState ? null : ProcessRequestedFile(requestHeader, server.Settings, out canICacheThisResource, respond);

                    
                    if(respondBody != null && !respondBody.Equals(HttpRequestHeader.ContentSplitter))
                    {
                        ///
                        ///  FOR REGULAR FILES AND SCRIPTS WHOME NOT USING SELF-RESPOND PROCEDURES
                        ///
                        respond.SetState(requestHeader.HttpVersion, (respondBody != null && respondBody.Length > 0) || respond.P_BodySet ? HttpRespondState.OK : HttpRespondState.NO_CONTENT);
                        respond.AddHeader("Server", ModetorServerVersion.V1_0.ToString());

                        respond.AddHeader("Connection", "close");//keep-alive
                        if ((respondBody != null && respondBody.Length > 0) || respond.P_BodySet)
                        {
                            if (canICacheThisResource)
                            {
                                if ((bool)server.Settings.Current.DebugMode)
                                {
                                    if((bool)server.Settings.Current.EnableCachingFrontendFiles)
                                        respond.AddHeader("Cache-Control", "public, max-age=604800, immutable");
                                    else if (!(requestHeader.AbsoluteFilePath.EndsWith(EXT_JAVASCRIPT) || requestHeader.AbsoluteFilePath.EndsWith(EXT_CSS) || requestHeader.AbsoluteFilePath.EndsWith(EXT_HTML)))
                                        respond.AddHeader("Cache-Control", "public, max-age=604800, immutable");
                                }
                                else
                                    respond.AddHeader("Cache-Control", "public, max-age=604800, immutable");

                            }


                            respond.AddHeader("Content-Type", mimeType);//+ "; charset=UTF-8"

                            respond.AddHeader("Content-Length", respond.P_BodySet ? respond.RespondBuffer[2].Length.ToString() : respondBody.Length.ToString());

                            respond.SetBody(respondBody);
                        }
                    }
                    else
                    {
                        ///
                        /// THIS FOR SCRIPTS WITH SELF-RESPOND PROCEDURES
                        ///
                    }


                }
            }
            
                
            


            return respond;
        }

        internal static byte[] ProcessRequestedFile(HttpRequestHeader requestHeader, Settings settings, out bool doesItApply, HttpRespondHeader resh)
        {
            doesItApply = false;
            if(requestHeader.AbsoluteFilePath.EndsWith(".py"))
            {
                // .Net's IronBython Powerfull Engine
                
                return PythonRunner.Run(requestHeader, resh); //System.IO.File.ReadAllBytes(requestHeader.AbsoluteFilePath);
            }
            else if(requestHeader.AbsoluteFilePath.EndsWith(".py3"))
            {
                // Python's C-based Engine
                return ProcessPython3Script(requestHeader, settings);
            }
            else if(requestHeader.AbsoluteFilePath.EndsWith(".jss"))
            {
                return ChakraCoreObject.RunScript(requestHeader.AbsoluteFilePath, requestHeader, resh);
            }
            else if(requestHeader.AbsoluteFilePath.EndsWith(".exe"))
            {
                // Native request processing app
                return RunNativeRequestProcessorApp(requestHeader, settings);
            }
            else
            {
                // Just read bytes and pass out
                doesItApply = true;
                int count = 0;
            RE_TRY:
                if (count > 10) return null;
                byte[] b;
                try { b = System.IO.File.ReadAllBytes(requestHeader.AbsoluteFilePath); }
                catch { count++; System.Threading.Thread.Sleep(50); goto RE_TRY; }
                return b;
            }
        }

        private static byte[] RunNativeRequestProcessorApp(HttpRequestHeader requestHeader, Settings settings)
        {
            using (System.Diagnostics.Process process = new System.Diagnostics.Process())
            {
                StringBuilder output = new StringBuilder();
                StringBuilder error = new StringBuilder();

                string serializedString = GenerateSerializedInput(requestHeader);


                process.StartInfo.FileName = CPYTHON_PATH;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.Arguments = $"\"{requestHeader.AbsoluteFilePath}\" \"{serializedString}\"";
                process.StartInfo.RedirectStandardOutput = process.StartInfo.RedirectStandardError = true;
                // Decision made in 31/2/2021 to wrap all input streams 
                //                            to pass arguments
                process.StartInfo.RedirectStandardInput = true;

                process.OutputDataReceived += (s, e) =>
                {
                    output.Append(e.Data);
                };
                process.ErrorDataReceived += (s, e) =>
                {
                    error.Append(e.Data);
                };
                process.Start();
                process.StandardInput.WriteLine(serializedString);
                process.StandardInput.Flush();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();



                //process.WaitForExit();
                process.Close();

                //RedirectErrors
                string result = "";
                if (error.Length != 0)
                {
                    if ((bool)settings.Current.RedirectErrors)
                    {
                        result = error.ToString();
                    }
                    else
                    {
                        result = ERR_MESSAGE;
                    }

                    ErrorLogger.WithTrace(settings, string.Format("[Warning][Server error => ProcessPython3Script()] : exception : {0}\n", error.ToString()), typeof(HttpRespondHeader));
                }
                else
                {
                    result = output.ToString();
                }

                byte[] bytes = Encoding.UTF8.GetBytes(result);


                return bytes;
            }
        }

        private static byte[] ProcessPython3Script(HttpRequestHeader requestHeader, Settings settings)
        {

            string serializedString = GenerateSerializedInput(requestHeader);
            
            string[] resultArr = Settings.Features["cpython"].Excute($"\"{requestHeader.AbsoluteFilePath}\"", serializedString, (bool)settings.Current.RedirectErrors, settings);
            if (resultArr == null) return null;

            string result;
            if (resultArr[1] != string.Empty)
                result = resultArr[1];
            else
                result = resultArr[0];

            byte[] bytes = Encoding.UTF8.GetBytes(result);


            return bytes;

        }





        public static string GenerateSerializedInput(HttpRequestHeader requestHeader)
        {
            return "[" + Newtonsoft.Json.JsonConvert.SerializeObject(new
            {
                RequestHeader = new
                {
                    //Parameters = requestHeader.Parameters.Serialize(),
                    HeaderKeys = requestHeader.HeaderKeys.ToArray(),
                    HasUploadFiles = requestHeader.HasUploadedFiles,
                    HTTPMethod = requestHeader.HttpMethod.ToString(),
                    HTTPVersion = requestHeader.HttpVersion.ToString(),
                    ModetorServerVersion = CurrentServerVersion.ToString()
                },
                Server = new
                {
                    ServerIP = requestHeader.Server.IP,
                    ServerPort = requestHeader.Server.Port,
                    ServerAddress = requestHeader.Server.Address,
                    ServerFullAddress = requestHeader.Server.FullAddress,
                    BlockedClients = requestHeader.Server.BlockedClients,
                },
                Repository = requestHeader.Repository,
                Client = new
                {
                    ClientIP = requestHeader.ClientIP,
                    ClientPort = requestHeader.ClientPort,
                    ClientAddress = requestHeader.ClientAddress,
                    IsBlocked = requestHeader.Server.IsBlockedClient(requestHeader.ClientAddress)
                }
            }) + ',' + (requestHeader.Parameters.Count > 0 ? requestHeader.Parameters.Serialize() : "") + "]";
        }





        public HttpRespondHeader()
        {
            //RequestHeader = requestHeader;
            RespondBuffer = new List<byte[]> { null };
            Header = new List<string> { null };
        }

        public void SetState(HttpVersion version,string state)
        {
            //if (P_BodySet) throw new NotSupportedException("Cannot add header after setting body.");
            //P_StateSet = true;
            string versionString = version switch
            {
                HttpVersion.HTTP1_0 => "HTTP/1.0",
                HttpVersion.HTTP1_1 => "HTTP/1.1",
                HttpVersion.HTTP2_0 => "HTTP/2.0",
                _ => "HTTP/1.1",
            };
            Header[0] = $"{versionString} {state}{LineTerminator}";
        }
        public void AddHeader(string key, string value)
        {
            string item = $"{key}: {value}{LineTerminator}";
            if (Header.Where(a => a?.StartsWith(key) ?? false).Any())
                return;
            else
                Header.Add(item);
        }

        public void AddExtraLineBreaker() => Header.Add(LineTerminator);

        public void SetBody(string body) => SetBody(Encoding.UTF8.GetBytes(body));

        public void SetBody(byte[] body)
        {
            if (body == null)
                return;

            //if (P_BodySet) /*throw new NotSupportedException("Cannot set mutiple bodies.");*/
            //    return;
            AddHeader("Content-Length", body.Length.ToString());
            RespondBuffer.Add(Encoding.UTF8.GetBytes(LineTerminator));
            RespondBuffer.Add(body);
            P_BodySet = true;
        }

        public byte[] GetHeaderBytes() => Encoding.UTF8.GetBytes(Header.SelectMany(a => a).ToArray());
        public byte[] Build()
        {
            if (Header[0] == null) return new byte[0];

            RespondBuffer[0] = GetHeaderBytes();
            return RespondBuffer.SelectMany(a => a).ToArray();
        }

        public bool DidRespond() => SelfResponded;
        public void Responded() => SelfResponded = true;

        private bool SelfResponded = false;
        private readonly List<byte[]> RespondBuffer;
        private readonly List<string> Header;
        private bool P_BodySet = false;
    }




    public struct HttpRespondState
    {
        public static readonly string SWITCHING_PROTOCOLS = "101 Switching Protocls";

        public static readonly string OK = "200 OK";
        public static readonly string NO_CONTENT = "204 No Content";

        public static readonly string MOVED_PERMANENTLY = "301 Moved Permanently";

        public static readonly string BAD_REQUEST = "400 Bad Request";
        public static readonly string MAX_CLIENTS_LIMIT = "401 Max Clients Limit"; /*server specific code*/
        public static readonly string FORBIDDEN = "403 Forbidden";
        public static readonly string NOT_FOUND = "404 Not Found";
        public static readonly string METHOD_NOT_ALLOWED = "405 Method Not Allowed";
        public static readonly string PAYLOAD_TOO_LARGE = "413 Payload Too Large";
        public static readonly string UPGRADE_REQUIRED = "426 Upgrade Required";

        public static readonly string INTERNAL_SERVER_ERROR = "500 Internal Server Error";
        
    }
}
