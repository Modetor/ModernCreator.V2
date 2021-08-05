using ChakraCore.NET;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modetor.Net.Server.Core.Backbone
{
    class ChakraCoreObject
    {
        internal static ChakraRuntime Runtime = ChakraRuntime.Create();
        private static bool registered = false;
        public static byte[] RunScript(string file, HttpRequestHeader req, HttpRespondHeader res)
        {
            
            

            using (ChakraContext context = Runtime.CreateContext(true)) {

                //HttpRequestHeader
                if (!registered)
                {
                    context.ServiceNode.GetService<IJSValueConverterService>().RegisterProxyConverter<HttpRequestHeader>( //register the object converter
                    (binding, instance, serviceNode) => {
                        binding.SetFunction("getServer", new Func<HttpServers.BaseServer>(() => instance.Server));
                        binding.SetFunction("getHeaderKeys", new Func<Dictionary<string, string>>(() => instance.HeaderKeys));
                        binding.SetFunction("getParameters", new Func<System.Collections.Specialized.NameValueCollection>(() => instance.Parameters));
                        binding.SetFunction("getParametersJSON", new Func<string>(() => instance.Parameters.Serialize()));
                        binding.SetFunction("getAbsoluteFilePath", new Func<string>(() => instance.AbsoluteFilePath));
                    });
                    // HttpRespondHeader
                    context.ServiceNode.GetService<IJSValueConverterService>().RegisterProxyConverter<HttpRespondHeader>( //register the object converter
                    (binding, instance, serviceNode) =>
                    {
                        binding.SetMethod<string, string>("setState", (http, state) => instance.SetState(http.Equals("HTTP/1.0") ? HttpVersion.HTTP1_0 : http.Equals("HTTP/1.1") ? HttpVersion.HTTP2_0 : HttpVersion.UNKNOWN, state));
                        binding.SetMethod<string, string>("addHeader", (key, value) => instance.AddHeader(key, value));
                        binding.SetMethod<string>("setBody", (body) => instance.SetBody(body));
                        binding.SetMethod<System.Net.Sockets.TcpClient>("send", c => {
                            byte[] b = instance.Build();
                            c.GetStream().Write(b, 0, b.Length);
                            c.GetStream().Flush();
                        });
                        binding.SetMethod("markAsResponded", instance.Responded);

                    });
                    // TcpClient stuff
                    context.ServiceNode.GetService<IJSValueConverterService>().RegisterProxyConverter<System.Net.Sockets.TcpClient>( //register the object converter
                    (binding, instance, serviceNode) =>
                    {
                        binding.SetMethod<string>("sendText", (s) => {
                            byte[] b = Encoding.UTF8.GetBytes(s);
                            instance.GetStream().Write(b, 0, b.Length);
                            instance.GetStream().Flush();
                        });
                        binding.SetMethod<HttpRespondHeader>("send", (s) => {
                            byte[] b = s.Build();
                            instance.GetStream().Write(b, 0, b.Length);
                            instance.GetStream().Flush();
                        });
                        binding.SetFunction<string>("getIP", () => ((System.Net.IPEndPoint)instance.Client.RemoteEndPoint).Address.ToString());
                        binding.SetFunction<int>("getPort", () => ((System.Net.IPEndPoint)instance.Client.RemoteEndPoint).Port);
                        binding.SetMethod("close", instance.Close);
                        binding.SetFunction<string>("readString", () => {
                            byte[] b = new byte[instance.Available];
                            instance.GetStream().Read(b, 0, b.Length);
                            return Encoding.UTF8.GetString(b);
                        });
                        binding.SetMethod<string, bool>("sendFile", (file, isBigFile) => { instance.Client.SendFile(file, null, null, isBigFile ? System.Net.Sockets.TransmitFileOptions.UseSystemThread : System.Net.Sockets.TransmitFileOptions.UseDefaultWorkerThread); });
                    });

                    registered = true;
                }


                context.GlobalObject.WriteProperty("RequestResult", "");
                context.GlobalObject.WriteProperty("RequestHandler", req);
                context.GlobalObject.WriteProperty("RespondHandler", res);
                context.GlobalObject.WriteProperty("Client", req.Client);
                context.GlobalObject.WriteProperty("AutoFlush", true);
                context.GlobalObject.Binding.SetMethod<string>("log", (s) => Console.WriteLine(s));
                context.GlobalObject.Binding.SetMethod<string>("send", (s) => { 
                    req.Client.GetStream().Write(Encoding.UTF32.GetBytes(s), 0, s.Length);
                    if(context.GlobalObject.ReadProperty<bool>("AutoFlush"))
                        req.Client.GetStream().Flush();
                });
                context.GlobalObject.Binding.SetMethod("flush", () => req.Client.GetStream().Flush());
                context.GlobalObject.Binding.SetMethod("closeStream", () => req.Client.GetStream().Dispose());
                context.GlobalObject.Binding.SetMethod("closeConnection", () => req.Client.Close());

                try
                {
                    context.RunScript(System.IO.File.ReadAllText(file));
                }
                catch (Exception exp)
                {
                    ErrorLogger.WithTrace(req.Server.Settings, string.Format("[Warning][Backend error => RunScript()] : exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace),typeof(ChakraCoreObject));
                }

                if (res.DidRespond())
                {
                    Runtime.CollectGarbage();
                    return HttpRequestHeader.ContentSplitter;
                }

                else
                {
                    byte[] b = Encoding.UTF8.GetBytes(context.GlobalObject.ReadProperty<string>("RequestResult"));
                    Runtime.CollectGarbage();
                    return b;
                }
                
            }

                
            //context.Dispose();
        }
    }
}
