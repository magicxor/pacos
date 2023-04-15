namespace Pacos.Models.Domain;

public record ContextItem(
    long UserId,
    string RealUserName,
    string UserMessage,
    string? BotReply = null
);
