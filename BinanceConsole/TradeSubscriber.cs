using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace BinanceConsole;

public class TradeSubscriber(string tradePair, ConcurrentDictionary<string, (Queue<string> Storage, Queue<string> Display)> tradeData)
{
    private readonly string _tradePair = tradePair;
    private readonly ConcurrentDictionary<string, (Queue<string> Storage, Queue<string> Display)> _tradeData = tradeData;
    private const string BINANCE_WEB_SOCKET_URL = "wss://fstream.binance.com/ws/";

    public void SubscribeToTrade()
    {
        var thread = new Thread(Subscribe);
        thread.Start();
    }

    private async void Subscribe()
    {
        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri($"{BINANCE_WEB_SOCKET_URL}{_tradePair.Replace("/", "")}@trade"), CancellationToken.None);
        var buffer = new byte[256];

        while (ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
            else
            {
                EnqueueMessage(buffer, result.Count);
            }
        }
    }

    private void EnqueueMessage(byte[] buffer, int count)
    {
        string data = Encoding.UTF8.GetString(buffer, 0, count);
        _tradeData[_tradePair].Storage.Enqueue(data);
        _tradeData[_tradePair].Display.Enqueue(data);
    }
}