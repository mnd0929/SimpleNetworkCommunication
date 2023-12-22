using SimpleTCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleNetworkCommunication
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Client client = new Client(new NetworkAddress(true));
            client.Send("hi");

            client.ScReceivedMessage += (_m) =>
            {
                client.Send(" penus");
                Console.Write(_m);
            };

            while (true) { Thread.Sleep(1000); };
        }
    }
}
