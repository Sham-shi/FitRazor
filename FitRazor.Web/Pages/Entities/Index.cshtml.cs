using FitRazor.Data.Models;
using FitRazor.Web.Services.Admin;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FitRazor.Web.Pages.Entities;

//[Authorize(Roles = "Trainer,Admin")]
public class IndexModel : PageModel
{
    private readonly FitRazorContext _context;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(FitRazorContext context, ILogger<IndexModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string EntityName { get; set; } = "Trainers";

    public void OnGet(string entityName)
    {
        EntityName = entityName ?? "Trainers";
        _logger.LogDebug("Запрос списка сущности: {EntityName}", EntityName);

        // Валидация имени сущности
        if (EntityAdminRegistry.Get(EntityName) == null)
        {
            _logger.LogWarning("Получено неизвестное имя сущности: {EntityName}, используем значение по умолчанию", EntityName);
            EntityName = "Trainers"; // или ошибка
        }
        else
        {
            _logger.LogDebug("Сущность {EntityName} валидна, продолжаем загрузку", EntityName);
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(string entityName, int id)
    {
        _logger.LogInformation("Запрос на удаление {EntityName}#{Id}", entityName, id);

        var meta = EntityAdminRegistry.Get(entityName);
        if (meta == null)
        {
            _logger.LogWarning("Попытка удаления неизвестной сущности: {EntityName}", entityName);
            TempData["ErrorMessage"] = "Неизвестная сущность";
            return RedirectToPage("Index", new { entityName });
        }

        try
        {
            // 🔹 Выполняем бизнес-проверки из метаданных
            if (meta.PreDeleteChecksAsync != null)
            {
                var (canDelete, errorMsg) = await meta.PreDeleteChecksAsync(_context, id);
                if (!canDelete)
                {
                    _logger.LogWarning("Не удалось удалить {EntityName}#{Id}: {Error}", entityName, id, errorMsg);
                    TempData["ErrorMessage"] = errorMsg ?? "Запись не найдена или уже удалена";
                    return RedirectToPage("Index", new { entityName });
                }
            }

            // 🔹 Особая обработка для сущностей с ApplicationUser
            if (meta.HasUserProfile && meta.GetApplicationUserId != null)
            {
                return await DeleteWithUserProfileAsync(meta, id, entityName);
            }

            _logger.LogDebug("Выполняем удаление через мета-сервис для {EntityName}#{Id}", entityName, id);
            var (success, error) = await meta.DeleteAsync(_context, id);

            if (success)
            {
                _logger.LogInformation("Успешно удалена запись {EntityName}#{Id}", entityName, id);
                TempData["SuccessMessage"] = "Запись успешно удалена!";
            }
            else
            {
                _logger.LogWarning("Не удалось удалить {EntityName}#{Id}: {Error}", entityName, id, error ?? "Запись не найдена");
                TempData["ErrorMessage"] = error ?? "Запись не найдена или уже удалена";
            }
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("FOREIGN KEY constraint") == true)
        {
            // Специфичная обработка ошибок внешних ключей
            _logger.LogWarning(ex, "Не удалось удалить {EntityName}#{Id}: запись используется в других таблицах (нарушение внешнего ключа)", entityName, id);
            TempData["ErrorMessage"] = "Невозможно удалить: запись используется в других данных";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Критическая ошибка при удалении {EntityName}#{Id}", entityName, id);
            TempData["ErrorMessage"] = $"Ошибка при удалении: {ex.Message}";
        }

        return RedirectToPage("Index", new { entityName });
    }

    /// <summary>
    /// Удаление сущности + связанного ApplicationUser в транзакции
    /// </summary>
    private async Task<IActionResult> DeleteWithUserProfileAsync(EntityAdminMetadata meta, int id, string entityName)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();

        try
        {
            // 1. Получаем сущность для извлечения ApplicationUserId
            var entity = await meta.GetByIdAsync(_context, id);
            if (entity == null)
            {
                await transaction.RollbackAsync();
                return NotFound();
            }

            // 2. Извлекаем и удаляем ApplicationUser
            var appUserId = meta.GetApplicationUserId!(entity);
            if (!string.IsNullOrEmpty(appUserId))
            {
                var user = await userManager.FindByIdAsync(appUserId);
                if (user != null)
                {
                    var result = await userManager.DeleteAsync(user);
                    if (!result.Succeeded)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(string.Join("; ", result.Errors.Select(e => e.Description)));
                    }
                }
            }

            // 3. Удаляем саму сущность (каскадное удаление связанных записей через EF)
            var (success, error) = await meta.DeleteAsync(_context, id);
            if (!success)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = error ?? "Не удалось удалить запись";
                return RedirectToPage("Index", new { entityName });
            }

            await transaction.CommitAsync();
            _logger.LogInformation("Успешно удалена запись и аккаунт {EntityName}#{Id}", entityName, id);
            TempData["SuccessMessage"] = "Запись и аккаунт пользователя успешно удалены!";
            return RedirectToPage("Index", new { entityName });
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("FOREIGN KEY") == true)
        {
            await transaction.RollbackAsync();
            _logger.LogWarning(ex, "Нарушение внешнего ключа при удалении {EntityName}#{Id}", entityName, id);
            TempData["ErrorMessage"] = "Невозможно удалить: запись используется в других данных";
            return RedirectToPage("Index", new { entityName });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
