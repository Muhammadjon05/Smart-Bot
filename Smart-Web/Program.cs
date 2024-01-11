
using Microsoft.EntityFrameworkCore;
using Serilog;
using Smart_Bot.Interfaces;
using Smart_Bot.Repositories;
using Smart_Bot.Services;
using Smart.Data.ApplicationDbContext;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<TelegramBotService>();

builder.Services.AddTransient<IUpdateHandler, UpdateHandlerRepository>();
builder.Services.AddScoped<IProductManager, ProductManager>();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString: connectionString, ServerVersion.AutoDetect(connectionString)));
builder.Services.AddHostedService<ConfigurationWebHook>();
builder.Services.AddScoped<HandleUpdateService>();

builder.Host.UseSerilog((context, configuration) =>
{
    configuration.WriteTo.File("log.txt", rollingInterval: RollingInterval.Minute);
});


builder.Services.AddHttpClient("tgwebhook").AddTypedClient<ITelegramBotClient>(client =>
    new TelegramBotClient("6470639945:AAE-jgG91xsJFM69NjM-HBvjppKwEsLT06A", client));
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseCors();
app.UseRouting();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();