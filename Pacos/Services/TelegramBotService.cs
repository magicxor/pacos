using System.Diagnostics;
using System.Text.RegularExpressions;
using Humanizer;
using NTextCat;
using Pacos.Extensions;
using Pacos.Models;
using Pacos.Services.BackgroundTasks;
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

    private const int MaxTelegramMessageLength = 4096;
    private const int MaxUsualResponseLength = 80;
    private const int MaxProgrammingResponseLength = 400;
    private static readonly char[] ValidEndOfSentenceCharacters = { '.', '!', '?', '…' };
    private static readonly string[] ProgrammingMathPromptMarkers = { "{", "}", "[", "]", "Console.", "public static void", "public static", "public void", "public class", "<<", ">>", "&&", "|", "C#", "F#", "yml", "yaml", "json", "xml", "html", " программу ", " код " };
    private static readonly string[] ProgrammingMathResponseMarkers = { "{", "}", "[", "]", "=", "+", "Console.", "public static void", "public static", "public void", "public class", "<<", ">>", "&&", "|", "C#", "F#", "yml", "yaml", "json", "xml", "html" };
    private static readonly string[] Mentions = { "пакос,", "pacos," };
    private static readonly Regex NewChatMessageWithNickRegex = new(@"\n((?!question|answer)\w{2,}):\s", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly ReceiverOptions ReceiverOptions = new()
    {
        // receive all update types
        AllowedUpdates = Array.Empty<UpdateType>(),
    };

    public TelegramBotService(ILogger<TelegramBotService> logger,
        ITelegramBotClient telegramBotClient,
        RankedLanguageIdentifier rankedLanguageIdentifier,
        IKoboldApi koboldApi,
        IBackgroundTaskQueue taskQueue)
    {
        _logger = logger;
        _telegramBotClient = telegramBotClient;
        _rankedLanguageIdentifier = rankedLanguageIdentifier;
        _koboldApi = koboldApi;
        _taskQueue = taskQueue;
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Received update with type={update}", update.Type.ToString());

        await _taskQueue.QueueBackgroundWorkItemAsync(async token =>
            await HandleUpdateFunction(botClient, update, cancellationToken));
    }

    private async Task HandleUpdateFunction(ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken)
    {
        try
        {
            if (update is { Type: UpdateType.Message, Message: { Text: { } updateMessageText, ForwardFrom: null, ForwardFromChat: null, ForwardSignature: null } }
                && update.Message.IsAutomaticForward != true
                && Mentions.FirstOrDefault(m => updateMessageText.StartsWith(m, StringComparison.InvariantCultureIgnoreCase)) is { } mentionText
                && updateMessageText.Length > mentionText.Length)
            {
                var author = update.Message.From?.Username ??
                             update.Message.From?.FirstName + " " + update.Message.From?.LastName;
                var updateMessageTextTrimmed = updateMessageText[mentionText.Length..].Trim();

                _logger.LogInformation("New prompt from {author}: {updateMessageTextTrimmed}", author, updateMessageTextTrimmed);

                var language = _rankedLanguageIdentifier.Identify(updateMessageTextTrimmed).FirstOrDefault();
                var template = language?.Item1?.Iso639_3 == "rus"
                    ? ChatTemplateFactory.GetRussianTemplate(author, updateMessageTextTrimmed)
                    : ChatTemplateFactory.GetEnglishTemplate(author, updateMessageTextTrimmed);

                var isProgramRequest = ProgrammingMathPromptMarkers.Any(m => updateMessageTextTrimmed.Contains(m));

                var stopwatch = Stopwatch.StartNew();
                var koboldResponse = await _koboldApi.Generate(new KoboldRequest
                {
                    N = 1,
                    MaxContextLength = MaxTelegramMessageLength + template.Length,
                    MaxLength = isProgramRequest ? MaxProgrammingResponseLength : MaxUsualResponseLength,
                    RepPen = 1.2,
                    Temperature = 0.51,
                    TopP = 1,
                    TopK = 0,
                    TopA = 0,
                    Typical = 1,
                    Tfs = 0.99,
                    RepPenRange = 2048,
                    RepPenSlope = 0,
                    SamplerOrder = new List<int> { 5, 0, 2, 3, 1, 4, 6 },
                    Quiet = true,
                    Prompt = template,
                }, cancellationToken);
                stopwatch.Stop();

                var generatedResult = koboldResponse.Results?.FirstOrDefault()?.Text ?? "Error: kobold response is empty";

                var newChatMessageWithNickMatch = NewChatMessageWithNickRegex.Match(generatedResult);

                if (newChatMessageWithNickMatch.Success)
                {
                    // GPT thinks up the following dialogue, so we need to remove it
                    generatedResult = generatedResult[..newChatMessageWithNickMatch.Index];
                }
                else
                {
                    if (!ValidEndOfSentenceCharacters.Any(eos => generatedResult.EndsWith(eos))
                        && !ProgrammingMathResponseMarkers.Any(pm => generatedResult.Contains(pm)))
                    {
                        // GPT couldn't complete the sentence, so we need to remove the incomplete sentence
                        var lastValidEndOfSentenceCharacter = generatedResult.LastIndexOfAny(ValidEndOfSentenceCharacters);
                        if (lastValidEndOfSentenceCharacter >= 0)
                        {
                            generatedResult = generatedResult[..(lastValidEndOfSentenceCharacter + 1)];
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
