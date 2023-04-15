using System.ComponentModel.DataAnnotations;

namespace Pacos.Models.Options;

public class PacosOptions
{
    [Required]
    [RegularExpression(@".*:.*")]
    public required string TelegramBotApiKey { get; init; }

    [Required]
    [Url]
    public required string KoboldApiAddress { get; set; }
}
