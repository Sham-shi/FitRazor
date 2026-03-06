using FitRazor.Data.Models;
using FitRazor.Web.Helpers;
using FitRazor.Web.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FitRazor.Web.Pages.Entities
{
    //[Authorize(Roles = "Trainer,Admin")]
    [BindProperties]
    public class CreateModel : PageModel
    {
        private readonly FitRazorContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(FitRazorContext context, IWebHostEnvironment env, ILogger<CreateModel> logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)] public string EntityName { get; set; } = "Trainers";

        // Для фото (Trainer.PhotoUrl)
        [BindProperty] public IFormFile? PhotoUrl { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("Запрос на создание {EntityName}", EntityName);

            var meta = EntityAdminRegistry.Get(EntityName);
            if (meta == null)
            {
                _logger.LogWarning("Попытка создания неизвестной сущности: {EntityName}", EntityName);
                TempData["ErrorMessage"] = "Неизвестная сущность";
                return RedirectToPage("Create", new { entityName = EntityName });
            }

            try
            {
                // Создаём новый экземпляр
                var entity = Activator.CreateInstance(meta.EntityType);
                if (entity == null)
                {
                    _logger.LogError("Не удалось создать экземпляр типа {EntityType}", meta.EntityType.Name);
                    throw new InvalidOperationException("Не удалось создать экземпляр сущности");
                }

                // Применяем значения из формы
                Helper.ApplyFormValuesToEntity(entity, Request.Form);
                _logger.LogDebug("Применены данные формы к сущности {EntityName}", EntityName);

                // Специальная обработка фото (для Trainers)
                if (EntityName == "Trainers" && entity is Trainer trainer && PhotoUrl != null && PhotoUrl.Length > 0)
                {
                    try
                    {
                        var newPhotoPath = await Helper.SaveImageAsync(
                            file: PhotoUrl,
                            env: _env,
                            subfolder: "Trainers"
                        // oldPath не передаём → null
                        );

                        if (newPhotoPath != null)
                        {
                            trainer.PhotoUrl = newPhotoPath;
                            _logger.LogInformation("Сохранено фото для тренера: {PhotoPath}", newPhotoPath);
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        _logger.LogWarning(ex, "Неверный формат фото для сущности {EntityName}", EntityName);
                        TempData["ErrorMessage"] = ex.Message;
                        return Page();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка при сохранении фото для {EntityName}", EntityName);
                        throw; // пробрасываем дальше для обработки в общем catch
                    }
                }

                // Специальная логика для отдельных сущностей
                switch (EntityName) // ← этот switch можно тоже вынести в реестр позже
                {
                    case "Clients" when entity is Client client:
                        client.RegistrationDate = DateOnly.FromDateTime(DateTime.Today);
                        _logger.LogDebug("Установлена дата регистрации для клиента: {Date}", client.RegistrationDate);
                        break;

                    case "Bookings" when entity is Booking booking:
                        booking.TotalPrice = booking.UnitPrice * booking.SessionsCount;
                        booking.CreatedDate = DateTime.Now;
                        _logger.LogDebug("Рассчитана стоимость бронирования: {TotalPrice} руб.", booking.TotalPrice);
                        break;
                }

                // Добавляем в контекст
                await _context.AddAsync(entity);
                var saveResult = await _context.SaveChangesAsync();

                if (saveResult > 0)
                {
                    // Получаем ID созданной сущности (если есть)
                    var entityId = entity.GetType().GetProperty("Id")?.GetValue(entity);
                    _logger.LogInformation("Успешно создана сущность {EntityName}, ID: {EntityId}, записей сохранено: {SaveCount}",
                        EntityName, entityId ?? "N/A", saveResult);

                    TempData["SuccessMessage"] = "Запись успешно создана!";
                    return RedirectToPage("Index", new { entityName = EntityName });
                }
                else
                {
                    _logger.LogWarning("SaveChangesAsync вернул 0 при создании {EntityName}", EntityName);
                    TempData["ErrorMessage"] = "Не удалось сохранить запись";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при создании сущности {EntityName}. Форма: {@FormValues}",
                    EntityName,
                    // Безопасно: не логируем пароли/токены, только мета-данные
                    new
                    {
                        FormKeys = Request.Form.Keys,
                        FilesCount = Request.Form.Files.Count,
                        UserAgent = Request.Headers.UserAgent.ToString()
                    });

                TempData["ErrorMessage"] = $"Ошибка при создании: {ex.Message}";
                return RedirectToPage("Create", new { entityName = EntityName });
            }
        }
    }
}