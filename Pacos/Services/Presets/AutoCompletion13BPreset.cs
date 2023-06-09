﻿using Pacos.Models.Domain;
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
        // based on 65B config;
        // see
        // https://gist.github.com/shawwn/726e7531573c3cd64664ceb9d9e477fa
        // https://gist.github.com/shawwn/63fe948dd4a6bc86ecfd6e51606a0b4b
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
