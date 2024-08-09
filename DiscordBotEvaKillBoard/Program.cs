using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using WebSocketSharp;

namespace DiscordBotEvaKillBoard
{
    internal class Program
    {
        private DiscordSocketClient _client;
        private IConfiguration _configuration;
        private readonly string _socketUrl = "wss://zkillboard.com/websocket/";

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
                var botTask = StartBotAsync(_configuration);
                var socketTask = StartListening();

                await Task.WhenAll(botTask, socketTask);
            }
            catch (Exception ex)
            {
                if (ex is AggregateException aggregateException)
                {
                    // Обработка нескольких исключений
                    foreach (var innerException in aggregateException.InnerExceptions)
                    {
                        Console.WriteLine($"Ошибка: {innerException.Message}");
                        Console.WriteLine(innerException.StackTrace);
                    }
                }
                else
                {
                    // Логирование одиночной ошибки
                    Console.WriteLine($"Ошибка: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }

        private async Task StartListening()
        {
            var ws = new WebSocket(_socketUrl);

            ws.OnMessage += async (sender, e) =>
            {
                // обработчик не дает запускать напрямую асинхронные методы мы используем Таск
                await Task.Run(async () =>
                {
                    try
                    {
                        KillEvent killEvent = JsonConvert.DeserializeObject<KillEvent>(e.Data)!;
                        if (killEvent != null)
                        {
                            await Console.Out.WriteLineAsync($"Полученно сообщение : {killEvent.Url}");
                            await SendMessageToChannelAsync(killEvent.Url);
                        }
                        else
                        {
                            Console.WriteLine("Не удалось обработать сообщение");
                        }

                    }
                    catch (JsonException jsonEx)
                    {
                        Console.WriteLine("Ошибка десериализации: " + jsonEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Произошла ошибка: " + ex.Message);
                    }


                }).ConfigureAwait(false);
            };

            ws.Connect();

            var subscribeMessage = "{\"action\":\"sub\",\"channel\":\"all:*\"}";
            ws.Send(subscribeMessage);

            await Task.Delay(Timeout.Infinite);
        }

        private async Task StartBotAsync(IConfiguration configuration)
        {
            DiscordSocketConfig config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.MessageContent | GatewayIntents.GuildMessages
            };

            _client = new DiscordSocketClient(config);

            _client.Log += LogHandler;
            _client.MessageReceived += MessageHandler;
            _client.Ready += OnReady;

            await _client.LoginAsync(TokenType.Bot, configuration["Discord:Token"]);
            await _client.StartAsync();

            await Task.Delay(Timeout.Infinite);
        }

        private async Task MessageHandler(SocketMessage message)
        {
            if (message.Author.IsBot)
            {
                return;
            }

            await SendMessageToChannelAsync(message.Content);
        }

        private async Task LogHandler(LogMessage message)
        {
            await Console.Out.WriteLineAsync(message.ToString());
        }

        private async Task OnReady()
        {
            await SendMessageToChannelAsync("Бот готов к работе !!");
        }

        public async Task SendMessageToChannelAsync(string message)
        {
            ulong channelId = ulong.Parse(_configuration["Discord:ChanelId"]!);
            var channel = _client.GetChannel(channelId) as ITextChannel;

            if (channel != null)
            {
                try
                {
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
