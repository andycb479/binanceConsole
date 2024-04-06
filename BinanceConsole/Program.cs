using System.Collections.Concurrent;
using System.Text.Json;

namespace BinanceConsole;

internal class Program
{
    private static readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(1);

    private const int MAX_TRADES_TO_KEEP = 10000;

    private const ConsoleColor BUY_COLOR = ConsoleColor.Green;
    private const ConsoleColor SELL_COLOR = ConsoleColor.Red;

    private static readonly ConcurrentDictionary<string, (Queue<string> Storage, Queue<string> Display)> _tradeData = new();

    private static void Main(string[] args)
    {
        Console.WriteLine("Enter the trade pairs (ex bnb/usdt, eth/btc)");
        string? input = Console.ReadLine();
        string[]? pairs = input?.Split(',');

        if (pairs is null)
        {
            return;
        }

        for (int i = 0; i < pairs.Length; i++)
        {
            var tradePair = pairs[i].Trim().ToLower();

            if (!_tradeData.TryAdd(tradePair, (new Queue<string>(), new Queue<string>())))
            {
                continue;
            }

            var tradeSubscriber = new TradeSubscriber(tradePair, _tradeData);
            tradeSubscriber.SubscribeToTrade();
        }

        var printThread = new Thread(PrintTradeData);
        printThread.Start();

        var cleanUpThread = new Thread(CleanUp);
        cleanUpThread.Start();

        Console.ReadKey();
    }

    private static void PrintTradeData()
    {
        while (true)
        {
            for (int pairIndex = 0; pairIndex < _tradeData.Keys.Count; pairIndex++)
            {
                string pair = _tradeData.Keys.ElementAt(pairIndex);

                if (!_tradeData[pair].Display.TryDequeue(out string? jsonTrade)) continue;

                Console.WriteLine($"Trade Pair: {pair}");

                var trade = JsonSerializer.Deserialize<Trade>(jsonTrade);
                Console.ForegroundColor = trade.IsBuyerMarketMaker ? BUY_COLOR : SELL_COLOR;
                Console.WriteLine(jsonTrade);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    }

    private static void CleanUp()
    {
        while (true)
        {
            Thread.Sleep(_cleanupInterval);

            for (int pairIndex = 0; pairIndex < _tradeData.Keys.Count; pairIndex++)
            {
                string pair = _tradeData.Keys.ElementAt(pairIndex);
                Queue<string> pairStorage = _tradeData[pair].Storage;

                if (pairStorage.Count <= MAX_TRADES_TO_KEEP) continue;

                int tradesToDequeue = pairStorage.Count - MAX_TRADES_TO_KEEP;
                for (int i = 0; i < tradesToDequeue; i++)
                {
                    pairStorage.Dequeue();
                }

                Console.WriteLine($"CLEANED UP {tradesToDequeue} TRADES FOR {pair}");
            }
        }
    }
}