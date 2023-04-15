using Pacos.Models.Domain;
using Pacos.Models.KoboldApi;
using Pacos.Services.Prompts;

namespace Pacos.Services.Presets;

public class Chat7BPreset : BasePresetFactory
{
    private readonly ChatPromptFactory _chatPromptFactory;

    public Chat7BPreset(ChatPromptFactory chatPromptFactory)
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
        // Coherent Creativity 6B
        return new KoboldRequest
        {
            N = 1,
            MaxContextLength = LLaMaContextTokens,
            MaxLength = responseTokens,
            RepPen = 1.2m,
            Temperature = 0.51m,
            TopP = 1,
            TopK = 0,
            TopA = 0,
            Typical = 1,
            Tfs = 0.99m,
            RepPenRange = 2048,
            RepPenSlope = 0,
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
