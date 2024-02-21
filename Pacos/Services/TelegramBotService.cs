using System.Diagnostics;
using Humanizer;
using NTextCat;
using Pacos.Constants;
using Pacos.Enums;
using Pacos.Extensions;
using Pacos.Models.Domain;
using Pacos.Models.KoboldApi;
using Pacos.Services.BackgroundTasks;
using Pacos.Services.Presets;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Pacos.Services;

public class TelegramBotService
{
    private readonly ILogger<TelegramBotService> _logger;
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly RankedLanguageIdentifier _rankedLanguageIdentifier;
    private readonly IKoboldApi _koboldApi;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly AutoCompletion13BPreset _autoCompletion13BPreset;
    private readonly Chat13BPreset _chat13BPreset;
    private readonly Instruction20BPreset _instruction20BPreset;

    private static readonly ReceiverOptions ReceiverOptions = new()
    {
        // receive all update types
        AllowedUpdates = Array.Empty<UpdateType>(),
    };

    public TelegramBotService(ILogger<TelegramBotService> logger,
        ITelegramBotClient telegramBotClient,
        RankedLanguageIdentifier rankedLanguageIdentifier,
        IKoboldApi koboldApi,
        IBackgroundTaskQueue taskQueue,
        AutoCompletion13BPreset autoCompletion13BPreset,
        Chat13BPreset chat13BPreset,
        Instruction20BPreset instruction20BPreset)
    {
        _logger = logger;
        _telegramBotClient = telegramBotClient;
        _rankedLanguageIdentifier = rankedLanguageIdentifier;
        _koboldApi = koboldApi;
        _taskQueue = taskQueue;
        _autoCompletion13BPreset = autoCompletion13BPreset;
        _chat13BPreset = chat13BPreset;
        _instruction20BPreset = instruction20BPreset;
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Received update with type={Update}", update.Type.ToString());

        await _taskQueue.QueueBackgroundWorkItemAsync(async token =>
            await HandleUpdateFunction(botClient, update, cancellationToken));
    }

    private (PromptResult promptResult, KoboldRequest koboldRequest) UseAutoCompletionPreset(
        string languageCode,
        IReadOnlyCollection<ContextItem> context,
        ContextItem newContextItem)
    {
        var promptResult = _autoCompletion13BPreset.CreatePrompt(
            new PromptRequest(languageCode,
                context,
                newContextItem));

        var isProgramRequest = Const.ProgrammingMathPromptMarkers.Any(m => newContextItem.UserMessage.Contains(m));
        var maxResponseTokens = isProgramRequest
            ? BasePresetFactory.MaxProgrammingResponseTokens
            : BasePresetFactory.MaxUsualResponseTokens;

        var koboldRequest = _autoCompletion13BPreset.CreateRequestData(promptResult.Prompt, maxResponseTokens);

        return (promptResult, koboldRequest);
    }

    private (PromptResult promptResult, KoboldRequest koboldRequest) UseChatPreset(
        string languageCode,
        IReadOnlyCollection<ContextItem> context,
        ContextItem newContextItem)
    {
        var promptResult = _chat13BPreset.CreatePrompt(
            new PromptRequest(languageCode,
                context,
                newContextItem));

        var isProgramRequest = Const.ProgrammingMathPromptMarkers.Any(m => newContextItem.UserMessage.Contains(m));
        var maxResponseTokens = isProgramRequest
            ? BasePresetFactory.MaxProgrammingResponseTokens
            : BasePresetFactory.MaxUsualResponseTokens;

        var koboldRequest = _chat13BPreset.CreateRequestData(promptResult.Prompt, maxResponseTokens);

        return (promptResult, koboldRequest);
    }

    private (PromptResult promptResult, KoboldRequest koboldRequest) UseInstructionPreset(
        string languageCode,
        IReadOnlyCollection<ContextItem> context,
        ContextItem newContextItem)
    {
        var promptResult = _instruction20BPreset.CreatePrompt(
            new PromptRequest(languageCode,
                context,
                newContextItem));

        var isProgramRequest = Const.ProgrammingMathPromptMarkers.Any(m => newContextItem.UserMessage.Contains(m));
        var maxResponseTokens = isProgramRequest
            ? BasePresetFactory.MaxProgrammingResponseTokens
            : BasePresetFactory.MaxUsualResponseTokens;

        var koboldRequest = _instruction20BPreset.CreateRequestData(promptResult.Prompt, maxResponseTokens);

        return (promptResult, koboldRequest);
    }

