using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NTextCat;
using Pacos.Enums;
using Pacos.Exceptions;
using Pacos.Extensions;
using Pacos.Models.KoboldApi;
using Pacos.Models.Options;
using Pacos.Services;
using Pacos.Services.BackgroundTasks;
using Pacos.Services.HttpMessageHandlers;
using Pacos.Services.Presets;
using Pacos.Services.Prompts;
using Polly;
using Polly.Extensions.Http;
using Refit;
using Telegram.Bot;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Pacos;

public class Program
{
    private static readonly IAsyncPolicy<HttpResponseMessage> HttpRetryPolicy = HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(retryAttempt * 1.5));

    private static readonly LoggingConfiguration LoggingConfiguration = new XmlLoggingConfiguration("nlog.config");

    private static readonly TimeSpan KoboldApiTimeout = TimeSpan.FromMinutes(10);
    private const int BackgroundTaskQueueCapacity = 100;

    public static void Main(string[] args)
    {
        // NLog: setup the logger first to catch all errors
        LogManager.Configuration = LoggingConfiguration;
        try
        {
            var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddEnvironmentVariables();
            })
            .ConfigureLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.SetMinimumLevel(LogLevel.Trace);
                loggingBuilder.AddNLog(LoggingConfiguration);
            })
            .ConfigureServices((hostContext, services) =>
            {
                services
                    .AddOptions<PacosOptions>()
                    .Bind(hostContext.Configuration.GetSection(nameof(OptionSections.Pacos)))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                services.AddHttpClient(nameof(HttpClientTypes.Telegram))
                    .AddPolicyHandler(HttpRetryPolicy);

                services.AddHttpClient(nameof(HttpClientTypes.Kobold))
                    .AddHttpMessageHandler(() => new HttpKoboldHandler())
                    .ConfigureHttpClient(client => client.Timeout = KoboldApiTimeout);

                var koboldApiAddress = hostContext.Configuration.GetKoboldApiAddress()
                                                ?? throw new ServiceException(LocalizationKeys.Errors.Configuration.KoboldApiAddressMissing);
                services.AddScoped<IKoboldApi>(s =>
                {
                    var httpClient = s.GetRequiredService<IHttpClientFactory>()
                        .CreateClient(nameof(HttpClientTypes.Kobold));
                    httpClient.BaseAddress = new Uri(koboldApiAddress);

                    var koboldApi = RestService.For<IKoboldApi>(httpClient,
                        new RefitSettings {
                            ContentSerializer = new SystemTextJsonContentSerializer(),
                        });

                    return koboldApi;
                });

                var telegramBotApiKey = hostContext.Configuration.GetTelegramBotApiKey()
                                        ?? throw new ServiceException(LocalizationKeys.Errors.Configuration.TelegramBotApiKeyMissing);
                services.AddScoped<ITelegramBotClient, TelegramBotClient>(s => new TelegramBotClient(telegramBotApiKey,
                    s.GetRequiredService<IHttpClientFactory>()
                        .CreateClient(nameof(HttpClientTypes.Telegram))));

                services.AddHostedService<QueuedHostedService>();
                services.AddSingleton<IBackgroundTaskQueue>(_ => new BackgroundTaskQueue(BackgroundTaskQueueCapacity));

                services.AddScoped<RankedLanguageIdentifier>(_ => new RankedLanguageIdentifierFactory().Load("Core14.profile.xml"));

                services.AddScoped<AutoCompletionPromptFactory>();
                services.AddScoped<ChatPromptFactory>();
                services.AddScoped<InstructionPromptFactory>();

                services.AddScoped<AutoCompletion13BPreset>();
                services.AddScoped<Chat13BPreset>();
                services.AddScoped<Instruction20BPreset>();

                services.AddScoped<TelegramBotService>();

                services.AddHostedService<Worker>();
            })
            .Build();

            host.Run();
        }
        catch (Exception ex)
        {
            // NLog: catch setup errors
            LogManager.GetCurrentClassLogger(typeof(Program)).Error(ex, "Stopped program because of exception");
            throw;
        }
        finally
        {
            // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
            LogManager.Shutdown();
        }
    }
}
