using System.Text.Json.Serialization;

namespace Pacos.Models;

public class KoboldRequest
{
    [JsonPropertyName("n")]
    public int N { get; set; }

    [JsonPropertyName("max_context_length")]
    public int MaxContextLength { get; set; }

    [JsonPropertyName("max_length")]
    public int MaxLength { get; set; }

    [JsonPropertyName("rep_pen")]
    public double RepPen { get; set; }

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }

    [JsonPropertyName("top_p")]
    public int TopP { get; set; }

    [JsonPropertyName("top_k")]
    public int TopK { get; set; }

    [JsonPropertyName("top_a")]
    public int TopA { get; set; }

    [JsonPropertyName("typical")]
    public int Typical { get; set; }

    [JsonPropertyName("tfs")]
    public double Tfs { get; set; }

    [JsonPropertyName("rep_pen_range")]
    public int RepPenRange { get; set; }

    [JsonPropertyName("rep_pen_slope")]
    public int RepPenSlope { get; set; }

    [JsonPropertyName("sampler_order")]
    public List<int>? SamplerOrder { get; set; }

    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }

    [JsonPropertyName("quiet")]
    public bool Quiet { get; set; }
}


