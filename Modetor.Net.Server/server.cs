using System;
using IronPython.Runtime;
namespace Modetor.Net.Server
{
    public class Server
    {
        public Server()
        {
            
        }
        public void SetAddress(string address)
        {
            if (address == null) throw new ArgumentNullException("address is null");
            Address = address;
        } 



        public string Address { get; private set; }
    }
}
