using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;

namespace BinanceConsole;

internal class Program
{
    private static readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(1);

    private const int MAX_TRADES_TO_KEEP = 10000;

    private const ConsoleColor BUY_COLOR = ConsoleColor.Green;
    private const ConsoleColor SELL_COLOR = ConsoleColor.Red;

    private static readonly ConcurrentDictionary<string, (ConcurrentQueue<string> Storage, ConcurrentQueue<string> Display)> _tradeData = new();

    private static void Main(string[] args)
    {
        var symbols = GetBinanceSymbols();
        Console.Clear();

        if (symbols.Count == 0)
        {
            return;
        }

        Console.WriteLine("Binance SPOT symbols:");
        Console.WriteLine(string.Join(", ", symbols));

        Console.WriteLine("\nEnter the trade pairs (ex BNB/USDT, ETH/BTC)");
        string? input = Console.ReadLine();
        string[]? pairs = input?.Split(',');

        if (pairs is null)
        {
            return;
        }

        for (int i = 0; i < pairs.Length; i++)
        {
            var tradePair = pairs[i].Trim().ToLower();

            if (!_tradeData.TryAdd(tradePair, (new ConcurrentQueue<string>(), new ConcurrentQueue<string>())))
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

    private static List<string> GetBinanceSymbols()
    {
        Console.WriteLine("Getting Binance exchange info...");

        string url = "https://api.binance.com/api/v3/exchangeInfo";
        HttpWebRequest request = WebRequest.CreateHttp(url);
        request.Method = "GET";

        using HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        using Stream responseStream = response.GetResponseStream();
        using StreamReader streamReader = new StreamReader(responseStream, Encoding.UTF8);

        var jsonResponse = streamReader.ReadToEnd();
        var exchangeInfo = JsonSerializer.Deserialize<ExchangeInfo>(jsonResponse);

        if (exchangeInfo is null)
        {
            return [];
        }

        var spotSymbols = new List<string>();
        for (int i = 0; i < exchangeInfo.Symbols.Count; i++)
        {
            if (exchangeInfo.Symbols[i].Permissions.Contains("SPOT"))
            {
                spotSymbols.Add($"{exchangeInfo.Symbols[i].BaseAsset}/{exchangeInfo.Symbols[i].QuoteAsset}");
            }
        }

        return spotSymbols;
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
                ConcurrentQueue<string> pairStorage = _tradeData[pair].Storage;

                if (pairStorage.Count <= MAX_TRADES_TO_KEEP) continue;

                int tradesToDequeue = pairStorage.Count - MAX_TRADES_TO_KEEP;
                for (int i = 0; i < tradesToDequeue; i++)
                {
                    pairStorage.TryDequeue(out _);
                }

                Console.WriteLine($"CLEANED UP {tradesToDequeue} TRADES FOR {pair}");
            }
        }
    }
}