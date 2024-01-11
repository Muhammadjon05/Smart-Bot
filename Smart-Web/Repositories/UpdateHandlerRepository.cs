using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using Smart_Bot.Interfaces;
using Smart_Bot.Services;
using Smart_Domain.Entities;
using Smart.Data.ApplicationDbContext;
using Smart.Data.Enums;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;
using User = Smart_Domain.Entities.User;

namespace Smart_Bot.Repositories;

public class UpdateHandlerRepository : IUpdateHandler
{
    private readonly AppDbContext _appDbContext;
    private readonly IProductManager _productManager;
    private readonly TelegramBotService _botService;
    private readonly IMemoryCache _memoryCache;

    public UpdateHandlerRepository(AppDbContext appDbContext, IProductManager productManager,
      TelegramBotService botService,
        IMemoryCache memoryCache)
    {
        _appDbContext = appDbContext;
        _productManager = productManager;
        _botService = botService;
        _memoryCache = memoryCache;
    }

    public async ValueTask<User> GetUser(long chatId)
    {
        var user = await _appDbContext.Users
            .FirstOrDefaultAsync(u => u.ChatId == chatId);
        if (user is null)
        {
            var userEntity = new User
            {
                ChatId = chatId,
                Step = (int)UserStep.Created,
            };

            await _appDbContext.Users.AddAsync(userEntity);
            await _appDbContext.SaveChangesAsync();
            await _appDbContext.SaveChangesAsync();
        }

        return user;
    }

    public async Task HandleUserName(User user, Message updateMessage)
    {
        await _botService.SendMessage(updateMessage.Chat.Id,
            "Assalomu alekum botimizga hush kelibsiz,Ismingizni kiriting: ");
        user.Step = (int)UserStep.Name;
        user.Username = updateMessage.Chat!.Username;
        await _appDbContext.SaveChangesAsync();
    }


    public async Task HandleNameStep(User user, Message updateMessage)
    {
        user.Name = updateMessage.Text;
        await _appDbContext.SaveChangesAsync();
        KeyboardButton button = KeyboardButton.WithRequestContact("📞 Raqamni jonatish");
        ReplyKeyboardMarkup keyboardMarkup = new ReplyKeyboardMarkup(button);
        keyboardMarkup.ResizeKeyboard = true;
        await _botService.SendMessage(updateMessage.Chat.Id, "Raqamingizni jonating", keyboardMarkup);
        user.Step = (int)UserStep.PhoneNumber;
        await _appDbContext.SaveChangesAsync();
    }

    public async Task HandlePhoneNumberStep(User user, Message updateMessage)
    {
        if (updateMessage.Contact == null || string.IsNullOrEmpty(updateMessage.Contact.PhoneNumber))
        {
            user.Step = (int)UserStep.PhoneNumber;
            await _appDbContext.SaveChangesAsync();
            KeyboardButton button = KeyboardButton.WithRequestContact("📞 Raqamni jonatish");
            ReplyKeyboardMarkup keyboardMarkup = new ReplyKeyboardMarkup(button);
            keyboardMarkup.ResizeKeyboard = true;
            await _botService.SendMessage(updateMessage.Chat.Id, "Iltimos, telefon raqamingizni jo'nating",
                keyboardMarkup);
            return;
        }
        else
        {
            user.Phone = updateMessage.Contact.PhoneNumber;
            await _appDbContext.SaveChangesAsync();
            await _botService.SendMessage(updateMessage.Chat.Id, "Raqamingiz tizimda ro'yhatga olindi");
            KeyboardButton button = KeyboardButton.WithRequestLocation("📍 Manzilni jo'natish");
            ReplyKeyboardMarkup keyboardMarkup = new ReplyKeyboardMarkup(button);
            keyboardMarkup.ResizeKeyboard = true;
            await _botService.SendMessage(updateMessage.Chat.Id, "Manziligizni jonating", keyboardMarkup);
            user.Step = (int)UserStep.Location;
            await _appDbContext.SaveChangesAsync();
        }
    }

