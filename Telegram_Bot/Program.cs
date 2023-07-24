using Telegram.Bot;
using Telegram.Bot.Services;

internal class Program
{
    private async static Task Main(string[] args)
    {
        IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Register Bot configuration
        services.Configure<BotConfiguration>(
            context.Configuration.GetSection(BotConfiguration.Configuration));

        // Register named HttpClient to benefits from IHttpClientFactory
        // and consume it with ITelegramBotClient typed client.
        services.AddHttpClient("telegram_bot_client")
                .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
                {
                    BotConfiguration botConfig = sp.GetConfiguration<BotConfiguration>();
                    TelegramBotClientOptions options = new(botConfig.BotToken);
                    return new TelegramBotClient(options, httpClient);
                });

        services.AddScoped<UpdateHandler>();
        services.AddScoped<ReceiverService>();
        services.AddHostedService<PollingService>();
    })
    .Build();

        await host.RunAsync();
    }
}

public class BotConfiguration
{
    public static readonly string Configuration = "BotConfiguration";

    public string BotToken { get; set; } = "";
}
