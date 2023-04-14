using System.ComponentModel.DataAnnotations;

namespace Pacos.Models;

public class PacosOptions
{
    [Required]
    [RegularExpression(@".*:.*")]
    public required string TelegramBotApiKey { get; init; }

    [Required]
    [Url]
    public required string KoboldApiAddress { get; set; }
}
