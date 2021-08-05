using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Modetor.Net.Server.Core.Backbone
{
    public class Broadcast
    {
        public static readonly string PUBLIC_TIME_CHANNEL = "public_time_channel";

        public object @locker = new();
        public ControlSignal RemoveSignal;
        
        public void Receive(string name, object values)
        {
            Receive(name, values, true);
        }
        public void Receive(string name, object values, bool autoRemove)
        {
            if (!P_work.ContainsKey(name)) return;
            else
            {
                for(int i = P_work[name].Count-1; i > -1; i--)
                {
                    lock(@locker)
                    {
                        Server.OnBroadcast(P_work[name][i], values);
                        if (RemoveSignal.Equals(ControlSignal.ON))
                            P_work[name].RemoveAt(i);
                    }
                }
                if (autoRemove)
                    P_work.Remove(name);

            }
        }
        public void Register(string name, Action<object> action)
        {
            if (P_work.ContainsKey(name))
                P_work[name].Add(action);
            else
                P_work.Add(name, new List<Action<object>> { action });
        }
        public void Register(string name)
        {
            if (!P_work.ContainsKey(name))
                P_work.Add(name, new List<Action<object>>());
        }
        public Broadcast(HttpServers.BaseServer server)
        {
            P_work = new Dictionary<string, List<Action<object>>>();
            Server = server;
        }

        public void Remove(string name, int index)
        {
            if (P_work.ContainsKey(name)) {
                if (index < 0 || index >= P_work[name].Count) return;
                P_work[name].RemoveAt(index);
                if (P_work[name].Count == 0)
                    P_work.Remove(name);
            }
        }
        public void Remove(string name)
        {
            if (P_work.ContainsKey(name))
                P_work.Remove(name);
        }

        private Dictionary<string, List<Action<object>>> P_work;
        private readonly HttpServers.BaseServer Server;

        internal int GetActionsCount(string name)
        {
            if (P_work.ContainsKey(name))
                return P_work[name].Count;
            return -1;
        }
    }
}
