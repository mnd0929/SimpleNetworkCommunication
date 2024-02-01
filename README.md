# SimpleNetworkCommunication (TESTS)

- Самая простая организация передачи данных по локальной сети (p2p)

Библиотека доступна [здесь](https://github.com/mnd0929/SimpleNetworkCommunicationLB) 

Создание клиента с выбором IP и порта через сетевое обнаружение:
```csharp
Client client = new Client(new NetworkAddress());
```

Создание клиента со своими IP и портом:
```csharp
Client client = new Client(new NetworkAddress(IPAddress.Parse("127.0.0.1"), 55555));
```

Отправка данных:
```csharp
client.Send("hi");
```

Получение данных:
```csharp
client.ScReceivedMessage += (_m) =>
{
    Console.Write(_m); // Выводим в консоль полученное сообщение
};
```
