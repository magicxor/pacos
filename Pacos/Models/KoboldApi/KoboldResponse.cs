using System.Text.Json.Serialization;

namespace Pacos.Models.KoboldApi;

public class Result
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class KoboldResponse
{
    [JsonPropertyName("results")]
    public List<Result>? Results { get; set; }
}
