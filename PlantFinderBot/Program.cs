using System.Net.Http.Headers;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Plant_API.Models;

namespace Plant_Finder;

internal enum BotState
{
    ChoosingSearchMode,
    AwaitingLanguageForName,
    AwaitingLanguageForPhoto,
    AwaitingPhotoMode,
    AwaitingPhotoSingle,
    AwaitingPhotoMultiple,
    AwaitingName,
    AwaitingFavName,
    AwaitingFavDeleteName,
    AwaitingNextAction
}

internal class Program
{
    private static readonly string? BotToken = Environment.GetEnvironmentVariable("BOT_TOKEN");
    private static readonly string ApiBaseUrl = "http://localhost:5087/api";
    private static readonly Dictionary<long, BotState> UserStates = new();
    private static readonly Dictionary<long, string> UserLanguages = new();
    private static readonly HttpClient httpClient = new();

    private static async Task Main()
    {
        var bot = new TelegramBotClient(BotToken!);

        await bot.SetMyCommands(new[]
        {
            new BotCommand { Command = "start", Description = "Відкрити меню" }
        });

        Console.WriteLine("Bot is running");

        using var cts = new CancellationTokenSource();

        bot.StartReceiving(
            HandleUpdate, HandleError, new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            },
            cts.Token);

        Console.ReadLine();
        cts.Cancel();
    }

    private static async Task HandleUpdate(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update.Message == null) return;

        var message = update.Message;
        var chatId = update.Message.Chat.Id;
        var userId = update.Message.From?.Id ?? 0;

        if (userId == 0)
        {
            Console.WriteLine($"Could not determine user ID for message in chat {chatId}. Update type: {update.Type}");
            return;
        }


        if (message.Type == MessageType.Text &&
            message.Text?.Equals("/start", StringComparison.OrdinalIgnoreCase) == true)
        {
            UserLanguages.Remove(chatId);
            await ShowNextActionsMenu(bot, chatId, ct);
            return;
        }

        if (!UserStates.TryGetValue(chatId, out var currentState))
        {
            await bot.SendMessage(chatId, "Спершу введіть /start.", cancellationToken: ct);
            return;
        }

        switch (currentState)
        {
            case BotState.ChoosingSearchMode:
                if (message.Type == MessageType.Text && !string.IsNullOrEmpty(message.Text))
                    await HandleChoosingSearchModeStateAsync(bot, chatId, message.Text, ct);
                else
                    await bot.SendMessage(chatId, "Будь ласка, оберіть спосіб пошуку за допомогою кнопок.",
                        cancellationToken: ct);
                break;
            case BotState.AwaitingLanguageForName:
            case BotState.AwaitingLanguageForPhoto:
                if (message.Type == MessageType.Text && !string.IsNullOrEmpty(message.Text))
                    await HandleAwaitingLanguageStateAsync(bot, chatId, message.Text, ct);
                else
                    await bot.SendMessage(chatId, "Будь ласка, оберіть мову за допомогою кнопок.",
                        cancellationToken: ct);

                break;
            case BotState.AwaitingPhotoMode:
                if (message.Type == MessageType.Text && !string.IsNullOrEmpty(message.Text))
                    await HandleAwaitingPhotoModeStateAsync(bot, chatId, message.Text, ct);
                else
                    await bot.SendMessage(chatId, "Будь ласка, оберіть опцію за допомогою кнопок.",
                        cancellationToken: ct);

                break;
            case BotState.AwaitingPhotoSingle:
            case BotState.AwaitingPhotoMultiple:
                if (message.Type == MessageType.Photo && message.Photo != null)
                    await HandlePhotoMessageAsync(bot, chatId, userId, message.Photo, currentState, ct);
                else
                    await bot.SendMessage(chatId,
                        "Будь ласка, надішліть фото для пошуку(Увага! Стиснутий формат, а не формат файлу).",
                        cancellationToken: ct);
                break;
            case BotState.AwaitingName:
                if (message.Type == MessageType.Text && !string.IsNullOrEmpty(message.Text))
                    await HandleAwaitingNameStateAsync(bot, chatId, userId, message.Text, ct);
                else
                    await bot.SendMessage(chatId, "Будь ласка, введіть назву рослини текстовим повідомленням.",
                        cancellationToken: ct);
                break;
            case BotState.AwaitingFavName:
                if (message.Type == MessageType.Text && !string.IsNullOrEmpty(message.Text))
                    await AddFavourite(bot, chatId, userId, message.Text, ct);
                else
                    await bot.SendMessage(chatId,
                        "Будь ласка, введіть назву рослини для додавання в улюблені (текстом).", cancellationToken: ct);
                break;
            case BotState.AwaitingFavDeleteName:
                if (message.Type == MessageType.Text && !string.IsNullOrEmpty(message.Text))
                    await DeleteFavourite(bot, chatId, userId, message.Text, ct);
                else
                    await bot.SendMessage(chatId,
                        "Будь ласка, введіть назву рослини для видалення з улюблених (текстом).",
                        cancellationToken: ct);
                break;
            case BotState.AwaitingNextAction:
                await HandleAwaitingNextActionStateAsync(bot, chatId, userId, message.Text, ct);
                break;
            default:
                await bot.SendMessage(chatId, "Виникла помилка. Розпочніть спочатку через /start.",
                    cancellationToken: ct);
                UserStates[chatId] = BotState.AwaitingNextAction;
                await ShowNextActionsMenu(bot, chatId, ct);
                break;
        }
    }

    private static async Task HandleChoosingSearchModeStateAsync(ITelegramBotClient bot, long chatId, string txt,
        CancellationToken ct)
    {
        if (txt.Trim().ToLower() == "пошук за фото")
        {
            UserStates[chatId] = BotState.AwaitingLanguageForPhoto;
            await RequestLanguage(bot, chatId, ct);
        }
        else if (txt.Trim().ToLower() == "пошук за назвою")
        {
            UserStates[chatId] = BotState.AwaitingLanguageForName;
            await RequestLanguage(bot, chatId, ct);
        }
        else
        {
            await bot.SendMessage(chatId, "Будь ласка, оберіть спосіб пошуку за допомогою кнопок.",
                cancellationToken: ct);
            await ShowSearchMenu(bot, chatId, ct);
        }
    }

    private static async Task HandleAwaitingLanguageStateAsync(ITelegramBotClient bot, long chatId, string txt,
        CancellationToken ct)
    {
        string langCode;
        if (txt.Trim().ToLower() == "англійська")
        {
            langCode = "en";
        }
        else if (txt.Trim().ToLower() == "українська")
        {
            langCode = "uk";
        }
        else
        {
            await bot.SendMessage(chatId, "Будь ласка, оберіть мову за допомогою кнопок.", cancellationToken: ct);
            await RequestLanguage(bot, chatId, ct);
            return;
        }

        UserLanguages[chatId] = langCode;

        if (UserStates[chatId] == BotState.AwaitingLanguageForName)
        {
            UserStates[chatId] = BotState.AwaitingName;
            await bot.SendMessage(chatId, "Введіть назву рослини.", replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: ct);
        }
        else
        {
            await ShowPhotoModeMenu(bot, chatId, ct);
        }
    }

    private static async Task HandleAwaitingPhotoModeStateAsync(ITelegramBotClient bot, long chatId, string txt,
        CancellationToken ct)
    {
        if (txt.Trim().ToLower() == "один результат")
        {
            UserStates[chatId] = BotState.AwaitingPhotoSingle;
            await bot.SendMessage(chatId, "Надішліть фото рослини.", replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: ct);
        }
        else if (txt.Trim().ToLower() == "декілька результатів")
        {
            UserStates[chatId] = BotState.AwaitingPhotoMultiple;
            await bot.SendMessage(chatId, "Надішліть фото рослини.", replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: ct);
        }
        else
        {
            await bot.SendMessage(chatId, "Скористайтесь кнопками для вибору.", cancellationToken: ct);
            await ShowPhotoModeMenu(bot, chatId, ct);
        }
    }

    private static async Task HandleAwaitingNameStateAsync(ITelegramBotClient bot, long chatId, long userId,
        string plantName, CancellationToken ct)
    {
        var langForName = UserLanguages.GetValueOrDefault(chatId, "uk");
        await SearchByName(bot, chatId, userId, plantName, langForName, ct);
    }

    private static async Task HandleAwaitingNextActionStateAsync(ITelegramBotClient bot, long chatId, long userId,
        string? txt, CancellationToken ct)
    {
        switch (txt)
        {
            case "Переглянути історію":
                await ShowHistory(bot, chatId, userId, ct);
                break;
            case "Переглянути улюблені":
                await ShowFavourites(bot, chatId, userId, ct);
                break;
            case "Додати в улюблені":
                await StartAddFavourite(bot, chatId, ct);
                break;
            case "Видалити з улюблених":
                await StartDeleteFavourite(bot, chatId, ct);
                break;
            case "Пошук рослини":
                await ShowSearchMenu(bot, chatId, ct);
                break;
            default:
                await bot.SendMessage(chatId, "Будь ласка, оберіть дію за допомогою кнопок.", cancellationToken: ct);
                await ShowNextActionsMenu(bot, chatId, ct);
                break;
        }
    }

    private static async Task HandlePhotoMessageAsync(ITelegramBotClient bot, long chatId, long userId,
        PhotoSize[] photos, BotState currentState, CancellationToken ct)
    {
        var langForPhoto = UserLanguages.GetValueOrDefault(chatId, "uk");

        if (currentState == BotState.AwaitingPhotoSingle)
            await SearchByPhoto(bot, chatId, userId, photos, langForPhoto, ct);
        else if (currentState == BotState.AwaitingPhotoMultiple)
            await SearchMultipleByPhoto(bot, chatId, userId, photos, langForPhoto, ct);
    }

    private static async Task ShowSearchMenu(ITelegramBotClient bot, long chatId, CancellationToken ct)
    {
        var key = new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("Пошук за фото"), new KeyboardButton("Пошук за назвою") }
            })
        { ResizeKeyboard = true, OneTimeKeyboard = true };

        await bot.SendMessage(chatId, "Оберіть спосіб пошуку:", replyMarkup: key, cancellationToken: ct);
        UserStates[chatId] = BotState.ChoosingSearchMode;
    }

    private static async Task ShowPhotoModeMenu(ITelegramBotClient bot, long chatId, CancellationToken ct)
    {
        var kb = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("Один результат"), new KeyboardButton("Декілька результатів") }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };

        await bot.SendMessage(chatId, "Оберіть спосіб виводу результатів:",
            replyMarkup: kb, cancellationToken: ct);

        UserStates[chatId] = BotState.AwaitingPhotoMode;
    }

    private static async Task ShowNextActionsMenu(ITelegramBotClient bot, long chatId, CancellationToken ct)
    {
        var replyKeyboard = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "Переглянути історію", "Переглянути улюблені" },
            new KeyboardButton[] { "Додати в улюблені", "Видалити з улюблених" },
            new KeyboardButton[] { "Пошук рослини" }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };

        await bot.SendMessage(chatId, "Оберіть дію:",
            replyMarkup: replyKeyboard,
            cancellationToken: ct);
        UserStates[chatId] = BotState.AwaitingNextAction;
    }

    private static async Task RequestLanguage(ITelegramBotClient bot, long chatId, CancellationToken ct)
    {
        var langKeyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("Англійська"), new KeyboardButton("Українська") }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };

        await bot.SendMessage(chatId, "Оберіть мову пошуку рослини:",
            replyMarkup: langKeyboard, cancellationToken: ct);
    }

    private static async Task<string> AnalyzePlantHealth(ITelegramBotClient bot, long chatId, long userId,
        PhotoSize[] photos, string lang, CancellationToken ct)
    {
        try
        {
            var largestPhoto = photos[^1];
            var tgFile = await bot.GetFile(largestPhoto.FileId, ct);

            if (string.IsNullOrEmpty(tgFile.FilePath))
            {
                Console.WriteLine($"Error: FilePath is null or empty для наступного fileId: {largestPhoto.FileId}");
                return "Не вдалося отримати шлях до файлу для аналізу стану рослини.";
            }

            await using var imageStream = new MemoryStream();
            await bot.DownloadFile(tgFile.FilePath, imageStream, ct);
            imageStream.Position = 0;

            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"{ApiBaseUrl}/PlantState/analyzePlantHealth?lang={lang}");
            request.Headers.Add("UserId", userId.ToString());

            var content = new MultipartFormDataContent();
            content.Add(new StreamContent(imageStream), "image", "plant.jpg");
            request.Content = content;


            var response = await httpClient.SendAsync(request, ct);
            var responseString = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                var plantInfo = JsonConvert.DeserializeObject<PlantInfo>(responseString);

                return plantInfo!.PlantHealthAnalysis!;
            }
            else
            {
                var errorMessage = responseString;
                Console.WriteLine($"Error: {errorMessage} в AnalyzePlantHealth ");
                return "Помилка визначення стану рослини.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message} в AnalyzePlantHealth");
            return "Помилка визначення стану рослини.";
        }
    }

    private static async Task SearchByName(ITelegramBotClient bot, long chatId, long userId, string name, string lang,
        CancellationToken ct)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{ApiBaseUrl}/Wiki/{Uri.EscapeDataString(name)}?lang={lang}");
            request.Headers.Add("UserId", userId.ToString());

            var resp = await httpClient.SendAsync(request, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);


            var messageBuilder = new StringBuilder();
            if (resp.IsSuccessStatusCode)
            {
                var info = JsonConvert.DeserializeObject<PlantInfo>(json)!;
                messageBuilder.AppendLine($"<b>{HttpUtility.HtmlEncode(info.Plant)}</b>");
                if (!string.IsNullOrWhiteSpace(info.Description))
                    messageBuilder.AppendLine(HttpUtility.HtmlEncode(info.Description));
                if (!string.IsNullOrWhiteSpace(info.Summary))
                {
                    messageBuilder.AppendLine(HttpUtility.HtmlEncode(info.Summary));
                    messageBuilder.AppendLine($"{info.WikiUrl}\n");
                }

                var linkPreviewOptions = new LinkPreviewOptions
                { IsDisabled = false };
                await bot.SendMessage(chatId, messageBuilder.ToString(), ParseMode.Html,
                    linkPreviewOptions: linkPreviewOptions,
                    cancellationToken: ct);
            }
            else
            {
                var errorMessage = json;
                await bot.SendMessage(chatId, errorMessage, cancellationToken: ct);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message}");
            await bot.SendMessage(chatId, "Помилка пошуку за назвою.", cancellationToken: ct);
        }

        await ShowNextActionsMenu(bot, chatId, ct);
    }

    private static async Task SearchByPhoto(ITelegramBotClient bot, long chatId, long userId, PhotoSize[] photos,
        string lang,
        CancellationToken ct)
    {
        try
        {
            var healthAnalysisResult = await AnalyzePlantHealth(bot, chatId, userId, photos, lang, ct);
            var largest = photos[^1];
            var tgFile = await bot.GetFile(largest.FileId, ct);

            if (string.IsNullOrEmpty(tgFile.FilePath))
            {
                Console.WriteLine(
                    $"Error: FilePath is null or empty для наступного fileId: {largest.FileId} in SearchByPhoto");
                await bot.SendMessage(chatId, "Не вдалося отримати шлях до файлу для ідентифікації.",
                    cancellationToken: ct);
                await ShowNextActionsMenu(bot, chatId, ct);
                return;
            }

            await using var ms = new MemoryStream();
            await bot.DownloadFile(tgFile.FilePath, ms, ct);
            ms.Position = 0;

            var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBaseUrl}/PlantNet/identify?lang={lang}");
            request.Headers.Add("UserId", userId.ToString());

            var form = new MultipartFormDataContent
            {
                {
                    new ByteArrayContent(ms.ToArray())
                        { Headers = { ContentType = MediaTypeHeaderValue.Parse("image/jpeg") } },
                    "image", "plant.jpg"
                }
            };
            request.Content = form;

            var resp = await httpClient.SendAsync(request, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);

            var messageBuilder = new StringBuilder();
            if (lang == "uk")
            {
                messageBuilder.AppendLine($"<b>Стан рослини:</b> {HttpUtility.HtmlEncode(healthAnalysisResult)}\n");
                messageBuilder.AppendLine("<b>Найімовірніший результат:</b>");
            }
            else
            {
                messageBuilder.AppendLine(
                    $"<b>State of the plant:</b> {HttpUtility.HtmlEncode(healthAnalysisResult)}\n");
                messageBuilder.AppendLine("<b>The most possible result:</b>");
            }


            if (resp.IsSuccessStatusCode)
            {
                var info = JsonConvert.DeserializeObject<PlantInfo>(json)!;

                messageBuilder.AppendLine($"<b>{HttpUtility.HtmlEncode(info.Plant)}</b>");
                if (!string.IsNullOrWhiteSpace(info.Description))
                    messageBuilder.AppendLine(HttpUtility.HtmlEncode(info.Description));

                if (!string.IsNullOrWhiteSpace(info.Summary))
                {
                    messageBuilder.AppendLine(HttpUtility.HtmlEncode(info.Summary));
                    messageBuilder.AppendLine($"{info.WikiUrl}\n");
                }


                var linkPreviewOptions = new LinkPreviewOptions { IsDisabled = false };
                await bot.SendMessage(chatId, messageBuilder.ToString(), ParseMode.Html,
                    linkPreviewOptions: linkPreviewOptions,
                    cancellationToken: ct);
            }
            else
            {
                var errorMessage = json;
                await bot.SendMessage(chatId, errorMessage, cancellationToken: ct);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message} в SearchByPhoto");
            await bot.SendMessage(chatId, "Помилка пошуку за фото.", cancellationToken: ct);
        }

        await ShowNextActionsMenu(bot, chatId, ct);
    }

    private static async Task SearchMultipleByPhoto(
        ITelegramBotClient bot,
        long chatId,
        long userId,
        PhotoSize[] photos,
        string lang,
        CancellationToken ct)
    {
        try
        {
            var healthAnalysisResult = await AnalyzePlantHealth(bot, chatId, userId, photos, lang, ct);
            var largest = photos[^1];
            var tgFile = await bot.GetFile(largest.FileId, ct);

            if (string.IsNullOrEmpty(tgFile.FilePath))
            {
                Console.WriteLine(
                    $"Error: FilePath is null or empty для наступного fileId: {largest.FileId} in SearchMultipleByPhoto");
                await bot.SendMessage(chatId, "Не вдалося отримати шлях до файлу для ідентифікації.",
                    cancellationToken: ct);
                await ShowNextActionsMenu(bot, chatId, ct);
                return;
            }

            await using var ms = new MemoryStream();
            await bot.DownloadFile(tgFile.FilePath, ms, ct);
            ms.Position = 0;

            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{ApiBaseUrl}/PlantNet/identifyMultiple?lang={lang}&limit=3");
            request.Headers.Add("UserId", userId.ToString());

            var form = new MultipartFormDataContent
            {
                {
                    new ByteArrayContent(ms.ToArray())
                        { Headers = { ContentType = MediaTypeHeaderValue.Parse("image/jpeg") } },
                    "image", "plant.jpg"
                }
            };
            request.Content = form;


            var resp = await httpClient.SendAsync(request, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);

            var messageBuilder = new StringBuilder();
            if (lang == "uk")
                messageBuilder.AppendLine($"<b>Стан рослини:</b> {HttpUtility.HtmlEncode(healthAnalysisResult)}\n");
            else
                messageBuilder.AppendLine(
                    $"<b>State of the plant:</b> {HttpUtility.HtmlEncode(healthAnalysisResult)}\n");

            if (!resp.IsSuccessStatusCode)
            {
                var errorMessage = json;
                await bot.SendMessage(chatId, errorMessage, cancellationToken: ct);
                await ShowNextActionsMenu(bot, chatId, ct);
                return;
            }

            var list = JsonConvert.DeserializeObject<List<PlantInfo>>(json) ?? new List<PlantInfo>();
            if (list.Count == 0)
            {
                messageBuilder.AppendLine("Не вдалося знайти рослину за фото.");
                await bot.SendMessage(chatId, messageBuilder.ToString(), ParseMode.Html, cancellationToken: ct);
                await ShowNextActionsMenu(bot, chatId, ct);
                return;
            }

            var first = list[0];

            if (lang == "uk")
                messageBuilder.AppendLine("<b>Найімовірніший результат:</b>");
            else messageBuilder.AppendLine("<b>The most possible result:</b>");

            messageBuilder.AppendLine($"<b>{HttpUtility.HtmlEncode(first.Plant)}</b>");
            if (!string.IsNullOrWhiteSpace(first.Description))
                messageBuilder.AppendLine(HttpUtility.HtmlEncode(first.Description));
            if (!string.IsNullOrWhiteSpace(first.Summary))
            {
                messageBuilder.AppendLine(HttpUtility.HtmlEncode(first.Summary));
                messageBuilder.AppendLine($"{first.WikiUrl}\n");
            }


            if (list.Count > 1)
            {
                if (lang == "uk")
                    messageBuilder.AppendLine("<b>Альтернативні результати:</b>");
                else
                    messageBuilder.AppendLine("<b>Alternative results:</b>");

                foreach (var alt in list.Skip(1))
                {
                    messageBuilder.AppendLine($"<b>{HttpUtility.HtmlEncode(alt.Plant)}</b>");
                    if (!string.IsNullOrWhiteSpace(alt.Description))
                        messageBuilder.AppendLine(HttpUtility.HtmlEncode(alt.Description));
                    if (!string.IsNullOrWhiteSpace(alt.Summary))
                    {
                        messageBuilder.AppendLine(HttpUtility.HtmlEncode(alt.Summary));
                        messageBuilder.AppendLine($"{alt.WikiUrl}\n");
                    }
                }
            }

            var linkPreviewOptions = new LinkPreviewOptions { IsDisabled = false };
            await bot.SendMessage(chatId, messageBuilder.ToString(),
                ParseMode.Html, linkPreviewOptions: linkPreviewOptions, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message} в SearchMultipleByPhoto");
            await bot.SendMessage(chatId, "Помилка пошуку за фото.", cancellationToken: ct);
        }

        await ShowNextActionsMenu(bot, chatId, ct);
    }


    private static async Task ShowHistory(ITelegramBotClient bot, long chatId, long userId, CancellationToken ct)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBaseUrl}/History/history");
            request.Headers.Add("UserId", userId.ToString());

            var response = await httpClient.SendAsync(request, ct);
            var json = await response.Content.ReadAsStringAsync(ct);


            string text;

            if (response.IsSuccessStatusCode)
            {
                var list = JsonConvert.DeserializeObject<List<SearchHistory>>(json);
                if (list != null && list.Count == 0)
                    text = "Історія пошуку порожня.";
                else if (list != null)
                    text = "Історія пошуків:\n" +
                           string.Join("\n", list.Select(h => $"{h.PlantName} ({h.CreatedAt:d})"));
                else
                    text = "Не вдалося отримати історію.";
            }
            else
            {
                text = json;
            }

            await bot.SendMessage(chatId, text, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message} в ShowHistory");
            await bot.SendMessage(chatId, "Помилка виведення історії.", cancellationToken: ct);
        }

        await ShowNextActionsMenu(bot, chatId, ct);
    }

    private static async Task ShowFavourites(ITelegramBotClient bot, long chatId, long userId, CancellationToken ct)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBaseUrl}/Favourites/favourites");
            request.Headers.Add("UserId", userId.ToString());


            var response = await httpClient.SendAsync(request, ct);
            var json = await response.Content.ReadAsStringAsync(ct);

            string text;

            if (response.IsSuccessStatusCode)
            {
                var list = JsonConvert.DeserializeObject<List<Favourite>>(json);
                if (list != null && list.Count == 0)
                    text = "Список улюблених порожній.";
                else if (list != null)
                    text = "Улюблені рослини:\n" + string.Join("\n",
                        list.Select(f => $"{f.FavPlantName} (Додано: {f.AddedAt:d})"));
                else
                    text = "Не вдалося отримати список улюблених.";
            }
            else
            {
                text = json;
            }

            await bot.SendMessage(chatId, text, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message} в ShowFavourites");
            await bot.SendMessage(chatId, "Помилка виведення улюблених.", cancellationToken: ct);
        }

        await ShowNextActionsMenu(bot, chatId, ct);
    }

    private static async Task StartAddFavourite(ITelegramBotClient bot, long chatId, CancellationToken ct)
    {
        await bot.SendMessage(chatId, "Введіть назву рослини для додавання в улюблені:",
            replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
        UserStates[chatId] = BotState.AwaitingFavName;
    }

    private static async Task AddFavourite(ITelegramBotClient bot, long chatId, long userId, string plantName,
        CancellationToken ct)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{ApiBaseUrl}/Favourites/favourite?plantName={Uri.EscapeDataString(plantName.Trim())}");
            request.Headers.Add("UserId", userId.ToString());
            request.Content = new StringContent("", Encoding.UTF8, "application/json");

            var resp = await httpClient.SendAsync(request, ct);
            var responseString = await resp.Content.ReadAsStringAsync(ct);
            if (resp.IsSuccessStatusCode)
            {
                await bot.SendMessage(chatId, responseString, cancellationToken: ct);
            }
            else
            {
                var errorMessage = responseString;
                await bot.SendMessage(chatId, errorMessage, cancellationToken: ct);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message} в AddFavourite");
            await bot.SendMessage(chatId, "Помилка додаванні в улюблені.", cancellationToken: ct);
        }

        await ShowNextActionsMenu(bot, chatId, ct);
    }

    private static async Task StartDeleteFavourite(ITelegramBotClient bot, long chatId, CancellationToken ct)
    {
        await bot.SendMessage(chatId, "Введіть назву рослини для видалення з улюблених:",
            replyMarkup: new ReplyKeyboardRemove(), cancellationToken: ct);
        UserStates[chatId] = BotState.AwaitingFavDeleteName;
    }

    private static async Task DeleteFavourite(ITelegramBotClient bot, long chatId, long userId, string plantName,
        CancellationToken ct)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Delete,
                $"{ApiBaseUrl}/Favourites/favourite?plantName={Uri.EscapeDataString(plantName.Trim())}");
            request.Headers.Add("UserId", userId.ToString());

            var resp = await httpClient.SendAsync(request, ct);
            var responseString = await resp.Content.ReadAsStringAsync(ct);
            if (resp.IsSuccessStatusCode)
            {
                await bot.SendMessage(chatId, responseString, cancellationToken: ct);
            }
            else
            {
                var errorMessage = responseString;
                await bot.SendMessage(chatId, errorMessage, cancellationToken: ct);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex.Message} в DeleteFavourite");
            await bot.SendMessage(chatId, "Помилка додаванні в улюблені.", cancellationToken: ct);
        }

        await ShowNextActionsMenu(bot, chatId, ct);
    }

    private static Task HandleError(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        string? errorMessage;
        if (exception is Telegram.Bot.Exceptions.ApiRequestException apiRequestException)
            errorMessage = $"Telegram API Error:\n{apiRequestException.Message}";
        else
            errorMessage = exception.ToString();

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }
}