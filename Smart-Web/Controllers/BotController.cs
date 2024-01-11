using Microsoft.AspNetCore.Mvc;
using Smart_Bot.Services;
using Telegram.Bot.Types;

namespace Smart_Bot.Controllers;

[ApiController]
[Route("/")]
public class BotController : ControllerBase
{
      
    [HttpPost]
    public async Task<IActionResult> Post([FromServices] HandleUpdateService updatesService, 
        [FromBody] Update update)
    {
        await updatesService.HandleUpdateAsync(update);
        return Ok();
    }

    
    
    [HttpGet]
    public string GetMe() => "Working...";
}