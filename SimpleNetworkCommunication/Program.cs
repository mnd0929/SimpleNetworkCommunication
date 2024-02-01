using ips;
using SimpleTCP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleNetworkCommunication
{
    internal class Program
    {
        public static int ver = 3;
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            while (true)
            {
                Console.Clear();
                Client client = new Client(new NetworkAddress(), false);

                client.Logs = true;
                client.NewLogReceived += (m) =>
                {
                    Print("System: " + m);
                };

                client.ScReceivedMessage += (m) =>
                {
                    Print("\r\n>> " + m);
                };

                client.Start();

                ColorConsole.WriteLine($"Вы подключились к {client.NetworkAddress.IP + ":" + client.NetworkAddress.Port} как {client.Role}", ConsoleColor.Blue);

                while (true)
                {
                    Console.Write("> ");
                    client.Send(Console.ReadLine());
                }
            }
        }

        public static string RunCommand(string arguments, bool readOutput)
        {
            var output = string.Empty;
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/C " + arguments,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = false
                };

                var proc = Process.Start(startInfo);

                if (readOutput)
                {
                    output = proc.StandardOutput.ReadToEnd();
                }

                proc.WaitForExit();

                return output;
            }
            catch (Exception)
            {
                return output;
            }
        }
        // чтобы полученное сообщение не накладывалось на ввод нового сообщения
        public static void Print(string message, ConsoleColor foreColor = ConsoleColor.Gray)
        {
            int initialCursorTop = Console.CursorTop;
            int initialCursorLeft = Console.CursorLeft;

            Console.MoveBufferArea(0, initialCursorTop, Console.WindowWidth,
                1, 0, initialCursorTop + 1);
            Console.CursorTop = initialCursorTop;
            Console.CursorLeft = 0;

            // Print the message here
            ColorConsole.WriteLine(message, foreColor);

            Console.CursorTop = initialCursorTop + 1;
            Console.CursorLeft = initialCursorLeft;
        }
    }
}
