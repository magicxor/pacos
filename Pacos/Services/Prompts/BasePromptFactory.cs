using Pacos.Models.Domain;

namespace Pacos.Services.Prompts;

public abstract class BasePromptFactory
{
    // 1 token = 2.5 symbols
    // LLaMaContextLength = 2048
    // 2048 * 2.5 = 5120
    public const int MaxPromptSymbols = 5000;
    public abstract PromptResult CreatePrompt(PromptRequest promptRequest);
}
