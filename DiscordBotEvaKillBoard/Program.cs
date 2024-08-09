using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
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
            catch (AggregateException ex)
            {
                // Обработка нескольких исключений
                foreach (var innerException in ex.InnerExceptions)
                {
                    Console.WriteLine($"Ошибка: {innerException.Message}");
                    Console.WriteLine(innerException.StackTrace);
                }
            }
            catch (Exception ex)
            {
                // Логирование ошибки
                Console.WriteLine($"Ошибка: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private async Task StartListening()
        {
            // создание
            var ws = new WebSocket(_socketUrl);

            // реакция на полученное событие от сокета к которому мы подключены
            ws.OnMessage += (sender, e) =>
            {
                Console.WriteLine("Получено сообщение: " + e.Data);
            };

            // подключение
            ws.Connect();

            // для теста подписка на весь поток
            //var subscribeMessage = "{\"action\":\"sub\",\"channel\":\"killstream\"}";
            // упрощенная информация
            var subscribeMessage = "{\"action\":\"sub\",\"channel\":\"all:*\"}";
            ws.Send(subscribeMessage);


            // не даем закрыться и держим в фоновом режиме
            await Task.Delay(Timeout.Infinite);

        }

        private async Task StartBotAsync(IConfiguration configuration)
        {
            DiscordSocketConfig config = new DiscordSocketConfig
            {
                //GatewayIntents = GatewayIntents.Guilds
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.MessageContent | GatewayIntents.GuildMessages
            };

            _client = new DiscordSocketClient(config);

            // Подписка на события логгирование и если кто-то отправил сообщение на сервере
            _client.Log += LogHandler;
            _client.MessageReceived += MessageHandler;
            _client.Ready += OnReady;

            await _client.LoginAsync(TokenType.Bot, configuration["Discord:Token"]);
            await _client.StartAsync();


            // Держим бота активным
            await Task.Delay(Timeout.Infinite);
        }

        private async Task MessageHandler(SocketMessage message)
        {
            //TODO: add command parser
            // at the moment all mesages from user in server from all chanels sended to one
            // check is it users message
            if (message.Author.IsBot)
            {
                return;
            }

            await SendMessageToChannel(message.Content);



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
