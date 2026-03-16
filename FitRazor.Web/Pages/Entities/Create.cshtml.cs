// FitRazor.Web/Pages/Entities/Create.cshtml.cs
using FitRazor.Data.Models;
using FitRazor.Web.Helpers;
using FitRazor.Web.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FitRazor.Web.Pages.Entities;

[Authorize]
[BindProperties]
public class CreateModel : PageModel
{
    private readonly FitRazorContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<CreateModel> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateModel(
        FitRazorContext context,
        IWebHostEnvironment env,
        ILogger<CreateModel> logger,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _env = env;
        _logger = logger;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)] public string EntityName { get; set; } = "Trainers";
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    // 🔹 Универсальные свойства для фото (как в EditModel)
    [BindProperty] public IFormFile? UploadedFile { get; set; }

    public IActionResult OnGet()
    {
        if (string.IsNullOrEmpty(ReturnUrl) && Request.Headers.Referer.Any())
        {
            var referer = Request.Headers.Referer.ToString();

            if (Url.IsLocalUrl(referer))
                ReturnUrl = referer;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        var meta = EntityAdminRegistry.Get(EntityName);

        // 🔹 Проверка авторизации
        if (!await CanCreateAsync(user, EntityName))
        {
            _logger.LogWarning("Пользователь {UserId} не имеет прав на создание {EntityName}",
                user?.Id, EntityName);
            TempData["ErrorMessage"] = "Нет прав на создание";
            return Forbid();
        }

        if (meta == null)
        {
            _logger.LogWarning("Попытка создания неизвестной сущности: {EntityName}", EntityName);
            TempData["ErrorMessage"] = "Неизвестная сущность";
            return RedirectToPage("Index", new { entityName = EntityName });
        }

        try
        {
            // 1. Создаём экземпляр
            var entity = Activator.CreateInstance(meta.EntityType);
            if (entity == null)
                throw new InvalidOperationException($"Не удалось создать {meta.EntityType.Name}");

            // 2. Применяем значения по умолчанию из метаданных
            if (meta.DefaultValues != null)
            {
                foreach (var (propName, valueFactory) in meta.DefaultValues)
                {
                    var prop = entity.GetType().GetProperty(propName);
                    if (prop?.CanWrite == true)
                        prop.SetValue(entity, valueFactory());
                }
            }

            // 3. Применяем данные формы
            Helper.ApplyFormValuesToEntity(entity, Request.Form);

            // 4. Обрабатываем загрузку файла (если есть конфиг)
            await HandleFileUploadAsync(entity, meta);

            // 5. Выполняем хук BeforeSave (пересчёт TotalPrice и т.п.)
            if (meta.BeforeSaveAsync != null)
                await meta.BeforeSaveAsync(_context, entity);

            // 6. Сохраняем
            await _context.AddAsync(entity);
            await _context.SaveChangesAsync();

            // 7. Хук AfterCreate (опционально)
            if (meta.AfterCreateAsync != null)
                await meta.AfterCreateAsync(_context, entity);

            _logger.LogInformation("Успешно создана {EntityName}", EntityName);
            TempData["SuccessMessage"] = "Запись успешно создана!";

            // 🔹 Возвращаем страницу с инструкцией для браузера вернуться назад
            // 🔹 Если ReturnUrl не передан — берём из заголовка Referer
            if (string.IsNullOrEmpty(ReturnUrl) && Request.Headers.Referer.Any())
            {
                var referer = Request.Headers.Referer.ToString();
                // Проверяем, что referer локальный (защита от open redirect)
                if (Url.IsLocalUrl(referer))
                    ReturnUrl = referer;
            }

            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }

            // fallback
            return RedirectToPage("Index", new { entityName = EntityName });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Ошибка валидации при создании {EntityName}", EntityName);
            TempData["ErrorMessage"] = ex.Message;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Критическая ошибка при создании {EntityName}", EntityName);
            TempData["ErrorMessage"] = $"Ошибка: {ex.Message}";
            return Page();
        }
    }

    /// <summary>
    /// Проверка прав на создание сущности
    /// </summary>
    private async Task<bool> CanCreateAsync(ApplicationUser? user, string entityName)
    {
        if (user == null) return false;

        // Админ может создавать всё
        if (await _userManager.IsInRoleAsync(user, "Admin"))
            return true;

        // Клиент может создавать только свои записи (Bookings)
        if (await _userManager.IsInRoleAsync(user, "Client"))
            return entityName == "Bookings";

        // Тренер не создаёт сущности через общий интерфейс
        if (await _userManager.IsInRoleAsync(user, "Trainer"))
            return false;

        return false;
    }

    /// <summary>
    /// Универсальная обработка загрузки файлов
    /// </summary>
    private async Task HandleFileUploadAsync(object entity, EntityAdminMetadata meta)
    {
        if (UploadedFile == null || UploadedFile.Length == 0)
            return;

        // Ищем свойство для загрузки фото
        var photoProp = entity.GetType().GetProperties()
            .FirstOrDefault(p => meta.PhotoUploadConfigs.ContainsKey(p.Name) ||
                                 p.Name.EndsWith("PhotoUrl", StringComparison.OrdinalIgnoreCase) ||
                                 p.Name.EndsWith("ImageUrl", StringComparison.OrdinalIgnoreCase) ||
                                 p.Name.EndsWith("AvatarUrl", StringComparison.OrdinalIgnoreCase));

        if (photoProp == null || !photoProp.CanWrite)
            return;

        var config = meta.PhotoUploadConfigs.TryGetValue(photoProp.Name, out var cfg)
            ? cfg
            : new PhotoUploadConfig { Subfolder = "Uploads" };

        var newPath = await Helper.SaveImageAsync(
            file: UploadedFile,
            env: _env,
            subfolder: config.Subfolder,
            maxSizeBytes: config.MaxSizeBytes,
            allowedExtensions: config.AllowedExtensions
        );

        if (newPath != null)
        {
            photoProp.SetValue(entity, newPath);
            _logger.LogInformation("Файл сохранён для {EntityName}: {PropertyName} = {NewPath}",
                EntityName, photoProp.Name, newPath);
        }
    }
}