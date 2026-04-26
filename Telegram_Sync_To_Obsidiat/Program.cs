using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramObsidianBridge
{
    public class AppConfig
    {
        public string Token { get; set; } = "";
        public string ProxyUrl { get; set; } = "";
        public string ObsidianPath { get; set; } = "";
        public long? AdminChatId { get; set; } = null;
    }

    class Program
    {
        private static string configPath = "config.json";
        private static AppConfig config = new AppConfig();
        private static TelegramBotClient? botClient;

        static async Task Main(string[] args)
        {
            Console.Title = "Obsidian Telegram Sync v1.0 [Full Release]";

            if (!LoadConfig()) SetupWizard();

            if (string.IsNullOrEmpty(config.Token))
            {
                Log("Ошибка: Токен отсутствует. Перезапустите программу для настройки.", ConsoleColor.Red);
                Console.ReadKey();
                return;
            }

            var options = new TelegramBotClientOptions(token: config.Token, baseUrl: config.ProxyUrl);
            botClient = new TelegramBotClient(options);

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

            try
            {
                botClient.StartReceiving(
                    updateHandler: HandleUpdateAsync,
                    errorHandler: HandleErrorAsync,
                    receiverOptions: new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() },
                    cancellationToken: cts.Token
                );

                Log("--- СИСТЕМА ЗАПУЩЕНА (v1.0) ---", ConsoleColor.Cyan);

                if (config.AdminChatId == null)
                {
                    Log("=============================================", ConsoleColor.Yellow);
                    Log("  КТО ТЫ, ЧЕЛОВЕК? НАПИШИ БОТУ В TELEGRAM.   ", ConsoleColor.Magenta);
                    Log("=============================================", ConsoleColor.Yellow);
                }
                else
                {
                    Log($"Бот активен. ID владельца: {config.AdminChatId}", ConsoleColor.DarkGray);
                    await botClient.SendMessage(config.AdminChatId.Value, "🚀 Бот запущен и готов принимать заметки!");
                }

                Log("Нажмите ENTER для завершения работы...", ConsoleColor.Gray);
                while (!cts.IsCancellationRequested)
                {
                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Enter) break;
                    await Task.Delay(100);
                }
            }
            finally
            {
                // Сообщение о выключении
                if (config.AdminChatId.HasValue && botClient != null)
                {
                    Log("Отправка уведомления о выключении...", ConsoleColor.Yellow);
                    try
                    {
                        using var shutdownCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        await botClient.SendMessage(config.AdminChatId.Value, "📴 Бот ушел в оффлайн. До связи!", cancellationToken: shutdownCts.Token);
                    }
                    catch { /* Игнорируем ошибки при закрытии */ }
                }
                cts.Cancel();
            }
        }

        static async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
        {
            if (update.Message is not { } message) return;

            string? text = message.Text ?? (message.Photo != null ? "[Фото] " + (message.Caption ?? "") : message.Caption);

            if (string.IsNullOrEmpty(text)) return;

            if (config.AdminChatId == null)
            {
                config.AdminChatId = message.Chat.Id;
                SaveConfig();
                Log($"ID {config.AdminChatId} успешно привязан!", ConsoleColor.Magenta);
                await bot.SendMessage(config.AdminChatId.Value, "✅ Привязка завершена! Я готов сохранять ваши мысли в Obsidian.", cancellationToken: ct);
                return;
            }

            if (message.Chat.Id != config.AdminChatId) return;

            try
            {
                if (!Directory.Exists(config.ObsidianPath)) Directory.CreateDirectory(config.ObsidianPath);

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
                string fullPath = Path.Combine(config.ObsidianPath, $"TG_{timestamp}.md");

                string content = $@"---
date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
source: telegram
---
# Заметка из Telegram

{text}

---
#telegram_inbox";

                await File.WriteAllTextAsync(fullPath, content, ct);
                Log($"[OK] Сохранено в Obsidian: {timestamp}", ConsoleColor.Green);
                await bot.SendMessage(config.AdminChatId.Value, "✅ Сохранено", cancellationToken: ct);
            }
            catch (Exception ex) { Log($"ОШИБКА: {ex.Message}", ConsoleColor.Red); }
        }

        private static bool LoadConfig()
        {
            try
            {
                string fullConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configPath);
                if (!File.Exists(fullConfigPath)) return false;

                string json = File.ReadAllText(fullConfigPath);
                config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                return !string.IsNullOrEmpty(config.Token);
            }
            catch { return false; }
        }

        private static void SaveConfig()
        {
            try
            {
                string fullConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configPath);
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(fullConfigPath, json);
            }
            catch (Exception ex) { Log($"Ошибка сохранения конфига: {ex.Message}", ConsoleColor.Red); }
        }

        private static void SetupWizard()
        {
            Console.Clear();
            Log("=== ПЕРВИЧНАЯ НАСТРОЙКА v1.0 ===", ConsoleColor.Cyan);
            Console.Write("Введите Token бота: "); config.Token = Console.ReadLine()?.Trim() ?? "";
            Console.Write("Введите Proxy URL: "); config.ProxyUrl = Console.ReadLine()?.Trim() ?? "";
            Console.Write("Путь к папке Obsidian: "); config.ObsidianPath = Console.ReadLine()?.Trim() ?? "";

            SaveConfig();

            Console.Clear();
            Log("==========================================", ConsoleColor.Yellow);
            Log("   НАСТРОЙКИ СОХРАНЕНЫ!", ConsoleColor.Yellow);
            Log("==========================================", ConsoleColor.Yellow);
            Log("Теперь запустите бота, и напишите ему в ТГ,", ConsoleColor.Magenta);
            Log("чтобы он запомнил ваш аккаунт.", ConsoleColor.Magenta);
            Log("==========================================", ConsoleColor.Yellow);
            Log("Нажмите ENTER для продолжения...", ConsoleColor.Gray);
            Console.ReadLine();
        }

        static Task HandleErrorAsync(ITelegramBotClient b, Exception e, HandleErrorSource s, CancellationToken c)
        {
            if (!e.Message.Contains("timed out")) Log($"API Error: {e.Message}", ConsoleColor.Red);
            return Task.CompletedTask;
        }

        static void Log(string m, ConsoleColor c) { Console.ForegroundColor = c; Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {m}"); Console.ResetColor(); }
    }
}