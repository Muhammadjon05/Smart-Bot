using Smart_Bot.Interfaces;
using Smart.Data.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Smart_Bot.Services;

public class HandleUpdateService
{
    private readonly IUpdateHandler _updateHandlerRepository;

    public HandleUpdateService(IUpdateHandler updateHandlerRepository)
    {
        _updateHandlerRepository = updateHandlerRepository;
    }

    public async Task HandleUpdateAsync(Update update)
    {
        var (message, chatId, messageId, isSuccess) = await GetData(update);
        var user = await _updateHandlerRepository.GetUser(chatId);
        
        if (!isSuccess)
        {
            return;
        }
        switch ((UserStep)user.Step)
        {
            case UserStep.Created:
                await _updateHandlerRepository.HandleUserName(user, update.Message!);
                break;
            case UserStep.Name:
                await _updateHandlerRepository.HandleNameStep(user, update.Message!);
                break;
            case UserStep.PhoneNumber:
                await _updateHandlerRepository.HandlePhoneNumberStep(user, update.Message!);
                break;
            case UserStep.Location:
                await _updateHandlerRepository.HandleLocationStep(user, update.Message!);
                break;
            case UserStep.Menu:
                await _updateHandlerRepository.HandleMenuStep(user, update.Message!);
                break;
            case UserStep.FastFoodMenu:
                await _updateHandlerRepository.HandleFastFood(user, update.Message!);
                break;
            case UserStep.FastFoodChoice:
                await _updateHandlerRepository.HandleFastFoodChoice(user, update.Message!);
                break;
            case UserStep.FastFood:
                await _updateHandlerRepository.HandleFastFoodAndSave(user, update.Message!);
                break;
            case UserStep.SaveOrder:
                if (update.Type == UpdateType.CallbackQuery)
                    await _updateHandlerRepository.HandleSaveOrder(user, update.CallbackQuery, null);
                else if (update.Type == UpdateType.Message)
                {
                    await _updateHandlerRepository.HandleSaveOrder(user, null, update.Message);
                }
                break;
            case UserStep.Cart:
                await _updateHandlerRepository.HandleCartAndSaveOrder(user, update.Message!);
                break;   
            case UserStep.OrderConfirmation:
                await _updateHandlerRepository.HandleOrderConfirmation(user, update.Message!);
                break;
        }
    }


    async Task<Tuple<string, long, int, bool>> GetData(Update update)
    {
        if (update.Type == UpdateType.Message)
        {
            return new(update.Message!.Text!, update.Message.From!.Id, update.Message.MessageId, true);
        }

        if (update.Type == UpdateType.CallbackQuery)
        {
            return new(update.CallbackQuery!.Data!, update.CallbackQuery.From.Id,
                update.CallbackQuery.Message!.MessageId, true);
        }

        return new(null, 0, 0, false);
    }
}