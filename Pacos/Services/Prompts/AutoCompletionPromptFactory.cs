using Pacos.Extensions;
using Pacos.Models.Domain;

namespace Pacos.Services.Prompts;

public class AutoCompletionPromptFactory : BasePromptFactory
{
    public override PromptResult CreatePrompt(PromptRequest promptRequest)
    {
        var newPrompt = promptRequest
            .NewContextItem
            .UserMessage
            .Cut(MaxPromptSymbols);

        return new PromptResult(newPrompt, Array.Empty<ContextItem>());
    }
}
