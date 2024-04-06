using System.Text.Json.Serialization;

namespace BinanceConsole;

public sealed class ExchangeSymbol
{
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; }

    [JsonPropertyName("permissions")]
    public List<string> Permissions { get; set; }

    [JsonPropertyName("baseAsset")]
    public string BaseAsset { get; set; }

    [JsonPropertyName("quoteAsset")]
    public string QuoteAsset { get; set; }
}