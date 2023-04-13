using Microsoft.Extensions.Options;
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
    private readonly PacosOptions _options;
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly RankedLanguageIdentifier _rankedLanguageIdentifier;
    private readonly IKoboldApi _koboldApi;
    private readonly IBackgroundTaskQueue _taskQueue;

    private const int MaxTelegramMessageLength = 4096;
    private static readonly char[] ValidEndOfSentenceCharacters = { '.', '!', '?', '…' };

    private static readonly ReceiverOptions ReceiverOptions = new()
    {
        // receive all update types
        AllowedUpdates = Array.Empty<UpdateType>(),
    };

    public TelegramBotService(ILogger<TelegramBotService> logger,
        IOptions<PacosOptions> options,
        ITelegramBotClient telegramBotClient,
        RankedLanguageIdentifier rankedLanguageIdentifier,
        IKoboldApi koboldApi,
        IBackgroundTaskQueue taskQueue)
    {
        _logger = logger;
        _options = options.Value;
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
            if (update is { Type: UpdateType.Message, Message.Text: { } updateMessageText }
                && updateMessageText.StartsWith("пакос,", StringComparison.InvariantCultureIgnoreCase))
            {
                var author = update.Message.From?.Username ??
                             update.Message.From?.FirstName + " " + update.Message.From?.LastName;

                var language = _rankedLanguageIdentifier.Identify(updateMessageText).FirstOrDefault();
                var template = language?.Item1?.Iso639_3 == "rus"
                    ? ChatTemplateFactory.GetRussianTemplate(author, updateMessageText)
                    : ChatTemplateFactory.GetEnglishTemplate(author, updateMessageText);

                var koboldResponse = await _koboldApi.Generate(new KoboldRequest
                {
                    N = 1,
                    MaxContextLength = 1024,
                    MaxLength = 255,
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

                var generatedResult = koboldResponse.Results?.FirstOrDefault()?.Text ?? "Error: kobold response is empty";

                var authorPhraseIndex = generatedResult.IndexOf($"{author}:", StringComparison.Ordinal);
                if (authorPhraseIndex >= 0)
                {
                    // GPT thinks up the following dialogue, so we need to remove it
                    generatedResult = generatedResult[..authorPhraseIndex];
                }
                else
                {
                    if (!ValidEndOfSentenceCharacters.Any(eos => generatedResult.EndsWith(eos)))
                    {
                        // GPT couldn't complete the sentence, so we need to remove the incomplete sentence
                        var lastValidEndOfSentenceCharacter = generatedResult.LastIndexOfAny(ValidEndOfSentenceCharacters);
                        if (lastValidEndOfSentenceCharacter >= 0)
                        {
                            generatedResult = generatedResult[..(lastValidEndOfSentenceCharacter + 1)];
                        }
                    }
                }

                await botClient.SendTextMessageAsync(new ChatId(update.Message.Chat.Id), generatedResult.Cut(MaxTelegramMessageLength, "empty"), cancellationToken: cancellationToken);
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