    private (PromptResult promptResult, KoboldRequest koboldRequest) UsePreset(
        UserMessageTypes userMessageType,
        string languageCode,
        IReadOnlyCollection<ContextItem> context,
        ContextItem newContextItem)
    {
        return userMessageType switch
        {
            UserMessageTypes.AutoCompletion => UseAutoCompletionPreset(languageCode, context, newContextItem),
            UserMessageTypes.Instruction => UseInstructionPreset(languageCode, context, newContextItem),
            _ => UseChatPreset(languageCode, context, newContextItem),
        };
    }

    private async Task HandleUpdateFunction(ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken)
    {
        try
        {
            if (update is { Type: UpdateType.Message, Message: { Text: { } updateMessageText, ForwardFrom: null, ForwardFromChat: null, ForwardSignature: null, From: not null } }
                && update.Message.IsAutomaticForward != true
                && Const.Mentions.FirstOrDefault(m => updateMessageText.StartsWith(m, StringComparison.InvariantCultureIgnoreCase)) is { } mentionText
                && updateMessageText.Length > mentionText.Length)
            {
                var author = update.Message.From.Username ?? string.Join(' ', update.Message.From.FirstName, update.Message.From.LastName);
                var message = updateMessageText[mentionText.Length..].Trim();

                var (messageType, messageTextTrimmed) = message switch
                {
                    string when message.StartsWith(Const.AutoCompletionMarker, StringComparison.InvariantCultureIgnoreCase)
                                && message.Length > Const.AutoCompletionMarker.Length => (UserMessageTypes.AutoCompletion, message[Const.AutoCompletionMarker.Length..].Trim()),
                    string when message.StartsWith(Const.InstructionMarker, StringComparison.InvariantCultureIgnoreCase)
                                && message.Length > Const.InstructionMarker.Length => (UserMessageTypes.Instruction, message[Const.InstructionMarker.Length..].Trim()),
                    _ => (UserMessageTypes.Normal, message),
                };

                var language = _rankedLanguageIdentifier.Identify(messageTextTrimmed).FirstOrDefault();
                var languageCode = language?.Item1?.Iso639_3 ?? "eng";

                _logger.LogInformation("Processing the prompt from {Author} (lang={LanguageCode}, type={UserMessageType}): {UpdateMessageTextTrimmed}",
                    author, languageCode, messageType, messageTextTrimmed);

                var (promptResult, koboldRequest) = UsePreset(
                    messageType,
                    languageCode,
                    Array.Empty<ContextItem>(),
                    new ContextItem(update.Message.From.Id, author, messageTextTrimmed));

                var stopwatch = Stopwatch.StartNew();
                var koboldResponse = await _koboldApi.Generate(koboldRequest, cancellationToken);
                stopwatch.Stop();

                var generatedResult = koboldResponse.Results?.FirstOrDefault()?.Text ?? "Error: kobold response is empty";

                generatedResult = OutputTransformation
                    .Transform(generatedResult)
                    .Cut(Const.MaxTelegramMessageLength);

                var replyText = string.IsNullOrWhiteSpace(generatedResult)
                    ? "Error: generated result is empty"
                    : generatedResult;

                _logger.LogInformation("Response ({Elapsed}): {GeneratedResult}", stopwatch.Elapsed.Humanize(), generatedResult);

                await botClient.SendTextMessageAsync(new ChatId(update.Message.Chat.Id),
                    replyText,
                    replyToMessageId: update.Message.MessageId,
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while handling update");
        }
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is ApiRequestException apiRequestException)
        {
            _logger.LogError(exception,
                @"Telegram API Error. ErrorCode={ErrorCode}, RetryAfter={RetryAfter}, MigrateToChatId={MigrateToChatId}",
                apiRequestException.ErrorCode,
                apiRequestException.Parameters?.RetryAfter,
                apiRequestException.Parameters?.MigrateToChatId);
        }
        else
        {
            _logger.LogError(exception, @"Telegram API Error");
        }

        return Task.CompletedTask;
    }

    public void Start(CancellationToken cancellationToken)
    {
        _telegramBotClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: ReceiverOptions,
            cancellationToken: cancellationToken
        );
    }
}
