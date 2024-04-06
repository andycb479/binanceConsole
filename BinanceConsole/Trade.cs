using System.Text.Json.Serialization;

namespace BinanceConsole;

public class Trade
{
    [JsonPropertyName("e")]
    public string EventType { get; set; }

    [JsonPropertyName("E")]
    public long EventTime { get; set; }

    [JsonPropertyName("s")]
    public string Symbol { get; set; }

    [JsonPropertyName("a")]
    public long AggregateTradeId { get; set; }

    [JsonPropertyName("p")]
    public string Price { get; set; }

    [JsonPropertyName("q")]
    public string Quantity { get; set; }

    [JsonPropertyName("f")]
    public long FirstTradeId { get; set; }

    [JsonPropertyName("l")]
    public long LastTradeId { get; set; }

    [JsonPropertyName("T")]
    public long TradeTime { get; set; }

    [JsonPropertyName("m")]
    public bool IsBuyerMarketMaker { get; set; }
}