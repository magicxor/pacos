using Pacos.Models.Domain;
using Pacos.Models.KoboldApi;
using Pacos.Services.Prompts;

namespace Pacos.Services.Presets;

public class ChatOuroboros13BPreset : BasePresetFactory
{
    private readonly ChatPromptFactory _chatPromptFactory;

    public ChatOuroboros13BPreset(ChatPromptFactory chatPromptFactory)
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
        // Ouroboros 13B
        return new KoboldRequest
        {
            N = 1,
            MaxContextLength = LLaMaContextTokens,
            MaxLength = responseTokens,
            RepPen = 1.05m,
            Temperature = 1.07m,
            TopP = 1m,
            TopK = 100,
            TopA = 0,
            Typical = 1,
            Tfs = 0.93m,
            RepPenRange = 404,
            RepPenSlope = 0.8m,
            SamplerOrder = new List<int>
            {
                0,
                5,
                3,
                2,
                1,
                4,
                6,
            },
            Quiet = true,
            Prompt = prompt,
        };
    }
}
