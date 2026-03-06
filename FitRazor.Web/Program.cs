using FitRazor.Data;
using FitRazor.Data.Models;
using Microsoft.AspNetCore.Identity;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataServices(builder.Configuration);

// Настройка Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.File(
        Path.Combine(Directory.GetCurrentDirectory(), "Logs", "FitRazor-.txt"),
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

// Используем Serilog как основной провайдер логов
builder.Host.UseSerilog();

// 🔐 Настройка Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Настройки пароля
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 1;
    options.Password.RequireNonAlphanumeric = false;

    // 🔥 Вход по логину, а не по email
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;

    // Имя пользователя как логин
    options.User.RequireUniqueEmail = false; // email не уникален и не обязателен
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ ";
})
.AddEntityFrameworkStores<FitRazorContext>()
.AddDefaultTokenProviders();

// 🔐 Настройка авторизации
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("TrainerOrAdmin", policy => policy.RequireRole("Trainer", "Admin"));
    options.AddPolicy("ClientOrHigher", policy => policy.RequireRole("Client", "Trainer", "Admin"));
});

builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Seed ролей и админа, инициализация БД
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    // Инициализация доменных данных
    var context = services.GetRequiredService<FitRazorContext>();
    await SeedData.InitializeAsync(context);

    // Инициализация ролей и тестовых пользователей
    await RoleSeed.SeedAsync(services);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection(); 
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();   // ← обязательно перед UseAuthorization
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
