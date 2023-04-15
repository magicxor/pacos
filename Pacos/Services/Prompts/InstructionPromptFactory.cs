using Pacos.Extensions;
using Pacos.Models.Domain;

namespace Pacos.Services.Prompts;

// todo: preserve context
public class InstructionPromptFactory : BasePromptFactory
{
    private static string GetEnglishPrompt(ContextItem contextItem)
    {
        var userMessage = contextItem.UserMessage;
        var botReply = contextItem.BotReply;

        return @$"Below is an instruction that describes a task. Write a response that appropriately completes the request.

### Instruction:
{userMessage}

### Response:
{botReply}";
    }

    private static string GetRussianPrompt(ContextItem contextItem)
    {
        var userMessage = contextItem.UserMessage;
        var botReply = contextItem.BotReply;

        return @$"Ниже приведена инструкция, описывающая задачу. Напишите ответ, который надлежащим образом завершает запрос.
### Инструкция:
{userMessage}

### Ответ:
{botReply}";
    }

    public override PromptResult CreatePrompt(PromptRequest promptRequest)
    {
        var newPrompt = promptRequest.LanguageCode == "rus"
            ? GetRussianPrompt(promptRequest.NewContextItem)
            : GetEnglishPrompt(promptRequest.NewContextItem);
        newPrompt = newPrompt.Cut(MaxPromptSymbols);

        return new PromptResult(newPrompt, Array.Empty<ContextItem>());
    }
}
