namespace Pacos.Models.Domain;

public record PromptResult(
    string Prompt,
    IReadOnlyCollection<ContextItem> Context
);
