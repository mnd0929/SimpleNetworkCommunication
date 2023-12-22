using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleNetworkCommunication
{
    public class NetPingers
    {
        private List<Ping> pingers = new List<Ping>();
        private int instances = 0;

        private object @lock = new object();

        private int result = 0;

        private int ttl = 5;

        public int timeOut = 1000;

        public List<IPAddress> resultList = new List<IPAddress>();

        public List<IPAddress> GetIpAdresses()
        {
            List<IPAddress> ips = new List<IPAddress>();
            List<IPAddress> checkips = new List<IPAddress>();

            // доступно ли сетевое подключение
            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                return null;
            // запросить у DNS-сервера IP-адрес, связанный с именем узла
            var host = Dns.GetHostEntry(Dns.GetHostName());
            // Пройдем по списку IP-адресов, связанных с узлом
            foreach (var ip in host.AddressList)
            {
                // если текущий IP-адрес версии IPv4, то выведем его 
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ips.Add(ip);
                }
            }

            foreach (var ip in ips)
            {
                if (!checkips.Contains<IPAddress>(ip))
                {
                    checkips.Add(ip);
                }
            }

            foreach (var ip in checkips)
            {
                var ipsegments = ip.ToString().Split('.');
                string baseIp = $"{ipsegments[0]}.{ipsegments[1]}.{ipsegments[2]}.";

                CreatePingers(255);
                
                PingOptions po = new PingOptions(ttl, true);
                System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                byte[] data = enc.GetBytes("ping");

                SpinWait wait = new SpinWait();
                int cnt = 1;

                Stopwatch watch = Stopwatch.StartNew();

                foreach (Ping p in pingers)
                {
                    lock (@lock)
                    {
                        instances += 1;
                    }

                    p.SendAsync(string.Concat(baseIp, cnt.ToString()), timeOut, data, po);
                    cnt += 1;
                }

                while (instances > 0)
                {
                    wait.SpinOnce();
                }

                watch.Stop();

                DestroyPingers();
            }

            return resultList;
        }

        public void Ping_completed(object s, PingCompletedEventArgs e)
        {
            lock (@lock)
            {
                instances -= 1;
            }

            if (e.Reply.Status == IPStatus.Success)
            {
                resultList.Add(e.Reply.Address);
                result += 1;
            }
            else
            {
                //Console.WriteLine(String.Concat("Non-active IP: ", e.Reply.Address.ToString()))
            }
        }

        private void CreatePingers(int cnt)
        {
            for (int i = 1; i <= cnt; i++)
            {
                Ping p = new Ping();
                p.PingCompleted += Ping_completed;
                pingers.Add(p);
            }
        }

        private void DestroyPingers()
        {
            foreach (Ping p in pingers)
            {
                p.PingCompleted -= Ping_completed;
                p.Dispose();
            }

            pingers.Clear();

        }
    }
}
