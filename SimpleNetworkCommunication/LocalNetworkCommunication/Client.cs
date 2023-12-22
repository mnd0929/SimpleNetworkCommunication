using SimpleTCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleNetworkCommunication
{
    public enum NetRole 
    { 
        Server,
        Client
    }
    public class Client
    {
        SimpleTCP.SimpleTcpClient TcpClient = new SimpleTCP.SimpleTcpClient { };
        SimpleTCP.SimpleTcpServer TcpServer = new SimpleTCP.SimpleTcpServer { };
        SimpleTCP.SimpleTcpServer ClientInfoServer;
        NetworkAddress NetworkAddress;

        public delegate void MessageReceived(string message);
        public event MessageReceived ScReceivedMessage;

        NetRole Role = NetRole.Server;
        public void Send(string message)
        {
            if (Role == NetRole.Client)
                TcpClient.WriteLine($"<TcpClientLineContentZmx515>{message}</TcpClientLineContentZmx515>");

            if (Role == NetRole.Server)
                TcpServer.BroadcastLine($"<TcpClientLineContentZmx515>{message}</TcpClientLineContentZmx515>");
        }
        public void Disconnect()
        {
            if (Role == NetRole.Client)
                TcpClient.Disconnect();

            if (Role == NetRole.Server)
                TcpServer.Stop();
        }
        public void Dispose()
        {
            TcpClient.Disconnect();
            TcpClient.Dispose();
            TcpServer.Stop();
        }
        public Client(NetworkAddress networkAddress) 
        { 
            NetworkAddress = networkAddress;

            // Создание сервера ифнормации о клиенте
            for (int i = 0; i < 2; i++)
            {
                Console.WriteLine($"Создание сервера информации о клиенте: Попытка {i}, порт {4349 - i}");

                try
                {
                    ClientInfoServer = new SimpleTCP.SimpleTcpServer();
                    ClientInfoServer.ClientConnected += (_s, _e) =>
                    {
                        ClientInfoServer.BroadcastLine($"<com>{NetworkAddress.Port}\\{Role}</com>");
                    };
                    ClientInfoServer.Start(4349 - i);

                    Console.WriteLine($"Создание сервера информации о клиенте: Успешно, попытка {i}");

                    break;
                }
                catch { }
            }

            // Получение роли подключаемой машины
            for (int i = 0; i < 2; i++)
            {
                Console.WriteLine($"Получение роли подключаемой машины: Попытка {i}, порт {4349 - i}");

                try
                {
                    bool isDataReceived = false;

                    SimpleTcpClient simpleTcpClient = new SimpleTcpClient();
                    simpleTcpClient.DataReceived += (_s, _e) =>
                    {
                        string resstr = Regex.Match(_e.MessageString, @"(?<=<com>)(.*)(?=</com>)").ToString();
                        string por = resstr.Split('\\')[0];
                        string rol = resstr.Split('\\')[1];

                        Console.WriteLine($"Получены данные: {resstr}");

                        Role = NetRole.Client.ToString().Contains(rol) ? NetRole.Server : NetRole.Client;

                        Console.WriteLine($"Получение роли подключаемой машины: Успешно, попытка {i}");

                        isDataReceived = true;
                    };
                    simpleTcpClient.Connect(NetworkAddress.IP.ToString(), 4349 - i);

                    for (int j = 0; j < 5; j++)
                    {
                        Thread.Sleep(500);

                        if (isDataReceived)
                            break;
                    }

                    break;
                }
                catch { }
            }

            Console.WriteLine($"Назначенная роль: {Role}");

            if (Role == NetRole.Client)
            {
                Console.WriteLine("\rЗапуск клиента...");

                TcpClient.Connect(NetworkAddress.IP.ToString(), NetworkAddress.Port - 1);
                TcpClient.DataReceived += (_s, _e) =>
                {
                    string resstr = Regex.Match(_e.MessageString, @"(?<=<TcpClientLineContentZmx515>)(.*)(?=</TcpClientLineContentZmx515>)").ToString();
                    ScReceivedMessage(resstr);
                };

                Console.Write($"\rЗапуск клиента: Успешно, клиент запущен по адресу: {networkAddress.IP}:{networkAddress.Port}\r\n");
            }

            if (Role == NetRole.Server)
            {
                Console.Write("\rЗапуск сервера...");

                TcpServer.Start(networkAddress.Port - 1);
                TcpServer.DataReceived += (_s, _e) =>
                {
                    string resstr = Regex.Match(_e.MessageString, @"(?<=<TcpClientLineContentZmx515>)(.*)(?=</TcpClientLineContentZmx515>)").ToString();
                    ScReceivedMessage(resstr);
                };

                Console.Write($"\rЗапуск сервера: Успешно, сервер запущен по адресу: {networkAddress.IP}:{networkAddress.Port}\r\n");
            }
        }
    }
}
