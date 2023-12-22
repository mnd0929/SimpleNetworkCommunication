using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNetworkCommunication
{
    public class NetworkAddress
    {
        public IPAddress IP { get; set; }
        public int Port { get; set; }
        public NetworkAddress(bool AutomaticUserSelection) 
        {
            AutoSelect();
        }
        private async void AutoSelect()
        {
            NetworkScanner networkScanner = new NetworkScanner();
            NetworkAddress networkAddress = null;
            networkScanner.UserSelectedDevice += (_e) => 
            {
                networkAddress = _e as NetworkAddress;
            };
            networkScanner.ShowDialog();
            
            IP = networkAddress.IP;
            Port = networkAddress.Port;
        }
        public NetworkAddress(IPAddress iPAddress, int port)
        {
            IP = iPAddress;
            Port = port;
        }
    }
}
