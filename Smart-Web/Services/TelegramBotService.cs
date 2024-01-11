using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Smart_Bot.Services;

public class TelegramBotService
{
    private readonly ITelegramBotClient _bot;

    public TelegramBotService(ITelegramBotClient bot)
    {
        _bot = bot;
    }

    public async Task SendMessage(long chatId, string message, IReplyMarkup? reply = null)
    {
        await _bot.SendTextMessageAsync(chatId, message, replyMarkup: reply);
    }

    public async Task SendMessage(long chatId, string message, Stream image, IReplyMarkup? reply = null)
    {
        await _bot.SendPhotoAsync(chatId, new InputFileStream(image), caption: message, replyMarkup: reply);
    } 

    public async Task Delete(long chatId, int msgId)
    {
        await _bot.DeleteMessageAsync(chatId, messageId: msgId);
    }

    public async Task EditMessageButtons(long chatId, int messageId, InlineKeyboardMarkup reply)
    {
        await _bot.EditMessageReplyMarkupAsync(chatId, messageId, replyMarkup: reply);
    }

    public ReplyKeyboardMarkup GetKeyboard(List<string> buttonsText)
    {
        return new ReplyKeyboardMarkup(buttonsText.Select(text =>
            new KeyboardButton[] { new(text) }))
        {
            ResizeKeyboard = true
        };
    }

    public ReplyKeyboardMarkup GenerateKeyboard(List<string> buttonTexts, string menuText = "Tanlang",
        bool resizeKeyboard = true)
    {
        var keyboardRows = new List<List<KeyboardButton>>();

        int buttonCount = buttonTexts.Count;
        int buttonsPerRow = 2;
        keyboardRows.Add(new List<KeyboardButton>() {"📥 Savatcha","⬅️ Ortga"});
        for (int i = 0; i < buttonCount; i += buttonsPerRow)
        {
            var rowButtons = buttonTexts
                .Skip(i)
                .Take(buttonsPerRow)
                .Select(buttonText => new KeyboardButton(buttonText))
                .ToList();
            keyboardRows.Add(rowButtons);
        }
        var keyboard = new ReplyKeyboardMarkup(keyboardRows)
        {
            ResizeKeyboard = resizeKeyboard
        };
        return keyboard;
    }
    public ReplyKeyboardMarkup GenerateKeyboardWithCart(List<string> buttonTexts, string menuText = "Tanlang",
        bool resizeKeyboard = true)
    {
        var keyboardRows = new List<List<KeyboardButton>>();
        int buttonCount = buttonTexts.Count;
        int buttonsPerRow = 2;
        for (int i = 0; i < buttonCount; i += buttonsPerRow)
        {
            var rowButtons = buttonTexts
                .Skip(i)
                .Take(buttonsPerRow)
                .Select(buttonText => new KeyboardButton(buttonText))
                .ToList();
            keyboardRows.Add(rowButtons);
        }
        var keyboard = new ReplyKeyboardMarkup(keyboardRows)
        {
            ResizeKeyboard = resizeKeyboard
        };
        return keyboard;
    }
    public InlineKeyboardMarkup GetInlineKeyboard(List<string> buttonsText)
    {
        return new InlineKeyboardMarkup(buttonsText.Select(text => new[]
        {
            InlineKeyboardButton.WithCallbackData(text: text, callbackData: text)
        }));
    }
}