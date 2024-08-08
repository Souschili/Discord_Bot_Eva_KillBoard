using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace DiscordBotEvaKillBoard
{
    internal class Program
    {
        private DiscordSocketClient _client;
        private IConfiguration _configuration;

        // Получение конфигурации
        private static IConfigurationBuilder GetConfig()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        }

        static async Task Main(string[] args)
        {
            Program program = new Program();
            await program.StartAsync();
        }

        private async Task StartAsync()
        {
            try
            {
                _configuration = GetConfig().Build();
                await StartBotAsync(_configuration);
                // Ожидание завершения работы бота
                await Task.Delay(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                Console.WriteLine($"Ошибка: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private async Task StartBotAsync(IConfiguration configuration)
        {
            DiscordSocketConfig config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds
            };

            _client = new DiscordSocketClient(config);

            // Подписка на события логгирование и если кто-то отправил сообщение на сервере
            _client.Log += LogHandler;
            _client.Ready += OnReady;

            await _client.LoginAsync(TokenType.Bot, configuration["Discord:Token"]);
            await _client.StartAsync();
        }

        private async Task LogHandler(LogMessage message)
        {
            await Console.Out.WriteLineAsync(message.ToString());
        }

        private async Task OnReady()
        {
            await SendMessageToChannel("This is a sea of my life!!!");
        }

        private async Task SendMessageToChannel(string message)
        {
            // Получаем ID канала из конфигурации
            ulong channelId = ulong.Parse(_configuration["Discord:ChanelId"]!);
            // Получаем канал по ID
            var channel = _client.GetChannel(channelId) as ITextChannel;

            // Проверяем, что канал не равен null
            if (channel != null)
            {
                try
                {
                    // Отправляем сообщение в канал
                    await channel.SendMessageAsync(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending message: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Channel not found.");
            }
        }
    }
}
