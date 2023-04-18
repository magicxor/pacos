using Pacos.Models.Domain;
using Pacos.Models.KoboldApi;
using Pacos.Services.Prompts;

namespace Pacos.Services.Presets;

public class Llama65BPreset : BasePresetFactory
{
    private readonly ChatPromptFactory _chatPromptFactory;

    public Llama65BPreset(ChatPromptFactory chatPromptFactory)
    {
        _chatPromptFactory = chatPromptFactory;
    }

    public override PromptResult CreatePrompt(PromptRequest promptRequest)
    {
        return _chatPromptFactory.CreatePrompt(promptRequest);
    }

    public override KoboldRequest CreateRequestData(string prompt,
        int responseTokens = MaxUsualResponseTokens)
    {
        return new KoboldRequest
        {
            N = 1,
            MaxContextLength = LLaMaContextTokens,
            MaxLength = responseTokens,
            RepPen = new decimal(1 / 0.85),
            Temperature = 0.7m,
            TopP = 0.0m,
            TopK = 40,
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
