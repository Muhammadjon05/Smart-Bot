using Telegram.Bot;

namespace Smart_Bot.Services;

public class ConfigurationWebHook : IHostedService
{
   
    private readonly ILogger<ConfigurationWebHook> _logger;
    private readonly IServiceProvider _serviceProvider;
    public ConfigurationWebHook(ILogger<ConfigurationWebHook> logger, IServiceProvider serviceProvider, 
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
        _logger.LogInformation("Bot started");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
        _logger.LogInformation("Webhook stopped");
        await botClient.SendTextMessageAsync
            (chatId: 2105508818, text: "Webhook stopped");
    }
}