    public async Task HandleLocationStep(User user, Message updateMessage)
    {
        if (updateMessage.Location is null)
        {
            user.Step = (int)UserStep.Location;
            await _appDbContext.SaveChangesAsync();
            var replyMarkup = new ReplyKeyboardMarkup(new[]
            {
                new KeyboardButton("📍 Manzilni jo'natish") { RequestLocation = true }
            });
            replyMarkup.ResizeKeyboard = true;
            await _botService.SendMessage(updateMessage.Chat.Id, "Iltimos, manzilingizni jo'nating",
                replyMarkup);
        }
        else
        {
            double latitude = updateMessage.Location.Latitude;
            double longitude = updateMessage.Location.Longitude;
            await _appDbContext.SaveChangesAsync();
            var locationService = new LocationService();
            var address = await locationService.GetLocationName(latitude, longitude);
            if (string.Equals(address.country, "Uzbekistan", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(address.city, "Andijan Region", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(address.state, "Andijan Region", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(address.county, "Marhamat District", StringComparison.OrdinalIgnoreCase))
                    {
                        user.Location = $"https://www.google.com/maps/search/?api=1&query={latitude},{longitude}";
                        user.Latitude = latitude;
                        user.Longitude = longitude;
                        await _appDbContext.SaveChangesAsync();

                        var cart = new KeyboardButton("📥 Savatcha");
                        var menu = new KeyboardButton("🍽️📝 Menu");
                        var orders = new KeyboardButton("🛒 Buyurtmalar");
                        var keyboard = new ReplyKeyboardMarkup(new[]
                        {
                            new[] { menu, orders },
                            new[] { cart }
                        });
                        keyboard.ResizeKeyboard = true;
                        await _botService.SendMessage(updateMessage.Chat.Id, $"Manzilingiz tizimda saqlandi", keyboard);
                        user.Step = (int)UserStep.Menu;
                        await _appDbContext.SaveChangesAsync();
                    }
                    else
                    {
                        user.Step = (int)UserStep.Location;
                        await _appDbContext.SaveChangesAsync();
                        var replyMarkup = new ReplyKeyboardMarkup(new[]
                        {
                            new KeyboardButton("📍 Manzilni jo'natish") { RequestLocation = true }
                        });
                        replyMarkup.ResizeKeyboard = true;
                        await _botService.SendMessage(updateMessage.Chat.Id,
                            "Bizing qamrov hudimiz faqatgina Marhamat tumani ichida, iltimos Marhamat tumani da manzilingizni jo'nating !!!",
                            replyMarkup);
                    }
                }
                else
                {
                    user.Step = (int)UserStep.Location;
                    await _appDbContext.SaveChangesAsync();
                    var replyMarkup = new ReplyKeyboardMarkup(new[]
                    {
                        new KeyboardButton("📍 Manzilni jo'natish") { RequestLocation = true }
                    });
                    replyMarkup.ResizeKeyboard = true;
                    await _botService.SendMessage(updateMessage.Chat.Id,
                        "Bizing qamrov hudimiz faqatgina Marhamat tumani ichida, iltimos Marhamat tumani da manzilingizni jo'nating !!!",
                        replyMarkup);
                }
            }
            else
            {
                user.Step = (int)UserStep.Location;
                await _appDbContext.SaveChangesAsync();
                var replyMarkup = new ReplyKeyboardMarkup(new[]
                {
                    new KeyboardButton("📍 Manzilni jo'natish") { RequestLocation = true }
                });
                replyMarkup.ResizeKeyboard = true;
                await _botService.SendMessage(updateMessage.Chat.Id,
                    "Bizing qamrov hudimiz faqatgina Marhamat tumani ichida, iltimos Marhamat tumani da manzilingizni jo'nating !!!",
                    replyMarkup);
            }
        }
    }

    public async Task HandleMenuStep(User user, Message updateMessage)
    {
        if (updateMessage.Text == "🍽️📝 Menu")
        {
            var fastFood = new KeyboardButton("🍔🍟 FastFood");
            var menu = new KeyboardButton("⬅️ Ortga");
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { fastFood, menu },
            });
            keyboard.ResizeKeyboard = true;
            await _botService.SendMessage(user.ChatId, "Menuni tanlang", keyboard);
            user.Step = (int)UserStep.FastFoodMenu;
            await _appDbContext.SaveChangesAsync();
        }
        else if (updateMessage.Text == "🛒 Buyurtmalar")
        {
            var orders = await _appDbContext.Orders.Where(i => i.UserId == user.Id)
                .ToListAsync();
            var cart = new KeyboardButton("📥 Savatcha");
            var menu = new KeyboardButton("🍽️📝 Menu");
            var orderButton = new KeyboardButton("🛒 Buyurtmalar");

            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { menu, orderButton },
                new[] { cart }
            });
            keyboard.ResizeKeyboard = true;
            var orderItems = orders.Select(i => i.OrderItems).ToList();
            if (orderItems.Count is 0 || orderItems is null)
            {
                await _botService.SendMessage(user.ChatId, "Siz hali buyurtma qilmagansiz! 🙄", keyboard);
                user.Step = (int)UserStep.Menu;
                await _appDbContext.SaveChangesAsync();
            }
            else
            {
                foreach (var order in orders)
                {
                    var message = new StringBuilder();
                    message.AppendLine($"🔢 Buyurtma raqami: {order.OrderId}\n" +
                                       $"📅 Sana: {order.OrderDate.ToString("d-M-yyyy")}\n" +
                                       $"⌚️ Vaqt: {order.OrderDate.Hour}:{order.OrderDate.Minute}\n");

                    long orderTotalPrice = 0;
                    foreach (var orderItem in order.OrderItems)
                    {
                        var product = await _productManager.GetProductById(orderItem.ProductId);

                        message.AppendLine($"📦 Mahsulot nomi: {product.ProductName}\n" +
                                           $"💰 Mahsulot narxi: {Convert.ToInt64(product.ProductPrice)}\n" +
                                           $"🔢 Sanoq: {orderItem.Quantity}\n");
                        long orderItemTotalPrice = (long)product.ProductPrice * orderItem.Quantity;
                        orderTotalPrice += orderItemTotalPrice;
                    }

                    orderTotalPrice += 10000;

                    message.AppendLine($"💲 To'lov usuli: Naqt\n" +
                                       $"💲 Yetkazib berish narxi: 10.000\n");

                    message.AppendLine($"💵 Umumiy buyurtma narxi: {orderTotalPrice}\n");

                    await _botService.SendMessage(user.ChatId, message.ToString(), keyboard);
                }

                user.Step = (int)UserStep.Menu;
                await _appDbContext.SaveChangesAsync();
            }
        }
        else if (updateMessage.Text == "📥 Savatcha")
        {
            await DisplayCart(user);
        }
    }

    public async Task HandleFastFood(User user, Message updateMessage)
    {
        if (updateMessage.Text == "🍔🍟 FastFood")
        {
            var products = await _productManager.GetAllProducts();
            var productTypes = products.Select(p => p.ProductType).Distinct().ToList();
            await _botService.SendMessage(user.ChatId, "Nimadan boshlaymiz? ",
                _botService.GenerateKeyboard(productTypes));
            user.Step = (int)UserStep.FastFoodChoice;
            await _appDbContext.SaveChangesAsync();
        }
        else if (updateMessage.Text == "⬅️ Ortga")
        {
            var cart = new KeyboardButton("📥 Savatcha");
            var menu = new KeyboardButton("🍽️📝 Menu");
            var orders = new KeyboardButton("🛒 Buyurtmalar");

            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { menu, orders },
                new[] { cart }
            });
            keyboard.ResizeKeyboard = true;
            await _botService.SendMessage(user.ChatId, "Assalomu alekum yana bir bor menudan tanlang",
                keyboard);
            user!.Step = (int)UserStep.Menu;
            await _appDbContext.SaveChangesAsync();
        }
    }

    public async Task HandleFastFoodChoice(User user, Message updateMessage)
    {
        var products = await _productManager.GetAllProducts();
        if (updateMessage.Text == "⬅️ Ortga")
        {
            var cart = new KeyboardButton("📥 Savatcha");
            var menu = new KeyboardButton("🍽️📝 Menu");
            var orders = new KeyboardButton("🛒 Buyurtmalar");

            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { menu, orders },
                new[] { cart }
            });
            keyboard.ResizeKeyboard = true;
            await _botService.SendMessage(user.ChatId, "Assalomu alekum yana bir bor menudan tanlang",
                keyboard);
            user!.Step = (int)UserStep.Menu;
            await _appDbContext.SaveChangesAsync();
        }
        else if (updateMessage.Text == "📥 Savatcha")
        {
            await DisplayCart(user);
        }
        else
        {
            var selectedProductType = products
                .Select(p => p.ProductType)
                .FirstOrDefault(type => updateMessage.Text!.Contains(type));
            if (selectedProductType != null)
            {
                var matchingProducts = products
                    .Where(p => p.ProductType.Equals(selectedProductType))
                    .ToList();

                var productNames = matchingProducts.Select(p => p.ProductName).ToList();
                await _botService.SendMessage(user.ChatId, $"{selectedProductType} ni tanlang",
                    _botService.GenerateKeyboard(productNames));
                user.Step = (int)UserStep.FastFood;
                await _appDbContext.SaveChangesAsync();
            }
            else
            {
                await _botService.SendMessage(user.ChatId,
                    "Noto'g'ri tanlov. Iltimos, mavjud bo'lganlardan tanlang.",
                    _botService.GenerateKeyboard(products.Select(p => p.ProductType).Distinct().ToList()));
            }
        }
    }

    public async Task HandleFastFoodAndSave(User user, Message updateMessage)
    {
        if (!_memoryCache.TryGetValue(user.Id, out int orderCount))
        {
            orderCount = 1;
            _memoryCache.Set(user.Id, orderCount);
        }

        var products = await _productManager.GetAllProducts();
        if (updateMessage.Text == "⬅️ Ortga")
        {
            await _botService.SendMessage(user.ChatId, "Nimadan boshlaymiz? ",
                _botService.GenerateKeyboard(products.Select(p => p.ProductType).Distinct().ToList()));
            user.Step = (int)UserStep.FastFoodChoice;
            await _appDbContext.SaveChangesAsync();
        }
        else if (updateMessage.Text == "📥 Savatcha")
        {
            await DisplayCart(user);
        }
        else
        {
            var product = products.FirstOrDefault(p => p.ProductName.Equals(updateMessage.Text));
            if (product is null)
            {
                user.Step = (int)UserStep.FastFoodChoice;
                await _appDbContext.SaveChangesAsync();
                await _botService.SendMessage(user.ChatId,
                    "Noto'g'ri tanlov. Iltimos, mavjud bo'lganlardan tanlang.",
                    _botService.GenerateKeyboard(products.Select(p => p.ProductType).Distinct().ToList()));
            }
            else
            {
                user.CurrentProductId = product.ProductId;
                var rows = new List<List<InlineKeyboardButton>>();
                var row = new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("-"),
                    InlineKeyboardButton.WithCallbackData($"{orderCount}"),
                    InlineKeyboardButton.WithCallbackData("+"),
                };
                var extra = new List<InlineKeyboardButton>()
                {
                    InlineKeyboardButton.WithCallbackData("📥 Savatga qo'shish"),
                };
                rows.Add(row);
                rows.Add(extra);
                var nonInlineButton = new KeyboardButton("📥 Savatcha");
                var button = new KeyboardButton("⬅️ Ortga");
                var keyboard = new ReplyKeyboardMarkup(new List<List<KeyboardButton>>()
                    { new List<KeyboardButton>() { nonInlineButton, button } });
                keyboard.ResizeKeyboard = true;

                if (product.Media.Exist)
                {
                    string relativePath =
                        $"C:\\Users\\muham\\RiderProjects\\Smart-Bot\\Smart-Web\\wwwroot\\ProductImages/{product.Media.ImageUrl}";
                    string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
                    var fileBytes = File.ReadAllBytes(fullPath);
                    var ms = new MemoryStream(fileBytes);
                    Stream s = ms;
                    user.Step = (int)UserStep.SaveOrder;
                    string message = $"{product.ProductName} \n \n" +
                                     $"{product.ProductDescription} \n \n" +
                                     $"Narxi: {product.ProductPrice} so'm";
                    await _botService.SendMessage(chatId: user.ChatId,
                        message: message, image: s, new InlineKeyboardMarkup(rows));
                    await _appDbContext.SaveChangesAsync();
                }
                else if (!product.Media.Exist)
                {
                    string message = $"{product.ProductName} \n \n" +
                                     $"{product.ProductDescription} \n \n" +
                                     $"Narxi: {product.ProductPrice} so'm";
                    await _botService.SendMessage(chatId: user.ChatId,
                        message: message, new InlineKeyboardMarkup(rows));

                    user.Step = (int)UserStep.SaveOrder;
                    await _appDbContext.SaveChangesAsync();
                }

                await _botService.SendMessage(chatId: user.ChatId, message: "Tanlang", reply: keyboard);
            }
        }
    }

    public async Task HandleSaveOrder(User user, CallbackQuery? updateMessage, Message? message = null)
    {
        if (!_memoryCache.TryGetValue(user.Id, out int orderCount))
        {
            orderCount = 1;
            _memoryCache.Set(user.Id, orderCount);
        }

        if (updateMessage?.Data == "+")
        {
            orderCount++;
            await UpdateOrderMessage(user.ChatId, updateMessage.Message!.MessageId, orderCount);
            _memoryCache.Set(user.Id, orderCount);
        }
        else if (updateMessage?.Data == "-")
        {
            if (orderCount == 1)
            {
                if (!string.IsNullOrEmpty(updateMessage.Data))
                {
                    var rows = new List<List<InlineKeyboardButton>>();
                    var row = new List<InlineKeyboardButton>()
                    {
                        InlineKeyboardButton.WithCallbackData($"{orderCount}"),
                        InlineKeyboardButton.WithCallbackData("+"),
                    };
                    var extra = new List<InlineKeyboardButton>()
                    {
                        InlineKeyboardButton.WithCallbackData("📥 Savatga qo'shish"),
                    };
                    rows.Add(row);
                    rows.Add(extra);
                    await _botService.EditMessageButtons(user.ChatId,
                        updateMessage.Message!.MessageId, reply: new InlineKeyboardMarkup(rows));
                }
            }
            else
            {
                orderCount--;
                await UpdateOrderMessage(user.ChatId, updateMessage.Message!.MessageId, orderCount);
                _memoryCache.Set(user.Id, orderCount);
            }
        }
        else if (updateMessage?.Data == "📥 Savatga qo'shish")
        {
            var cart = await IsCartExists(user);
            var existingProduct = cart.CartItems.FirstOrDefault(c => c.ProductId == user.CurrentProductId);
            if (existingProduct is not null)
            {
                existingProduct.Quantity += orderCount;
                _appDbContext.CartItems.Update(existingProduct);
                await _appDbContext.SaveChangesAsync();
                orderCount = 1;
                _memoryCache.Set(user.Id, orderCount);
                var cartButton = new KeyboardButton("📥 Savatcha");
                var menu = new KeyboardButton("🍽️📝 Menu");
                var orders = new KeyboardButton("🛒 Buyurtmalar");

                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new[] { menu, orders },
                    new[] { cartButton }
                });
                keyboard.ResizeKeyboard = true;
                await _botService.SendMessage(user.ChatId, "Savatchaga qabul qilindi",
                    keyboard);
                await _botService.Delete(user.ChatId, updateMessage.Message!.MessageId);
                user.Step = (int)UserStep.Menu;
                await _appDbContext.SaveChangesAsync();
            }
            else
            {
                var cartItem = new CartItem()
                {
                    CartId = cart.CartId,
                    ProductId = user.CurrentProductId,
                    Quantity = orderCount
                };
                await _appDbContext.CartItems.AddAsync(cartItem);
                await _appDbContext.SaveChangesAsync();

                orderCount = 1;
                _memoryCache.Set(user.Id, orderCount);
                var cartButton = new KeyboardButton("📥 Savatcha");
                var menu = new KeyboardButton("🍽️📝 Menu");
                var orders = new KeyboardButton("🛒 Buyurtmalar");

                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new[] { menu, orders },
                    new[] { cartButton }
                });
                keyboard.ResizeKeyboard = true;
                await _botService.SendMessage(user.ChatId, "Savatchaga qabul qilindi",
                    keyboard);
                await _botService.Delete(user.ChatId, updateMessage.Message!.MessageId);
                user.Step = (int)UserStep.Menu;
                await _appDbContext.SaveChangesAsync();
            }
        }
        else if (message?.Text == "📥 Savatcha")
        {
            await DisplayCart(user);
        }
        else if (message?.Text == "⬅️ Ortga")
        {
            var products = await _productManager.GetAllProducts();
            await _botService.SendMessage(user.ChatId, "Nimadan boshlaymiz? ",
                _botService.GenerateKeyboard(products.Select(p => p.ProductType).Distinct().ToList()));
            _memoryCache.Set(user.Id, 1);
            user.Step = (int)UserStep.FastFoodChoice;
            await _appDbContext.SaveChangesAsync();
        }
        else
        {
            user.Step = (int)UserStep.SaveOrder;
            await _appDbContext.SaveChangesAsync();
            await _botService.SendMessage(user.ChatId, "Iltimos, berilgan buyruqlardan birini tanlang");
        }
    }

    public async Task HandleCartAndSaveOrder(User user, Message updateMessage)
    {
        if (updateMessage.Text == "🧹 To'zalash")
        {
            var cart = await IsCartExists(user);
            _appDbContext.CartItems.RemoveRange(cart.CartItems);
            await _appDbContext.SaveChangesAsync();
            var cartButton = new KeyboardButton("📥 Savatcha");
            var menu = new KeyboardButton("🍽️📝 Menu");
            var orders = new KeyboardButton("🛒 Buyurtmalar");

            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { menu, orders },
                new[] { cartButton }
            });
            keyboard.ResizeKeyboard = true;
            await _botService.SendMessage(updateMessage.Chat.Id,
                $"Savatingiz to'zalandi😊,Menyudan yana mahsulot qoshishingiz mumkin ", keyboard);
            user.Step = (int)UserStep.Menu;
            await _appDbContext.SaveChangesAsync();
        }
        else if (updateMessage.Text == "⬅️ Ortga")
        {
            var products = await _productManager.GetAllProducts();
            await _botService.SendMessage(user.ChatId, "Nimadan boshlaymiz? ",
                _botService.GenerateKeyboard(products.Select(p => p.ProductType).Distinct().ToList()));
            user.Step = (int)UserStep.FastFoodChoice;
            await _appDbContext.SaveChangesAsync();
        }
        else if (updateMessage.Text == "🚖 Buyurtuma berish")
        {
            var back = new KeyboardButton("⬅️ Ortga");
            var yes = new KeyboardButton("✅ Ha");
            var no = new KeyboardButton("❌ Yoq,yana mahsulot qo'shaman");
            var addOrder = new KeyboardButton("➕ Mahsulot qo'shish");

            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { yes, no },
                new[] { back, addOrder }
            });
            keyboard.ResizeKeyboard = true;


            var orderItems = new List<OrderItem>();
            decimal total = 0;
            foreach (var cartItem in user.Cart.CartItems)
            {
                var product = await _productManager.GetProductById(cartItem.ProductId);
                var orderItem = new OrderItem
                {
                    ProductId = product.ProductId,
                    Quantity = cartItem.Quantity
                };
                orderItems.Add(orderItem);
                total += product.ProductPrice * cartItem.Quantity;
            }

            var message = "";
            foreach (var o in orderItems)
            {
                var product = await _productManager.GetProductById(o.ProductId);
                message +=
                    $"📦 Mahsulot nomi: {product.ProductName}\n" +
                    $"🔢 Sanoq: {o.Quantity}\n" +
                    $"💰 Narxi: {product.ProductPrice} so'm\n\n";
            }

            message += $"\n Jami: {(long)total} so'm";
            await _botService.SendMessage(user.ChatId,
                $"Siz buyurtma qilmoqchi bo'lgan mahsulotlar bular \n {message} \n Buyurmani tasdqilaysizmi ?",
                keyboard);
            user.Step = (int)UserStep.OrderConfirmation;
            await _appDbContext.SaveChangesAsync();
        }
    }

    public async Task HandleOrderConfirmation(User user, Message updateMessage)
    {
        if (updateMessage.Text == "⬅️ Ortga")
        {
            var cart = new KeyboardButton("📥 Savatcha");
            var menu = new KeyboardButton("🍽️📝 Menu");
            var orders = new KeyboardButton("🛒 Buyurtmalar");

            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { menu, orders },
                new[] { cart }
            });
            keyboard.ResizeKeyboard = true;
            await _botService.SendMessage(user.ChatId, "Assalomu alekum yana bir bor menudan tanlang",
                keyboard);
            user!.Step = (int)UserStep.Menu;
            await _appDbContext.SaveChangesAsync();
        }
        else if (updateMessage.Text == "✅ Ha")
        {
            decimal total = 0;
            var orderItems = new List<OrderItem>();
            var order = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.Now
            };
            await _appDbContext.Orders.AddAsync(order);
            await _appDbContext.SaveChangesAsync();
            foreach (var cartItem in user.Cart.CartItems)
            {
                var product = await _productManager.GetProductById(cartItem.ProductId);
                var orderItem = new OrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = product.ProductId,
                    Quantity = cartItem.Quantity,
                };
                orderItems.Add(orderItem);
                total += product.ProductPrice * cartItem.Quantity;
            }

            await _appDbContext.OrderItems.AddRangeAsync(orderItems);
            await _appDbContext.SaveChangesAsync();
            _appDbContext.CartItems.RemoveRange(user.Cart.CartItems);
            await _appDbContext.SaveChangesAsync();
            var message =
                $"Sizning buyurtmangiz qabul qilindi o'zimiz siz bilan bog'lanamiz buyurtmangiz 25-30 daqiqa ichida yetkazib beriladi😊\n";
            var orderLogMessage = $"🛒 Foydalanuvchi {user.Name}. Username :@{user.Username} buyurtma berdi!\n";
            foreach (var o in orderItems)
            {
                var product = await _productManager.GetProductById(o.ProductId);

                orderLogMessage +=
                    $"📦 Mahsulot nomi : {product.ProductName}\n" +
                    $"💰  Mahsulot narxi : {product.ProductPrice}\n" +
                    $"🔢 Sanoq: {o.Quantity}\n\n";
            }

            var orderDate = order.OrderDate;
            var formattedDate =
                $"📅Sana:{orderDate.ToString("d-M-yyyy")}\n" +
                $"⌚Vaqt:{orderDate.Hour}:{orderDate.Minute}\n";

            message += $"{orderLogMessage}" +
                       $"{formattedDate}" +
                       $"💲To'lov usuli: Naqt \n" +
                       $"💲Yetkazib berish narxi: 10.000\n\n" +
                       $"💵Jami:{(long)total + 10000} so'm";
            Log.Information(orderLogMessage, "Buyurtma qo'shildi!");

            orderLogMessage += $"💲To'lov usuli: Naqt\n" +
                               $"💲Yetkazib berish narxi: 10.000\n" +
                               $"{formattedDate}" +
                               $"📍 Manzil: {user.Location}\n" +
                               $"📞 Telefon raqami: {user.Phone}\n\n" +
                               $"💵Jami:{(long)total + 10000} so'm\n" +
                               $"🚀 Buyurtma qo'shildi!";

            await _botService.SendMessage(-1002045810454, $"{orderLogMessage} ");
            await _botService.SendMessage(user.ChatId, message);
            var cart = new KeyboardButton("📥 Savatcha");
            var menu = new KeyboardButton("🍽️📝 Menu");
            var orders = new KeyboardButton("🛒 Buyurtmalar");

            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { menu, orders },
                new[] { cart }
            });
            keyboard.ResizeKeyboard = true;
            await _botService.SendMessage(user.ChatId, "Buyurtmangiz qabul qilindi,menudan tanlang",
                keyboard);
            user!.Step = (int)UserStep.Menu;
            await _appDbContext.SaveChangesAsync();
        }
        else if (updateMessage.Text == "❌ Yoq,yana mahsulot qo'shaman")
        {
            var products = await _productManager.GetAllProducts();
            await _botService.SendMessage(user.ChatId, "Nimadan boshlaymiz? ",
                _botService.GenerateKeyboard(products.Select(p => p.ProductType).Distinct().ToList()));
            user.Step = (int)UserStep.FastFoodChoice;
            await _appDbContext.SaveChangesAsync();
        }
        else if (updateMessage.Text == "➕ Mahsulot qo'shish")
        {
            var products = await _productManager.GetAllProducts();
            await _botService.SendMessage(user.ChatId, "Nimadan boshlaymiz? ",
                _botService.GenerateKeyboard(products.Select(p => p.ProductType).Distinct().ToList()));
            user.Step = (int)UserStep.FastFoodChoice;
            await _appDbContext.SaveChangesAsync();
        }
        else
        {
            await _botService.SendMessage(user.ChatId, "Iltimos,berilgan buyruqlardan birini tanlang");
        }
    }

    private async Task<Cart> IsCartExists(User user)
    {
        var existingCart = await _appDbContext.Carts
            .FirstOrDefaultAsync(c => c.UserId == user.Id);
        if (existingCart is null)
        {
            var newCart = new Cart()
            {
                UserId = user.Id,
                DateCreated = DateTime.Now
            };

            await _appDbContext.Carts.AddAsync(newCart);
            await _appDbContext.SaveChangesAsync();
            user.CartId = newCart.CartId;
            return newCart;
        }
        else
        {
            user.CartId = existingCart.CartId;
            return existingCart;
        }
    }

    public async Task DisplayCart(User user)
    {
        var cart = await IsCartExists(user);

        if (cart.CartItems.Count == 0 || cart.CartItems is null)
        {
            await _botService.SendMessage(user.ChatId, "Savatchangiz bo'sh");
        }
        else
        {
            decimal total = 0;
            var cartMessage = "";

            foreach (var product in cart.CartItems)
            {
                var productById = await _productManager.GetProductById(product.ProductId);
                cartMessage += $"📦 {productById.ProductName}\n {product.Quantity} x {productById.ProductPrice}\n";
                total += (decimal)product.Quantity * (decimal)productById.ProductPrice;
            }

            cartMessage += $"\n Umumiy: {total}";
            var clear = new KeyboardButton("🧹 To'zalash");
            var order = new KeyboardButton("🚖 Buyurtuma berish");
            var back = new KeyboardButton("⬅️ Ortga");

            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { order, clear },
                new[] { back }
            });
            keyboard.ResizeKeyboard = true;
            //var keyboard = _botService.GenerateKeyboard(new List<string>() { "🧹 To'zalash", "🚖 Buyurtuma berish" });
            await _botService.SendMessage(user.ChatId, cartMessage, keyboard);
            user.Step = (int)UserStep.Cart;
            await _appDbContext.SaveChangesAsync();
        }
    }

    private async Task UpdateOrderMessage(long chatId, int messageId, int orderCount)
    {
        var rows = new List<List<InlineKeyboardButton>>();
        var row = new List<InlineKeyboardButton>()
        {
            InlineKeyboardButton.WithCallbackData("-"),
            InlineKeyboardButton.WithCallbackData($"{orderCount}"),
            InlineKeyboardButton.WithCallbackData("+"),
        };
        var extra = new List<InlineKeyboardButton>()
        {
            InlineKeyboardButton.WithCallbackData("📥 Savatga qo'shish"),
        };
        rows.Add(row);
        rows.Add(extra);
        try
        {
            await _botService.EditMessageButtons(chatId, messageId, reply: new InlineKeyboardMarkup(rows));
        }
        catch (ApiRequestException ex)
        {
            Console.WriteLine($"Error editing message: {ex.Message}");
        }
    }
}