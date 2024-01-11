using Telegram.Bot.Types;
using User = Smart_Domain.Entities.User;

namespace Smart_Bot.Interfaces;

public interface IUpdateHandler
{
    ValueTask<User> GetUser(long chatId);
    
    Task HandleUserName(User user, Message updateMessage);
    Task HandleNameStep(User user, Message updateMessage);
    Task HandlePhoneNumberStep(User user, Message updateMessage);
    Task HandleLocationStep(User user, Message updateMessage);
    Task HandleMenuStep(User user, Message updateMessage);
    Task HandleFastFood(User user, Message updateMessage);
    Task HandleFastFoodChoice(User user, Message updateMessage);
    
    Task HandleFastFoodAndSave(User user, Message updateMessage);
    
    Task HandleSaveOrder(User user, CallbackQuery? update , Message? message = null);
    Task HandleCartAndSaveOrder(User user, Message updateMessage);
    Task HandleOrderConfirmation(User user, Message updateMessage);

}