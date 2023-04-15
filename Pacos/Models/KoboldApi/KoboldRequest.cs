using System.Text.Json.Serialization;

namespace Pacos.Models.KoboldApi;

public class KoboldRequest
{
    [JsonPropertyName("n")]
    public int N { get; set; }

    [JsonPropertyName("max_context_length")]
    public int MaxContextLength { get; set; }

    [JsonPropertyName("max_length")]
    public int MaxLength { get; set; }

    [JsonPropertyName("rep_pen")]
    public decimal RepPen { get; set; }

    [JsonPropertyName("temperature")]
    public decimal Temperature { get; set; }

    [JsonPropertyName("top_p")]
    public decimal TopP { get; set; }

    [JsonPropertyName("top_k")]
    public int TopK { get; set; }

    [JsonPropertyName("top_a")]
    public int TopA { get; set; }

    [JsonPropertyName("typical")]
    public int Typical { get; set; }

    [JsonPropertyName("tfs")]
    public decimal Tfs { get; set; }

    [JsonPropertyName("rep_pen_range")]
    public int RepPenRange { get; set; }

    [JsonPropertyName("rep_pen_slope")]
    public decimal RepPenSlope { get; set; }

    [JsonPropertyName("sampler_order")]
    public List<int>? SamplerOrder { get; set; }

    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }

    [JsonPropertyName("quiet")]
    public bool Quiet { get; set; }
}


