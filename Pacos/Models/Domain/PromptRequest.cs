namespace Pacos.Models.Domain;

public record PromptRequest(
    string LanguageCode,
    IReadOnlyCollection<ContextItem> Context,
    ContextItem NewContextItem
);
