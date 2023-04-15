using System.Diagnostics;
using System.Text.RegularExpressions;
using Humanizer;
using NTextCat;
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

    private const int MaxTelegramMessageLength = 4096;
    private static readonly char[] ValidEndOfSentenceCharacters = { '.', '!', '?', '…', ';' };
    private static readonly string[] ProgrammingMathPromptMarkers = { "{", "}", "[", "]", "==", "Console.", "public static void", "public static", "public void", "public class", "<<", ">>", "&&", "|", "C#", "F#", "C++", "javascript", " js", "typescript", "yml", "yaml", "json", "xml", "html", " программу ", " код ", "code snippet" };
    // can't use #, /*, // because they sometimes occur in normal output too
    private static readonly string[] ProgrammingMathResponseMarkers = { "{", "}", "[", "]", "==", "Console.", "public static void", "public static", "public void", "public class", "<<", ">>", "&&", "|", "/>" };
    private static readonly string[] Mentions = { "пакос,", "pacos," };
    private const string AutoCompletionMarker = "!continue";
    private const string InstructionMarker = "!";
    private static readonly Regex StartOfNewMessageRegex = new(@"\n((?!question|answer)\w{2,}):\s", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
        _logger.LogDebug("Received update with type={update}", update.Type.ToString());

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

        var isProgramRequest = ProgrammingMathPromptMarkers.Any(m => newContextItem.UserMessage.Contains(m));
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

        var isProgramRequest = ProgrammingMathPromptMarkers.Any(m => newContextItem.UserMessage.Contains(m));
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

        var isProgramRequest = ProgrammingMathPromptMarkers.Any(m => newContextItem.UserMessage.Contains(m));
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
                && Mentions.FirstOrDefault(m => updateMessageText.StartsWith(m, StringComparison.InvariantCultureIgnoreCase)) is { } mentionText
                && updateMessageText.Length > mentionText.Length)
            {
                var author = update.Message.From.Username ??
                             update.Message.From.FirstName + " " + update.Message.From.LastName;
                var updateMessageTextTrimmed = updateMessageText[mentionText.Length..].Trim();

                var userMessageType = updateMessageTextTrimmed switch
                {
                    string when updateMessageTextTrimmed.StartsWith(AutoCompletionMarker, StringComparison.InvariantCultureIgnoreCase)
                                && updateMessageTextTrimmed.Length > AutoCompletionMarker.Length => UserMessageTypes.AutoCompletion,
                    string when updateMessageTextTrimmed.StartsWith(InstructionMarker, StringComparison.InvariantCultureIgnoreCase)
                                && updateMessageTextTrimmed.Length > InstructionMarker.Length => UserMessageTypes.Instruction,
                    _ => UserMessageTypes.Normal,
                };
                updateMessageTextTrimmed = userMessageType switch
                {
                    UserMessageTypes.AutoCompletion => updateMessageTextTrimmed[AutoCompletionMarker.Length..].Trim(),
                    UserMessageTypes.Instruction => updateMessageTextTrimmed[InstructionMarker.Length..].Trim(),
                    _ => updateMessageTextTrimmed,
                };

                var language = _rankedLanguageIdentifier.Identify(updateMessageTextTrimmed).FirstOrDefault();
                var languageCode = language?.Item1?.Iso639_3 ?? "eng";

                _logger.LogInformation("Processing the prompt from {author} (lang={languageCode}, type={userMessageType}): {updateMessageTextTrimmed}",
                    author, languageCode, userMessageType, updateMessageTextTrimmed);

                var (promptResult, koboldRequest) = UsePreset(
                    userMessageType,
                    languageCode,
                    Array.Empty<ContextItem>(),
                    new ContextItem(update.Message.From.Id, author, updateMessageTextTrimmed));

                var stopwatch = Stopwatch.StartNew();
                var koboldResponse = await _koboldApi.Generate(koboldRequest, cancellationToken);
                stopwatch.Stop();

                var generatedResult = koboldResponse.Results?.FirstOrDefault()?.Text ?? "Error: kobold response is empty";

                var startOfNewMessageMatch = StartOfNewMessageRegex.Match(generatedResult);

                if (startOfNewMessageMatch.Success)
                {
                    // GPT thinks up the following dialogue, so we need to remove it
                    generatedResult = generatedResult[..startOfNewMessageMatch.Index];
                }
                else
                {
                    if (!ProgrammingMathResponseMarkers.Any(pm => generatedResult.Contains(pm)))
                    {
                        // it's not a code snippet, so we can trim the output using various rules
                        generatedResult = generatedResult.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).First();
                        generatedResult = generatedResult.Split("\r\n\r\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).First();
                        generatedResult = generatedResult.Split("/*", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).First();

                        if (!ValidEndOfSentenceCharacters.Any(eos => generatedResult.EndsWith(eos))
                            && !ValidEndOfSentenceCharacters.Any(eos => generatedResult.EndsWith($"{eos})")))
                        {
                            // GPT couldn't complete the sentence, so we need to remove the incomplete sentence
                            var lastValidEndOfSentenceCharacter = generatedResult.LastIndexOfAny(ValidEndOfSentenceCharacters);
                            if (lastValidEndOfSentenceCharacter >= 0)
                            {
                                generatedResult = generatedResult[..(lastValidEndOfSentenceCharacter + 1)];
                            }
                        }
                    }
                }

                _logger.LogInformation("Response ({elapsed}): {generatedResult}", stopwatch.Elapsed.Humanize(), generatedResult);

                await botClient.SendTextMessageAsync(new ChatId(update.Message.Chat.Id),
                    generatedResult.Cut(MaxTelegramMessageLength, "empty"),
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
