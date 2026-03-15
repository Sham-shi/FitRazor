using FitRazor.Data.Models;
using FitRazor.Web.Helpers;
using FitRazor.Web.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FitRazor.Web.Pages.Entities;

[Authorize(Roles = "Admin")]
[BindProperties]
public class EditModel : PageModel
{
    private readonly FitRazorContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<EditModel> _logger;

    public EditModel(FitRazorContext context, IWebHostEnvironment env, ILogger<EditModel> logger)
    {
        _context = context;
        _env = env;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)] public string EntityName { get; set; } = "Trainers";
    [BindProperty(SupportsGet = true)] public int Id { get; set; }
    public bool EntityNotFound { get; set; }

    // 🔹 Универсальные свойства для фото (работают для любой сущности)
    [BindProperty] public IFormFile? UploadedFile { get; set; }
    [BindProperty] public string? OldFileUrl { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var meta = EntityAdminRegistry.Get(EntityName);
        if (meta == null || !await meta.ExistsAsync(_context, Id))
        {
            EntityNotFound = true;
            return Page();
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        _logger.LogInformation("POST: обновление {EntityName}#{Id}", EntityName, Id);

        var meta = EntityAdminRegistry.Get(EntityName);
        if (meta == null)
        {
            _logger.LogError("Сущность {EntityName} не найдена", EntityName);
            TempData["ErrorMessage"] = "Неизвестная сущность";
            return RedirectToPage("Index", new { entityName = EntityName });
        }

        var entity = await meta.GetByIdAsync(_context, Id);
        if (entity == null)
        {
            _logger.LogError("Запись {EntityName}#{Id} не найдена", EntityName, Id);
            TempData["ErrorMessage"] = "Запись не найдена";
            return RedirectToPage("Index", new { entityName = EntityName });
        }

        try
        {
            // 1. Применяем данные формы
            Helper.ApplyFormValuesToEntity(entity, Request.Form);

            // 2. Обрабатываем загрузку файла (если есть конфиг в метаданных)
            await HandleFileUploadAsync(entity, meta);

            // 3. Выполняем хук BeforeSave (пересчёт TotalPrice и т.п.)
            if (meta.BeforeSaveAsync != null)
            {
                await meta.BeforeSaveAsync(_context, entity);
            }

            // 4. Сохраняем
            await _context.SaveChangesAsync();

            // 5. Хук AfterSave (опционально)
            if (meta.AfterSaveAsync != null)
            {
                await meta.AfterSaveAsync(_context, entity);
            }

            _logger.LogInformation("Запись {EntityName}#{Id} успешно обновлена!", EntityName, Id);
            TempData["SuccessMessage"] = "Запись успешно обновлена!";
            return RedirectToPage("Index", new { entityName = EntityName });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Ошибка валидации данных для {EntityName}#{Id}", EntityName, Id);
            TempData["ErrorMessage"] = ex.Message;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Критическая ошибка при обновлении {EntityName}#{Id}", EntityName, Id);
            TempData["ErrorMessage"] = $"Ошибка: {ex.Message}";
            return Page();
        }
    }

    /// <summary>
    /// Универсальная обработка загрузки файлов на основе конфигурации в метаданных
    /// </summary>
    private async Task HandleFileUploadAsync(object entity, EntityAdminMetadata meta)
    {
        if (UploadedFile == null || UploadedFile.Length == 0)
            return;

        // Ищем свойство, для которого загружается файл
        var photoProp = entity.GetType().GetProperties()
            .FirstOrDefault(p => meta.PhotoUploadConfigs.ContainsKey(p.Name) ||
                                 p.Name.EndsWith("PhotoUrl", StringComparison.OrdinalIgnoreCase) ||
                                 p.Name.EndsWith("ImageUrl", StringComparison.OrdinalIgnoreCase) ||
                                 p.Name.EndsWith("AvatarUrl", StringComparison.OrdinalIgnoreCase));

        if (photoProp == null || !photoProp.CanWrite)
            return;

        // Получаем конфиг (или создаём дефолтный)
        var config = meta.PhotoUploadConfigs.TryGetValue(photoProp.Name, out var cfg)
            ? cfg
            : new PhotoUploadConfig { Subfolder = "Uploads" };

        // Сохраняем файл
        var newPath = await Helper.SaveImageAsync(
            file: UploadedFile,
            env: _env,
            subfolder: config.Subfolder,
            oldPath: OldFileUrl,
            maxSizeBytes: config.MaxSizeBytes,
            allowedExtensions: config.AllowedExtensions
        );

        if (newPath != null)
        {
            photoProp.SetValue(entity, newPath);
            _logger.LogInformation("Файл обновлён для {EntityName}#{Id}: {PropertyName} = {NewPath}",
                EntityName, Id, photoProp.Name, newPath);
        }
    }
}