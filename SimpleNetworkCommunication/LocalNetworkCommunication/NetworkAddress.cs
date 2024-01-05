using System.Net;

namespace SimpleNetworkCommunication
{
    /// <summary>
    /// Представляет информацию для подключения к внешней машине
    /// </summary>
    public class NetworkAddress
    {
        public IPAddress IP { get; set; }
        public int Port { get; set; }
        public NetworkAddress() 
        {
            AutoSelect();
        }
        public NetworkAddress(IPAddress iPAddress, int port)
        {
            IP = iPAddress;
            Port = port;
        }
        private void AutoSelect()
        {
            NetworkScanner networkScanner = new NetworkScanner();
            NetworkAddress networkAddress = null;
            networkScanner.UserSelectedDevice += (_e) => 
            {
                networkAddress = _e;
            };
            networkScanner.ShowDialog();
            
            IP = networkAddress.IP;
            Port = networkAddress.Port;
        }
    }
}
