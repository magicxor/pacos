using Pacos.Models.Domain;
using Pacos.Models.KoboldApi;
using Pacos.Services.Prompts;

namespace Pacos.Services.Presets;

public class AutoCompletion13BPreset : BasePresetFactory
{
    private readonly AutoCompletionPromptFactory _autoCompletionPromptFactory;

    public AutoCompletion13BPreset(AutoCompletionPromptFactory autoCompletionPromptFactory)
    {
        _autoCompletionPromptFactory = autoCompletionPromptFactory;
    }

    public override PromptResult CreatePrompt(PromptRequest promptRequest)
    {
        return _autoCompletionPromptFactory.CreatePrompt(promptRequest);
    }

    public override KoboldRequest CreateRequestData(string prompt,
        int responseTokens = MaxUsualResponseTokens)
    {
        // basic coherence 13b with low temperature
        return new KoboldRequest
        {
            N = 1,
            MaxContextLength = LLaMaContextTokens,
            MaxLength = responseTokens,
            RepPen = 1.1m,
            Temperature = 0.2m,
            TopP = 1m,
            TopK = 0,
            TopA = 0,
            Typical = 1,
            Tfs = 0.87m,
            RepPenRange = 2048,
            RepPenSlope = 0.3m,
            SamplerOrder = new List<int>
            {
                5,
                0,
                2,
                3,
                1,
                4,
                6,
            },
            Quiet = true,
            Prompt = prompt,
        };
    }
}
