using Pacos.Models.Domain;
using Pacos.Models.KoboldApi;
using Pacos.Services.Prompts;

namespace Pacos.Services.Presets;

public class Instruction20BPreset : BasePresetFactory
{
    private readonly InstructionPromptFactory _instructionPromptFactory;

    public Instruction20BPreset(InstructionPromptFactory instructionPromptFactory)
    {
        _instructionPromptFactory = instructionPromptFactory;
    }

    public override PromptResult CreatePrompt(PromptRequest promptRequest)
    {
        return _instructionPromptFactory.CreatePrompt(promptRequest);
    }

    public override KoboldRequest CreateRequestData(string prompt,
        int responseTokens = MaxUsualResponseTokens)
    {
        // default 20b
        return new KoboldRequest
        {
            N = 1,
            MaxContextLength = LLaMaContextTokens,
            MaxLength = responseTokens,
            RepPen = 1.04m,
            Temperature = 0.6m,
            TopP = 0.9m,
            TopK = 0,
            TopA = 0,
            Typical = 1,
            Tfs = 1m,
            RepPenRange = 1024,
            RepPenSlope = 0.7m,
            SamplerOrder = new List<int>
            {
                0,
                1,
                2,
                3,
                4,
                5,
                6,
            },
            Quiet = true,
            Prompt = prompt,
        };
    }
}
