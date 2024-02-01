using SimpleTCP;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;

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
        /// Клиент / Сервер
        /// </summary>
        /// <param name="networkAddress">IP и порт машины для подключения</param>
        /// <param name="quickStart">Указывает, выполняется ли автоматический запуск с параметрами по умолчанию</param>
        public Client(NetworkAddress networkAddress, bool quickStart = true)
        {
            NetworkAddress = networkAddress;
            if (quickStart) Start();
        }

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
        /// Указывает колличество ожиданий ответа информационного сервера (по 100мс)
        /// </summary>
        public int NumberOfWaitsForInformationServerResponse = 8;

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
        /// Указывает, будет ли сервер уведомлять о смене статуса инициализации. Используется событие NewLogReceived. Для корректной работы выключите QuickStart в конструкторе класса и запустите сервер после привязки к событиям
        /// </summary>
        public bool Logs = false;

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
        public string Key = "msg5";

        /// <summary>
        /// Логи сервера
        /// </summary>
        public event ProgrammedStatusChanged NewLogReceived;
        public delegate void ProgrammedStatusChanged(string status);

        /// <summary>
        /// Событие, возникающее когда сервер / клиент получает сообщение
        /// </summary>
        public event MessageReceived ScReceivedMessage;
        public delegate void MessageReceived(string message);

        /// <summary>
        /// Событие возникающее когда к серверу подключается клиент
        /// </summary>
        public event ClientConnected Connected;
        public delegate void ClientConnected(TcpClient client);

        /// <summary>
        /// Событие возникающее когда от сервера отключается клиент
        /// </summary>
        public event ClientDisconnected Disconnected;
        public delegate void ClientDisconnected(TcpClient client);

        /// <summary>
        /// Последнее полученное сообщение
        /// </summary>
        public SimpleTCP.Message lastMessage;

        /// <summary>
        /// Отправка сообщения подключенной / lastMessage машине
        /// </summary>
        /// <param name="message">Сообщение в кодировке UTF-8</param>
        /// <param name="isAnswer">Указывает, нужно ли отправить сообщение машине от которой было получено последнее сообщение (Работает только в случае отключенного p2p)</param>
        public void Send(string message, bool isAnswer = false)
        {
            if (isAnswer)
            {
                lastMessage?.ReplyLine($"<{Key}>{message}</{Key}>");
                return;
            }

            if (Role == NetRole.Client)
                TcpClient?.WriteLine($"<{Key}>{message}</{Key}>");

            if (Role == NetRole.Server)
                TcpServer?.BroadcastLine($"<{Key}>{message}</{Key}>");
        }

        /// <summary>
        /// Отправка массива байт подключенной / lastMessage машине
        /// </summary>
        /// <param name="data">Массив байт</param>
        /// <param name="isAnswer">Указывает, нужно ли отправить сообщение машине от которой было получено последнее сообщение (Работает только в случае отключенного p2p)</param>
        public void SendData(byte[] data, bool isAnswer = false)
        {
            if (isAnswer)
            {
                lastMessage?.Reply(data);
                return;
            }

            if (Role == NetRole.Client)
                TcpClient?.Write(data);

            if (Role == NetRole.Server)
                TcpServer?.Broadcast(data);
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
        /// Запускает сервер / клиент с заданными параметрами
        /// </summary>
        public void Start()
        {
            if (AutoRole)
            {
                // Создание сервера информации о клиенте
                for (int i = 0; i < InformationServerNumberConnectionAttempts; i++)
                {
                    Log($"Создание сервера информации о клиенте: Попытка {i}, порт {ClientInfoPort - i}");

                    try
                    {
                        ClientInfoServer = new SimpleTCP.SimpleTcpServer();
                        ClientInfoServer.ClientConnected += (_s, _e) =>
                        {
                            ClientInfoServer.BroadcastLine($"<com>{NetworkAddress.Port}\\{Role}</com>");
                        };
                        ClientInfoServer.Start(ClientInfoPort - i);

                        Log($"Создание сервера информации о клиенте: Успешно, попытка {i}");

                        break;
                    }
                    catch { }
                }

                // Получение роли подключаемой машины
                for (int i = 0; i < InformationServerNumberConnectionAttempts; i++)
                {
                    Log($"Получение роли подключаемой машины: Попытка {i}, порт {ClientInfoPort - i}");

                    try
                    {
                        bool isDataReceived = false;

                        SimpleTcpClient simpleTcpClient = new SimpleTcpClient();
                        simpleTcpClient.DataReceived += (_s, _e) =>
                        {
                            string resstr = Regex.Match(_e.MessageString, @"(?<=<com>)(.*)(?=</com>)").ToString();
                            string por = resstr.Split('\\')[0];
                            string rol = resstr.Split('\\')[1];

                            Log($"Получены данные: {resstr}");

                            Role = NetRole.Client.ToString().Contains(rol) ? NetRole.Server : NetRole.Client;

                            Log($"Получение роли подключаемой машины: Успешно, попытка {i}");

                            isDataReceived = true;
                        };
                        simpleTcpClient.Connect(NetworkAddress.IP.ToString(), ClientInfoPort - i);

                        for (int j = 0; j < NumberOfWaitsForInformationServerResponse; j++)
                        {
                            Thread.Sleep(100);

                            if (isDataReceived)
                                break;
                        }

                        simpleTcpClient.Disconnect();
                        simpleTcpClient.Dispose();

                        break;
                    }
                    catch { }
                }

                Log($"Назначена роль: {Role}");
            }

            if (Role == NetRole.Client)
            {
                Log("\rЗапуск клиента:");

                TcpClient.Connect(NetworkAddress.IP.ToString(), NetworkAddress.Port - 1);
                TcpClient.DataReceived += (_s, _e) =>
                {
                    foreach (string str in DecrypteString(_e.MessageString))
                    {
                        ScReceivedMessage(str);
                    }
                };

                Log($"\rЗапуск клиента: Успешно, клиент запущен по адресу: {NetworkAddress.IP}:{NetworkAddress.Port}\r\n");
            }

            if (Role == NetRole.Server)
            {
                Log("\rЗапуск сервера:");

                TcpServer.Start(NetworkAddress.Port - 1);
                TcpServer.DataReceived += (_s, _e) =>
                {
                    lastMessage = _e;
                    foreach (string str in DecrypteString(_e.MessageString))
                    {
                        ScReceivedMessage(str);
                    }
                };
                TcpServer.ClientConnected += (_s, _e) => 
                {
                    Connected(_e);
                };
                TcpServer.ClientDisconnected += (_s, _e) =>
                {
                    Disconnected(_e);
                };

                Log($"\rЗапуск сервера: Успешно, сервер запущен по адресу: {NetworkAddress.IP}:{NetworkAddress.Port}\r\n");
            }
        }

        public void Log(string msg)
        {
            try
            {
                NewLogReceived(msg);
            }
            catch 
            {
                throw new Exception("Ошибка вызова делегата ProgrammedStatusChanged. Получение данных о статусе сервера невозможно если запуск сервера происходит до создания события. Для предотвращения непредвиденного исключения попробуйте отключить QuickStart и запускать сервер вручную.");
            }
        }

        /// <summary>
        /// Получает все расшифрованные строки из текущей строки полученной из потока
        /// </summary>
        /// <param name="encryptedString">Строка из потока</param>
        /// <returns>Коллекция строк</returns>
        private List<string> DecrypteString(string encryptedString) 
        {
            List<string> result = new List<string>();
            MatchCollection matchCollection;
            matchCollection = Regex.Matches(encryptedString, $@"(?<=<{Key}>)(.*)(?=</{Key}>)");
            foreach (Match match in matchCollection)
            {
                result.Add(match.Value);
            }
            return result;
        }
    }
}
