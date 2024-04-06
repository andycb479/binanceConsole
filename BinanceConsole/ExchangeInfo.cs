using System.Text.Json.Serialization;

namespace BinanceConsole;

public sealed class ExchangeInfo
{
    [JsonPropertyName("symbols")]
    public List<ExchangeSymbol> Symbols { get; set; }
}