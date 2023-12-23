using SimpleTCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleNetworkCommunication
{
    /// <summary>
    /// Сетевая роль
    /// </summary>
    public enum NetRole 
    { 
        Server,
        Client
    }
    public class Client
    {
        /// <summary>
        /// Клиент (НЕ null ЕСЛИ Role == Client)
        /// </summary>
        public SimpleTCP.SimpleTcpClient TcpClient = new SimpleTCP.SimpleTcpClient { };

        /// <summary>
        /// Сервер (НЕ null ЕСЛИ Role == Server)
        /// </summary>
        public SimpleTCP.SimpleTcpServer TcpServer = new SimpleTCP.SimpleTcpServer { };

        /// <summary>
        /// Сервер с информацией о текущей машине
        /// </summary>
        public SimpleTCP.SimpleTcpServer ClientInfoServer;

        /// <summary>
        /// Указывает колличество ожиданий ответа информационного сервера (по 200мс)
        /// </summary>
        public int NumberOfWaitsForInformationServerResponse = 4;

        /// <summary>
        /// Указывает число попыток подключения к портам информационного сервера (ClientInfoPort - InformationServerNumberConnectionAttempts)
        /// </summary>
        public int InformationServerNumberConnectionAttempts = 2;

        /// <summary>
        /// Порт использующийся для получения информации о текущей машине
        /// </summary>
        public int ClientInfoPort = 4349;

        /// <summary>
        /// Данные для подключения к внешней машине
        /// </summary>
        public NetworkAddress NetworkAddress;

        /// <summary>
        /// Событие, возникающее когда сервер / клиент получает сообщение
        /// </summary>
        public event MessageReceived ScReceivedMessage;
        public delegate void MessageReceived(string message);

        /// <summary>
        /// Автоматическое определение роли (клиент / сервер) в зависимости от полученной информации с информационного сервера внешней машины
        /// </summary>
        public bool AutoRole = true;

        /// <summary>
        /// Сетевая роль (Клиент / Сервер)
        /// </summary>
        public NetRole Role = NetRole.Server;

        /// <summary>
        /// Ключ использующийся для передачи данных (На всех машинах в сети он должен быть одинаковым)
        /// </summary>
        public string Key = "TcpClientLineContentZmx515";

        /// <summary>
        /// Отправка сообщения подключенной машине
        /// </summary>
        /// <param name="message">Сообщение в кодировке UTF-8</param>
        public void Send(string message)
        {
            if (Role == NetRole.Client)
                TcpClient.WriteLine($"<{Key}>{message}</{Key}>");

            if (Role == NetRole.Server)
                TcpServer.BroadcastLine($"<{Key}>{message}</{Key}>");
        }

        /// <summary>
        /// Выключение клиента / сервера
        /// </summary>
        public void Disconnect()
        {
            if (Role == NetRole.Client)
            {
                TcpClient.Disconnect();
                TcpClient.Dispose();
            }

            if (Role == NetRole.Server)
                TcpServer.Stop();
        }

        /// <summary>
        /// Клиент / Сервер
        /// </summary>
        /// <param name="networkAddress">IP и порт машины для подключения</param>
        public Client(NetworkAddress networkAddress) 
        { 
            NetworkAddress = networkAddress;

            if (AutoRole)
            {
                // Создание сервера ифнормации о клиенте
                for (int i = 0; i < InformationServerNumberConnectionAttempts; i++)
                {
                    Console.WriteLine($"Создание сервера информации о клиенте: Попытка {i}, порт {ClientInfoPort - i}");

                    try
                    {
                        ClientInfoServer = new SimpleTCP.SimpleTcpServer();
                        ClientInfoServer.ClientConnected += (_s, _e) =>
                        {
                            ClientInfoServer.BroadcastLine($"<com>{NetworkAddress.Port}\\{Role}</com>");
                        };
                        ClientInfoServer.Start(ClientInfoPort - i);

                        Console.WriteLine($"Создание сервера информации о клиенте: Успешно, попытка {i}");

                        break;
                    }
                    catch { }
                }

                // Получение роли подключаемой машины
                for (int i = 0; i < InformationServerNumberConnectionAttempts; i++)
                {
                    Console.WriteLine($"Получение роли подключаемой машины: Попытка {i}, порт {ClientInfoPort - i}");

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
                        simpleTcpClient.Connect(NetworkAddress.IP.ToString(), ClientInfoPort - i);

                        for (int j = 0; j < NumberOfWaitsForInformationServerResponse; j++)
                        {
                            Thread.Sleep(200);

                            if (isDataReceived)
                                break;
                        }

                        break;
                    }
                    catch { }
                }

                Console.WriteLine($"Назначена роль: {Role}");
            }

            if (Role == NetRole.Client)
            {
                Console.WriteLine("\rЗапуск клиента:");

                TcpClient.Connect(NetworkAddress.IP.ToString(), NetworkAddress.Port - 1);
                TcpClient.DataReceived += (_s, _e) =>
                {
                    string resstr = Regex.Match(_e.MessageString, $@"(?<=<{Key}>)(.*)(?=</{Key}>)").ToString();
                    ScReceivedMessage(resstr);
                };

                Console.Write($"\rЗапуск клиента: Успешно, клиент запущен по адресу: {networkAddress.IP}:{networkAddress.Port}\r\n");
            }

            if (Role == NetRole.Server)
            {
                Console.Write("\rЗапуск сервера:");

                TcpServer.Start(networkAddress.Port - 1);
                TcpServer.DataReceived += (_s, _e) =>
                {
                    string resstr = Regex.Match(_e.MessageString, $@"(?<=<{Key}>)(.*)(?=</{Key}>)").ToString();
                    ScReceivedMessage(resstr);
                };

                Console.Write($"\rЗапуск сервера: Успешно, сервер запущен по адресу: {networkAddress.IP}:{networkAddress.Port}\r\n");
            }
        }
    }
}
