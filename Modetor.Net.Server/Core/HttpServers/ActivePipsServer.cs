using System;
using System.Net.Sockets;

namespace Modetor.Net.Server.Core.HttpServers
{
    public class ActivePipsServer : BaseServer
    {
        public ActivePipsServer(string ip, int port) : base(ip, port) { }


        protected override void OnStarted()
        {
            if (Pips == null)
                Pips = new ActivePips((int)Settings.Current.PipsCount);

        }

        protected override void OnResumed()
        {
            Pips?.Resume();
            base.OnResumed();
        }
        protected override void OnSuspended()
        {
            Pips?.Suspend();
            base.OnSuspended();
        }

        ~ActivePipsServer() {
            Pips?.Kill();
        }
        protected override void OnShutteddown()
        {
            Pips?.Kill();
            base.OnShutteddown();
        }
        protected internal override void OnRecieveRequest(TcpClient client)
        {
            Pips?.AddWork(async () =>
            {
                if (client.Connected)
                    await ProcessRequest(client);
                else
                    client.Close();
            });
        }
        protected internal override void OnBroadcast(Action<object> action, object obj)
        {
            Pips?.AddWork(() => action?.Invoke(obj));
        }

        private ActivePips Pips;
    }
}
