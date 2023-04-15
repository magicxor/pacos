using Pacos.Models.Domain;
using Pacos.Models.KoboldApi;

namespace Pacos.Services.Presets;

public abstract class BasePresetFactory
{
    // The llama models were trained with a context size of 2048.
    // By default llama.cpp limits it to 512,
    // but you can use -c 2048 -n 2048 to get the full context window.
    public const int LLaMaContextTokens = 2048;

    public const int MaxUsualResponseTokens = 80;
    public const int MaxProgrammingResponseTokens = 200;

    public abstract PromptResult CreatePrompt(PromptRequest promptRequest);

    public abstract KoboldRequest CreateRequestData(string prompt,
        int responseTokens = MaxUsualResponseTokens);
}
