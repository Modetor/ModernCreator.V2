using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EmbededServer
{ 
    // MF Shit
    public class ClientsBook
    {
        private static Dictionary<int, EmbededServerInterface.Session> InternalBook = new Dictionary<int, EmbededServerInterface.Session>();

        public static EmbededServerInterface.Session GetClient(int i) => InternalBook[i];
        
    }

    
}
