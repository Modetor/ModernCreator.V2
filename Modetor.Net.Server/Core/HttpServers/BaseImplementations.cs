using Modetor.Net.Server.Core.Backbone;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Modetor.Net.Server.Core.HttpServers
{
    public abstract partial class BaseServer
    {


        private static byte[] IntToByteArray(ushort v)
        {
            byte[] arr = BitConverter.GetBytes(v);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(arr);
            }

            return arr;
        }
        
        private static int GetWebSocketHeader(int ff, int opcode, int len)
        {
            int h = ff; // ? 1 : 0; // fin, 0=more frames, 1=last frame
            h = (h << 1) + 0; // rsv1
            h = (h << 1) + 0; // rsv2
            h = (h << 1) + 0; // rsv3
            h = (h << 4) + opcode; // (cf ? 0 : opcode); // opcode, 0=contenuation, 1= text, 2=binary
            h = (h << 1) + 0; // mask.. we don't do that here
            h = (h << 7) + len; // message len
            return h;


            #region Draft 1
            /*private static int GetHeader(bool ff, bool cf, int opcode)
            {
                int h = ff ? 1 : 0; // fin, 0=more frames, 1=last frame
                h = (h << 1) + 0; // rsv1
                h = (h << 1) + 0; // rsv2
                h = (h << 1) + 0; // rsv3
                h = (h << 4) + (cf ? 0 : opcode); // opcode, 0=contenuation, 1= text, 2=binary
                h = (h << 1) + 0; // mask.. we don't do that here

                return h;
            }*/
            #endregion
        }
        public static async void SendWebSocketMessage(NetworkStream stream, byte[] msg, int opcode)
        {
            if (stream == null) return;
            byte[][] x = msg.SplitInChunks(123).ToArray();
            for (int i = 0; i < x.Length; i++)
            {
                try
                {
                    int h = GetWebSocketHeader(i == x.Length - 1 ? 1 : 0, i == 0 ? opcode : 0, x[i].Length);
                    await stream?.WriteAsync(IntToByteArray((ushort)h), 0, 2);
                    await stream?.WriteAsync(x[i], 0, x[i].Length);
                    await stream?.FlushAsync();
                }
                catch (TimeoutException exp)
                {
                    ErrorLogger.WithTrace(string.Format("[Warning][Server error => SendWebSocketMessage()] : type: TimeoutException. exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), typeof(BaseServer));
                }
                catch (System.IO.IOException exp)
                {
                    ErrorLogger.WithTrace(string.Format("[Warning][Server error => SendWebSocketMessage()] : type: IOException. exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), typeof(BaseServer));
                }
                catch (SocketException exp)
                {
                    ErrorLogger.WithTrace(string.Format("[Warning][Server error => SendWebSocketMessage()] : type: SocketException. exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), typeof(BaseServer));
                }
                catch (ObjectDisposedException exp)
                {
                    ErrorLogger.WithTrace(string.Format("[Warning][Server error => SendWebSocketMessage()] : type: OjectDisposedException. exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), typeof(BaseServer));
                }
                catch (Exception exp)
                {
                    ErrorLogger.WithTrace(string.Format("[Warning][Server error => SendWebSocketMessage()] : type: Exception. exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), typeof(BaseServer));
                }
            }



            #region Draft 1
            /*Queue<byte[]> que = new Queue<byte[]>(msg.SplitInChunks(123));
            int len = que.Count;

            while (que.Count > 0)
            {//GetHeader(que.Count > 1 ? false : true, que.Count == len ? false : true, opcode);
                int h = GetHeader(que.Count > 1 ? false : true, que.Count == 1 ? false : true, opcode);
                byte[] l = que.Dequeue();
                h = (h << 7) + l.Length;
                await stream.WriteAsync(IntToByteArray((ushort)h), 0, 2);
                await stream.WriteAsync(l, 0, l.Length);
                //await stream.FlushAsync();
            }
            await stream.FlushAsync();*/
            #endregion
        }


        private async void ProcessWebsocketRequest(TcpClient client, HttpRequestHeader req)
        {
            try
            {
                int REPO_IDLE_TIMEOUT = req.Repository.WebSocketIdelTimeout;
                int CHANCES = req.Repository.WebSocketIdelChances;
                if (CHANCES == -1)
                {
                    CHANCES = int.MinValue;
                }

                NetworkStream stream = client.GetStream();
                const string WEBSOCKET_GUID = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
                int SEC_WEBSOCKET_VERSION = int.Parse(req.HeaderKeys["Sec-WebSocket-Version"]);
                string SEC_WEBSOCKET_KEY = req.HeaderKeys["Sec-WebSocket-Key"];

                IronPythonObject ipy = PythonRunner.InitializeForWebsocket(req);

                // فقط لهذا الإصدار
                if (SEC_WEBSOCKET_VERSION == 13)
                {
                    #region Handshake
                    byte[] SWKA = System.Security.Cryptography.SHA1.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(SEC_WEBSOCKET_KEY + WEBSOCKET_GUID));
                    string Base64Sha1Key = Convert.ToBase64String(SWKA);

                    HttpRespondHeader resh = new HttpRespondHeader();
                    resh.SetState(req.HttpVersion, HttpRespondState.SWITCHING_PROTOCOLS);
                    resh.AddHeader("Server", HttpRespondHeader.CurrentServerVersion.ToString());
                    resh.AddHeader("Connection", "Upgrade");
                    resh.AddHeader("Upgrade", "websocket");
                    resh.AddHeader("Sec-WebSocket-Accept", Base64Sha1Key + "\r\n");
                    await stream.WriteAsync(resh.GetHeaderBytes());
                    await stream.FlushAsync();
                    #endregion

                    List<byte[]> Buffer = new List<byte[]>();
                    string LastMessage = string.Empty;

                    while (!IsSuspended && Active && client.Connected)
                    {
                        #region Idle section
                        if (!stream.DataAvailable)
                        {
                            if (!client.Connected)
                            {
                                return;
                            }
                            else
                            {
                            SPIN_STATE:
                                if (!System.Threading.SpinWait.SpinUntil(() => stream.DataAvailable && client.Available != 0, REPO_IDLE_TIMEOUT))
                                {
                                    if (CHANCES == int.MinValue)
                                    {
                                        goto SPIN_STATE;
                                    }
                                    else if (CHANCES == 0)
                                    {
                                        await CloseConnection(stream, client);
                                        client.Close();
                                        return;
                                    }
                                    else
                                    {
                                        CHANCES--;
                                    }
                                }

                            }
                        }
                        #endregion

                        byte[] buffer = new byte[client.Available];

                        if (await stream.ReadAsync(buffer, 0, buffer.Length) == 0)
                        {
                            continue;
                        }

                    LOOP:

                        if (buffer.Length == 0)
                        {
                            continue;
                        }

                        #region IF CLIENT WANTS TO CLOSE THE CONNECION
                        if (buffer[0] == 0b10001000)
                        {
                            await CloseConnection(stream, client);
                            client.Close();
                            return;
                        }
                        #endregion


                        bool FIN = (buffer[0] & 0b10000000) != 0; //(buffer[0] & 0b10000000) != 0;
                        bool MASK = (buffer[1] & 0b10000000) != 0;

                        int OPCODE = buffer[0] & 0b00001111;
                        int MESSAGE_LEN = buffer[1] & 0b01111111; // - 128
                        int OFFSET = 2;

                        if (MESSAGE_LEN == 0)
                        {
                            continue;
                        }
                        else if (MESSAGE_LEN == 126)
                        {
                            MESSAGE_LEN = BitConverter.ToUInt16(new byte[] { buffer[3], buffer[2] }, 0);
                            OFFSET = 4;
                        }
                        else if (MESSAGE_LEN == 127)
                        {
                            MESSAGE_LEN = (int)BitConverter.ToUInt64(new byte[] { buffer[9], buffer[8], buffer[7], buffer[6], buffer[5], buffer[4], buffer[3], buffer[2] }, 0);
                            OFFSET = 10;
                        }


                        if (!MASK) { await CloseConnection(stream, client); return; }
                        else if (MASK)
                        {
                            byte[] decoded = new byte[MESSAGE_LEN];
                            byte[] masks = new byte[4] { buffer[OFFSET], buffer[OFFSET + 1], buffer[OFFSET + 2], buffer[OFFSET + 3] };
                            OFFSET += 4;

                            for (int i = 0; i < MESSAGE_LEN; ++i)
                            {
                                decoded[i] = (byte)(buffer[OFFSET + i] ^ masks[i % 4]);
                            }

                            buffer = buffer[(MESSAGE_LEN + OFFSET)..];

                            Buffer.Add(decoded);

                            // IF DATA FRAME DOESN'T CONTAIN THE WHOLE DATA
                            if (OPCODE == 0)
                                goto LOOP;
                            else
                            {
                                byte[] message = Buffer.SelectMany(a => a).ToArray();

                                PythonRunner.WebsocketRun(ipy, message, OPCODE, LastMessage);

                                if (OPCODE == 1)
                                    LastMessage = Encoding.UTF8.GetString(message);

                                Buffer.Clear();

                                if (buffer.Length == 0)
                                    continue;
                                else
                                    goto LOOP;
                            }
                        }


                    } // while-loop end

                }
                else
                {
                    // الإصدار غير معروف
                    HttpRespondHeader resh = new HttpRespondHeader();
                    resh.SetState(req.HttpVersion, HttpRespondState.UPGRADE_REQUIRED);
                    resh.AddHeader("Server", HttpRespondHeader.CurrentServerVersion.ToString());
                    resh.AddHeader("Sec-WebSocket-Version", "13");
                    await stream.WriteAsync(resh.GetHeaderBytes());
                    await stream.FlushAsync();
                    await stream.DisposeAsync();
                    client.Close();
                    return;
                }

            }
            catch (Exception exp)
            {
                ErrorLogger.WithTrace(Settings, string.Format("[Warning][Server error => ProcessWebsocketRequest()] : exception-message : {0}.\nstacktrace : {1}\n", exp.Message, exp.StackTrace), GetType());
            }
            
            
        }
        private async System.Threading.Tasks.Task CloseConnection(NetworkStream stream, TcpClient client)
        {
            await stream.WriteAsync(new byte[] { 136, 130 });
            await stream.FlushAsync();

            string[] keys = ClientDictionary.Keys.ToArray();
            for(int j = 0; j < keys.Length; j++)
            {
                for (int i = 0; i < ClientDictionary[keys[j]].Count; i++)
                {
                    if (ClientDictionary[keys[j]][i].Equals(client))
                    {
                        ClientDictionary[keys[j]].RemoveAt(i);
                        break;
                    }
                }

                if (ClientDictionary[keys[j]].Count == 0)
                {
                    ClientDictionary.Remove(keys[j]);
                }
            }

            await stream.DisposeAsync();
        }

        public void AddToClientDictionary(string key, TcpClient client)
        {

            lock(ClientDictionary)
            {
                if (ClientDictionary.ContainsKey(key))
                {
                    if (!ClientDictionary[key].Contains(client))
                    {
                        ClientDictionary[key].Add(client);
                    }
                }
                else
                {
                    ClientDictionary.Add(key, new List<TcpClient>() { client });
                }
            }
            
        }
        public void RemoveFromClientDictionary(string key, TcpClient client)
        {
            lock(ClientDictionary)
            {
                if (ClientDictionary.ContainsKey(key))
                {
                    if (ClientDictionary[key].Contains(client))
                        ClientDictionary[key].Remove(client);

                    if (ClientDictionary[key].Count == 0)
                        ClientDictionary.Remove(key);
                }
            }
            
        }

        private void SendBadRequestAndClose(TcpClient client, HttpRequestHeader req, NetworkStream stream, string status_code)
        {
            HttpRespondHeader resh = new HttpRespondHeader();
            resh.SetState(req.HttpVersion, status_code);
            resh.AddHeader("Server", HttpRespondHeader.CurrentServerVersion.ToString());
            stream.Write(resh.GetHeaderBytes());
            stream.Flush();
            stream.Dispose();
            client.Close();
        }
        private void SendBadRequestAndClose(TcpClient client, HttpRequestHeader req, NetworkStream stream)
                => SendBadRequestAndClose(client, req, stream, HttpRespondState.BAD_REQUEST);
        
        
        private async void Respond_OK(HttpRequestHeader req, NetworkStream stream, TcpClient client)
        {
            if (req.Repository.HasConnectionHandler)
            {
                PythonRunner.Run(req, new HttpRespondHeader(), true);
                await stream.DisposeAsync();
                client.Close();
            }
            else
            {
                if (req.IsWebsocketUpgradRequest())
                {
                    if (req.Repository.AllowWebSocket)
                    {
                        if (req.AbsoluteFilePath.EndsWith(".py"))
                        {
                            new System.Threading.Thread(() => {
                                ProcessWebsocketRequest(client, req);
                                GC.Collect();
                            })
                            { IsBackground = true, Priority = System.Threading.ThreadPriority.Lowest }.Start();
                        }
                        else
                            SendBadRequestAndClose(client, req, stream);

                    }
                    else
                    {
                        SendBadRequestAndClose(client, req, stream);
                    }
                }
                else if(req.IsServerSentEventRequest())
                {
                    if(req.Repository.AllowServerEvent)
                    {
                        if (req.AbsoluteFilePath.EndsWith(".py"))
                        {
                            ProcessServerEventRequest(client, req);
                        }
                        else
                            SendBadRequestAndClose(client, req, stream);
                    }
                    else
                        SendBadRequestAndClose(client, req, stream);
                }
                else
                {
                    HttpRespondHeader res = HttpRespondHeader.GenerateRespond(req, this);
                    if (!res.DidRespond())
                    {
                        stream?.Write(res.Build());
                        stream?.Flush();
                    }

                    if (req.HasUploadedFiles)
                        req.DeleteUploadFiles();

                    if (client.Connected)
                    {
                        stream?.Dispose();
                        client?.Close();
                    }

                }

            }
        }



        private  void ProcessServerEventRequest(TcpClient client, HttpRequestHeader req)
        {
            if (req.Repository.ServerEventMethod == ServerEventMethod.LOOP)
            {
                new System.Threading.Thread(() =>
                {
                    while(Active)
                    {
                        PythonRunner.ServerEventRun(req);
                    }
                    GC.Collect();
                })
                { IsBackground = true, Priority = System.Threading.ThreadPriority.Lowest }.Start();
            }
            else
                PythonRunner.ServerEventRun(req);
        }
        private  void Respond_SECURITY_FAILURE(HttpRequestHeader req, NetworkStream stream, TcpClient client) => SendBadRequestAndClose(client, req, stream);
        private  void Respond_PERMISSION_FAILURE(HttpRequestHeader req, NetworkStream stream, TcpClient client) => SendBadRequestAndClose(client, req, stream, HttpRespondState.FORBIDDEN);
        private  void Respond_GENERIC_FAILURE(HttpRequestHeader req, NetworkStream stream, TcpClient client) => SendBadRequestAndClose(client, req, stream);
        private  void Respond_IO_FAILURE(HttpRequestHeader req, NetworkStream stream, TcpClient client) => SendBadRequestAndClose(client, req, stream);
        private  void Respond_PARSE_FAILURE(HttpRequestHeader req, NetworkStream stream, TcpClient client) => SendBadRequestAndClose(client, req, stream, HttpRespondState.INTERNAL_SERVER_ERROR);
        private  void Respond_PayloodTooLarge(HttpRequestHeader req, NetworkStream stream, TcpClient client) => SendBadRequestAndClose(client, req, stream, HttpRespondState.PAYLOAD_TOO_LARGE);
        private  void Respond_UNKNOWN_RESOURCE_FAILURE(HttpRequestHeader req, NetworkStream stream, TcpClient client)
        { 
            void __Default_Response__(ref HttpRespondHeader res, bool state)
            {
                res.SetState(req.HttpVersion, HttpRespondState.NOT_FOUND);
                res.AddHeader("Server", HttpRespondHeader.CurrentServerVersion.ToString());
                if(state)
                    res.SetBody(Encoding.UTF8.GetBytes("Resources requested doesn't exsist anymore!"));
            }
            string FNF = Settings.Current.FileNotFoundPage.ToString();
            bool provide_content = !req.HeaderKeys.ContainsKey("Accept") ? false : req.HeaderKeys["Accept"].Contains("text/html,application/xhtml+xml,application/xml") ? true : false;
            if (FNF.Equals(string.Empty))
            {
                HttpRespondHeader res = new();
                res.AddHeader("Content-Type", "text/plain");
                
                __Default_Response__(ref res, provide_content);
                stream.Write(res.Build());
                stream.Flush();
            }
            else
            {
                req.AbsoluteFilePath = FNF;
                HttpRespondHeader res = HttpRespondHeader.GenerateRespond(req, this);
                res.SetState(req.HttpVersion, HttpRespondState.NOT_FOUND);
                if (provide_content)
                    stream.Write(res.Build());
                else
                    stream.Write(res.GetHeaderBytes());
                stream.Flush();

            }

            
            stream.Dispose();
            client.Close();
        }


        public Dictionary<string, List<TcpClient>> ClientDictionary;
        
    }
}
