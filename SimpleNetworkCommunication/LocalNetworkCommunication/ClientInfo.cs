using SimpleTCP;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading;

namespace SimpleNetworkCommunication
{
    public class ClientInfo
    {
        public string IP { get; private set; }
        public string Port { get; private set; }
        public string NetRole { get; private set; }
        public bool isActive = false;
        public void GetClientInfo(string ip, string hostname)
        {
            Client client = new Client(null, false);

            for (int i = 0; i < client.InformationServerNumberConnectionAttempts; i++)
            {
                try
                {
                    bool isDataReceived = false;

                    SimpleTcpClient simpleTcpClient = new SimpleTcpClient();
                    simpleTcpClient.DataReceived += (_s, _e) =>
                    {
                        string resstr = Regex.Match(_e.MessageString, @"(?<=<com>)(.*)(?=</com>)").ToString();
                        isActive = true;
                        Port = resstr.Split('\\')[0];
                        NetRole = resstr.Contains("Server") ? "Server" : "Client";

                        isDataReceived = true;
                    };
                    simpleTcpClient.Connect(ip, client.ClientInfoPort - i);

                    for (int j = 0; j < client.NumberOfWaitsForInformationServerResponse; j++)
                    {
                        Thread.Sleep(100);

                        if (isDataReceived)
                            break;
                    }

                    break;
                }
                catch { }
            }
        }

        public string GetMashineInfo(string host)
        {
            //string acc;
            //string os;
            //string board;
            //string biosVersion;
            string temp = null;

            //Find system information using Win32 classes
            //https://msdn.microsoft.com/en-us/ie/aa394084%28v=vs.94%29?f=255&MSPPError=-2147217396
            string[] searchClass = { "Win32_ComputerSystem", "Win32_OperatingSystem", "Win32_ComputerSystem", "Win32_ComputerSystem", "Win32_DesktopMonitor" }; //Class type
            string[] param = { "UserName", "Caption", "SystemType", "Domain", "Caption" }; //Parameter within class

            //Iterate through Win32 classes and query system info
            for (int i = 0; i <= searchClass.Length - 1; i++)
            {
                try
                {
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher("\\\\" + host + "\\root\\CIMV2", "SELECT *FROM " + searchClass[i]);
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        //Add system info to dialog box
                        temp += obj.GetPropertyValue(param[i]).ToString() + "\n";
                        if (i == searchClass.Length - 1)
                        {
                            return temp;
                        }
                    }
                }
                catch
                {
                    return "No information";
                }
            }

            return "No information";
        }
    }
}
