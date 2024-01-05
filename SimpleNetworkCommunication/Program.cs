using ips;
using SimpleTCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
            while (true)
            {
                Console.WriteLine($"\r\n ** SIMPLE TCP CHAT v{ver} **" +
                               "\r\n 1. Децентралзованная сеть (Только 2 участника чата)" +
                               "\r\n 2. Централизованная сеть (Неограниченное колличество участников)");

                try
                {
                    if (Console.ReadKey().Key == ConsoleKey.D1)
                    {
                        Console.Clear();

                        Client client = new Client(new NetworkAddress());

                        ColorConsole.WriteLine($"Вы вошли в чат как {client.Role}", ConsoleColor.Blue);

                        if (client.Role == NetRole.Server)
                        {
                            client.Connected += (_c) =>
                            {
                                Print($"Клиент подключен", ConsoleColor.Blue);
                            };

                            client.ScReceivedMessage += (_m) =>
                            {
                                Print($"Client > {_m}");
                            };

                            while (true)
                            {
                                Console.Write($"{client.Role} > ");

                                try
                                {
                                    client.Send(Console.ReadLine());
                                }
                                catch
                                {
                                    ColorConsole.WriteLine("Ошибка отправки: 0x");
                                }
                            }
                        }
                        else
                        {
                            // Клиент

                            client.ScReceivedMessage += (_m) =>
                            {
                                Print($"Server > {_m}");
                            };

                            while (true)
                            {
                                Console.Write($"{client.Role} > ");
                                client.Send(Console.ReadLine());
                            }
                        }
                    }
                    else
                    {
                        Console.Clear();

                        Client client = new Client(new NetworkAddress());

                        if (client.Role == NetRole.Server)
                        {
                            Console.Write($"\r\nВведите ник: ");
                            string name = Console.ReadLine() + " (Server)";

                            Console.WriteLine($"Вы вошли в чат как {name}\r\n");

                            client.Connected += (_c) =>
                            {
                                var ep = _c.Client.RemoteEndPoint;
                                Print($"Клиент {ep} вошел в чат", ConsoleColor.Blue);
                                client.Send($"Клиент {ep} вошел в чат");
                            };

                            client.Disconnected += (_c) =>
                            {
                                var ep = _c.Client.RemoteEndPoint;
                                Print($"Клиент {ep} покинул чат", ConsoleColor.Blue);
                                client.Send($"Клиент {ep} покинул чат");
                            };

                            client.ScReceivedMessage += (_m) =>
                            {
                                Print($"{_m}");
                                client.Send($"{_m}");
                            };

                            while (true)
                            {
                                Console.Write($"{name} > ");
                                client.Send($"{name} > {Console.ReadLine()}");
                            }
                        }
                        else
                        {
                            // Клиент

                            Console.Write($"\r\nВведите ник: ");
                            string name = Console.ReadLine();

                            Console.WriteLine($"Вы вошли в чат как {name}\r\n");

                            client.ScReceivedMessage += (_m) =>
                            {
                                if (!_m.StartsWith(name))
                                    Print($"{_m}");
                            };

                            while (true)
                            {
                                Console.Write($"{name} > ");
                                client.Send($"{name} > {Console.ReadLine()}");
                            }
                        }
                    }
                }
                catch { }
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
