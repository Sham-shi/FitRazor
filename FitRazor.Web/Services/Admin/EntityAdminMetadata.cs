using FitRazor.Data;
using FitRazor.Data.Models;
using FitRazor.Web.Helpers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FitRazor.Web.Services.Admin;

/// <summary>
/// Конфигурация для загрузки изображений в формах
/// </summary>
public class PhotoUploadConfig
{
    public string Subfolder { get; init; } = "Uploads";
    public string DefaultImagePath { get; init; } = "/Images/no-photo.jpg";
    public string[] AllowedExtensions { get; init; } = { ".jpg", ".jpeg", ".png" };
    public long MaxSizeBytes { get; init; } = 5 * 1024 * 1024; // 5 MB
    public string? PreviewLabel { get; init; } = "Текущее изображение";
    public string? UploadLabel { get; init; } = "Загрузить новое";
}

public class EntityAdminMetadata
{
    public string Name { get; init; } = null!;
    public string PluralDisplayName { get; init; } = null!;
    public Type EntityType { get; init; } = null!;
    public string KeyPropertyName { get; init; } = "Id";

    // Как загрузить список записей
    public Func<FitRazorContext, IQueryable<object>> QueryFactory { get; init; } = null!;

    // Получить одну запись по ID (с Include где нужно)
    public Func<FitRazorContext, int, Task<object?>> GetByIdAsync { get; init; } = null!;

    // Существует ли запись с таким ID
    public Func<FitRazorContext, int, Task<bool>> ExistsAsync { get; init; } = null!;

    // Удаление + бизнес-проверки
    public Func<FitRazorContext, int, Task<(bool Success, string? Error)>> DeleteAsync { get; init; } = null!;

    // Какое свойство/выражение использовать для красивого имени в подтверждении удаления
    public Func<FitRazorContext, int, Task<string>> GetDisplayNameForDeleteAsync { get; init; } = null!;

    // Провайдеры данных для выпадающих списков (FK)
    public Dictionary<string, Func<FitRazorContext, Task<IEnumerable<SelectListItem>>>> DropdownProviders { get; init; } = new();

    /// <summary>
    /// Флаг: сущность имеет связь с ApplicationUser и требует удаления аккаунта
    /// </summary>
    public bool HasUserProfile { get; init; } = false;

    /// <summary>
    /// Функция для извлечения ApplicationUserId из сущности
    /// </summary>
    public Func<object, string?>? GetApplicationUserId { get; init; }

    /// <summary>
    /// Бизнес-проверки перед удалением (например, есть ли связанные записи)
    /// Возвращает (можноУдалить, сообщениеОбОшибке)
    /// </summary>
    public Func<FitRazorContext, int, Task<(bool CanDelete, string? ErrorMessage)>>? PreDeleteChecksAsync { get; init; }

    /// <summary>
    /// Функция для получения свойств, отображаемых в таблице списка
    /// (по умолчанию используется рефлексия с фильтрацией)
    /// </summary>
    public Func<Type, IEnumerable<PropertyInfo>>? GetDisplayPropertiesFunc { get; init; }

    /// <summary>
    /// Синхронная функция для получения отображаемого имени сущности 
    /// (для использования в модальных окнах, подсказках и т.д.)
    /// </summary>
    public Func<object, string>? GetDisplayNameFunc { get; init; }

    /// <summary>
    /// Настройки форматирования для конкретных свойств
    /// Ключ: имя свойства, Значение: функция форматирования
    /// </summary>
    public Dictionary<string, Func<object, Type, string>>? PropertyFormatters { get; init; } = new();

    /// <summary>
    /// Свойства, которые должны отображаться как изображения
    /// </summary>
    public HashSet<string> ImageProperties { get; init; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "PhotoUrl", "ImageUrl", "AvatarUrl"
    };

    /// <summary>
    /// Путь к изображению-заглушке по умолчанию
    /// </summary>
    public string? DefaultImagePath { get; init; }

    // ────────────────────────────────────────────────
    // 🔹 Конфигурация для форм редактирования
    // ────────────────────────────────────────────────

    /// <summary>
    /// Функция для получения свойств, отображаемых в форме редактирования
    /// (по умолчанию использует Helper.GetFormProperties)
    /// </summary>
    public Func<Type, IEnumerable<PropertyInfo>>? GetEditPropertiesFunc { get; init; }

    /// <summary>
    /// Свойства, которые должны быть доступны только для чтения в форме
    /// (отображаются, но не редактируются)
    /// </summary>
    public HashSet<string> ReadOnlyProperties { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Свойства, которые нужно полностью скрыть из формы редактирования
    /// </summary>
    public HashSet<string> HiddenProperties { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Генераторы кастомных HTML-инпутов для конкретных свойств
    /// Ключ: имя свойства, Значение: функция генерации HTML
    /// </summary>
    public Dictionary<string, Func<PropertyInfo, object?, string, Dictionary<string, IEnumerable<SelectListItem>>, string>>?
        CustomInputGenerators
    { get; init; } = new();

    /// <summary>
    /// Конфигурация для загрузки изображений
    /// Ключ: имя свойства (PhotoUrl, ImageUrl...), Значение: настройки
    /// </summary>
    public Dictionary<string, PhotoUploadConfig> PhotoUploadConfigs { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Хук для выполнения логики перед сохранением (например, пересчёт TotalPrice)
    /// </summary>
    public Func<FitRazorContext, object, Task>? BeforeSaveAsync { get; init; }

    /// <summary>
    /// Хук для выполнения логики после успешного сохранения
    /// </summary>
    public Func<FitRazorContext, object, Task>? AfterSaveAsync { get; init; }

    /// <summary>
    /// Свойства, которые нужно скрыть в форме создания
    /// </summary>
    public HashSet<string> HiddenInCreate { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Значения по умолчанию для свойств при создании
    /// </summary>
    public Dictionary<string, Func<object?>>? DefaultValues { get; init; } = new();

    /// <summary>
    /// Хук после успешного создания
    /// </summary>
    public Func<FitRazorContext, object, Task>? AfterCreateAsync { get; init; }
}

public static class EntityAdminRegistry
{
    private static readonly Dictionary<string, EntityAdminMetadata> _registry;

    static EntityAdminRegistry()
    {
        _registry = new Dictionary<string, EntityAdminMetadata>(StringComparer.OrdinalIgnoreCase);

        // ────────────────────────────────────────────────
        // Trainers
        // ────────────────────────────────────────────────
        _registry["Trainers"] = new EntityAdminMetadata
        {
            Name = "Trainers",
            PluralDisplayName = "Тренеры",
            EntityType = typeof(Trainer),
            KeyPropertyName = "TrainerId",

            // Синхронное получение имени для отображения
            GetDisplayNameFunc = entity =>
                (entity as Trainer)?.FullName ?? $"Тренер #{(entity as Trainer)?.TrainerId}",

            // Настройка отображаемых свойств (опционально, если нужно скрыть/переупорядочить)
            GetDisplayPropertiesFunc = type => Helper.GetFormProperties(type),

            // Форматтеры для специфичных свойств
            PropertyFormatters = new()
            {
                ["Salary"] = (value, type) =>
                    $"<span class='text-success fw-bold'>{((decimal)value):N2} ₽</span>",
                ["WorkExperience"] = (value, type) =>
                    $"<span class='text-primary'>{value}</span>",
            },

            // Настройка изображений
            ImageProperties = { "PhotoUrl" },
            DefaultImagePath = "/Images/Trainers/no-photo.jpg",

            // Конфигурация формы редактирования
            GetEditPropertiesFunc = type => Helper.GetFormProperties(type),

            // Скрыть служебные поля
            HiddenProperties = { "ApplicationUserId", "ApplicationUser", "TrainerServices" },

            // Поля только для чтения
            ReadOnlyProperties = { }, // все поля редактируемые

            // Кастомные инпуты
            CustomInputGenerators = new()
            {
                // Специальный инпут для фото
                ["PhotoUrl"] = (prop, value, fieldName, dropdowns) =>
                    GeneratePhotoInput(prop, value, fieldName, new PhotoUploadConfig
                    {
                        Subfolder = "Trainers",
                        DefaultImagePath = "/Images/Trainers/no-photo.jpg",
                        PreviewLabel = "Текущее фото тренера",
                        UploadLabel = "Загрузить новое фото (jpg, png, до 5 МБ)"
                    }),

                // Multiline для описаний
                ["SpecializationDescription"] = (prop, value, fieldName, dropdowns) =>
                    $"<textarea name='{prop.Name}' class='form-control' rows='3' maxlength='1000'>{value?.ToString()}</textarea>",
                ["WorkExperience"] = (prop, value, fieldName, dropdowns) =>
                    $"<textarea name='{prop.Name}' class='form-control' rows='4' maxlength='1000'>{value?.ToString()}</textarea>",
                ["SportsAchievements"] = (prop, value, fieldName, dropdowns) =>
                    $"<textarea name='{prop.Name}' class='form-control' rows='3' maxlength='1000'>{value?.ToString()}</textarea>",
                ["Email"] = (prop, value, fieldName, dropdowns) =>
                    $"<input type='email' name='{prop.Name}' class='form-control' value='{value?.ToString()}' maxlength='100' />",
                ["Phone"] = (prop, value, fieldName, dropdowns) =>
                    $"<input type='text' name='{prop.Name}' class='form-control phone-mask' value='{value?.ToString()}' maxlength='20' autocomplete='off' />",
            },

            // Конфигурация загрузки фото (дублируется для удобства в тег-хелпере)
            PhotoUploadConfigs =
            {
                ["PhotoUrl"] = new PhotoUploadConfig
                {
                    Subfolder = "Trainers",
                    DefaultImagePath = "/Images/Trainers/no-photo.jpg",
                    PreviewLabel = "Текущее фото тренера",
                    UploadLabel = "Загрузить новое фото (jpg,jpeg, png)"
                }
            },

            // Логика перед сохранением
            BeforeSaveAsync = async (ctx, entity) =>
            {
                // Здесь можно добавить валидацию или преобразования
                await Task.CompletedTask;
            },

            HasUserProfile = true,

            // Извлечение ApplicationUserId из сущности
            GetApplicationUserId = entity =>
                (entity as Trainer)?.ApplicationUserId,

            // Бизнес-проверки перед удалением
            PreDeleteChecksAsync = async (ctx, id) =>
            {
                if (await ctx.TrainerServices.AnyAsync(ts => ts.TrainerId == id))
                    return (false, "Нельзя удалить тренера — есть назначенные услуги");
                return (true, null);
            },

            QueryFactory = ctx => ctx.Trainers.AsQueryable<object>(),
            GetByIdAsync = async (ctx, id) => await ctx.Trainers.FindAsync(id),
            ExistsAsync = (ctx, id) => ctx.Trainers.AnyAsync(t => t.TrainerId == id),

            // Базовое удаление сущности (без Identity)
            DeleteAsync = async (ctx, id) =>
            {
                var trainer = await ctx.Trainers.FindAsync(id);
                if (trainer == null) return (false, "Тренер не найден");

                ctx.Trainers.Remove(trainer);
                await ctx.SaveChangesAsync();
                return (true, null);
            },

            GetDisplayNameForDeleteAsync = async (ctx, id) =>
            {
                var t = await ctx.Trainers.FindAsync(id);
                return t?.FullName ?? $"Тренер #{id}";
            },

            DropdownProviders = { }
        };

        // ────────────────────────────────────────────────
        // Clients
        // ────────────────────────────────────────────────
        _registry["Clients"] = new EntityAdminMetadata
        {
            Name = "Clients",
            PluralDisplayName = "Клиенты",
            EntityType = typeof(Client),
            KeyPropertyName = "ClientId",

            GetDisplayNameFunc = entity =>
                (entity as Client)?.FullName ?? $"Клиент #{(entity as Client)?.ClientId}",

            GetDisplayPropertiesFunc = type => Helper.GetFormProperties(type),

            PropertyFormatters = new()
            {
                ["BirthDate"] = (value, type) =>
                    value is DateOnly d ? $"<span>{d:dd.MM.yyyy}</span>" : "—",
            },

            ImageProperties = { "PhotoUrl" }, // если у клиентов тоже есть фото
            DefaultImagePath = "/Images/Clients/no-photo.jpg",

            HiddenProperties = { "ApplicationUserId", "ApplicationUser", "Bookings" },
            ReadOnlyProperties = { "RegistrationDate" }, // дата регистрации не редактируется

            CustomInputGenerators = new()
            {
                ["Email"] = (prop, value, fieldName, dropdowns) =>
                    $"<input type='email' name='{prop.Name}' class='form-control' value='{value?.ToString()}' maxlength='100' />",
                ["Phone"] = (prop, value, fieldName, dropdowns) =>
                    $"<input type='text' name='{prop.Name}' class='form-control phone-mask' value='{value?.ToString()}' maxlength='20' autocomplete='off' />",
            },

            PhotoUploadConfigs =
            {
                ["PhotoUrl"] = new PhotoUploadConfig
                {
                    Subfolder = "Clients",
                    DefaultImagePath = "/Images/Clients/no-photo.jpg"
                }
            },


            HasUserProfile = true,

            GetApplicationUserId = entity =>
                (entity as Client)?.ApplicationUserId,

            PreDeleteChecksAsync = async (ctx, id) =>
            {
                if (await ctx.Bookings.AnyAsync(b => b.ClientId == id))
                    return (false, "Нельзя удалить клиента — есть активные бронирования");
                return (true, null);
            },

            QueryFactory = ctx => ctx.Clients.AsQueryable<object>(),
            GetByIdAsync = async (ctx, id) => await ctx.Clients.FindAsync(id),
            ExistsAsync = (ctx, id) => ctx.Clients.AnyAsync(c => c.ClientId == id),

            DeleteAsync = async (ctx, id) =>
            {
                var client = await ctx.Clients.FindAsync(id);
                if (client == null) return (false, "Клиент не найден");

                ctx.Clients.Remove(client);
                await ctx.SaveChangesAsync();
                return (true, null);
            },

            GetDisplayNameForDeleteAsync = async (ctx, id) =>
            {
                var c = await ctx.Clients.FindAsync(id);
                return c?.FullName ?? $"Клиент #{id}";
            },

            DropdownProviders = { },

            HiddenInCreate = { "ApplicationUserId", "ApplicationUser", "ClientId", "Bookings" },

            DefaultValues = new()
            {
                ["RegistrationDate"] = () => DateOnly.FromDateTime(DateTime.Today),
            },
        };

        // ────────────────────────────────────────────────
        // Services
        // ────────────────────────────────────────────────
        _registry["Services"] = new EntityAdminMetadata
        {
            Name = "Services",
            PluralDisplayName = "Услуги",
            EntityType = typeof(Service),
            KeyPropertyName = "ServiceId",

            GetDisplayNameFunc = entity =>
                (entity as Service)?.ServiceName ?? $"Услуга #{(entity as Service)?.ServiceId}",

            PropertyFormatters = new()
            {
                ["BasePrice"] = (value, type) =>
                    $"<span class='text-success fw-bold'>{((decimal)value):N2} ₽</span>",
                ["DurationMinutes"] = (value, type) =>
                    $"<span>{value} мин</span>",
            },

            HiddenProperties = { "TrainerServices" },
            ReadOnlyProperties = { },

            CustomInputGenerators = new()
            {
                ["Description"] = (prop, value, fieldName, dropdowns) =>
                    $"<textarea name='{prop.Name}' class='form-control' rows='4' maxlength='500'>{value?.ToString()}</textarea>",
            },

            QueryFactory = ctx => ctx.Services.AsQueryable<object>(),
            GetByIdAsync = async (ctx, id) => await ctx.Services.FindAsync(id),
            ExistsAsync = (ctx, id) => ctx.Services.AnyAsync(s => s.ServiceId == id),

            DeleteAsync = async (ctx, id) =>
            {
                var svc = await ctx.Services.FindAsync(id);
                if (svc == null) return (false, "Услуга не найдена");

                if (await ctx.TrainerServices.AnyAsync(ts => ts.ServiceId == id))
                    return (false, "Нельзя удалить услугу — она назначена тренерам");

                ctx.Services.Remove(svc);
                await ctx.SaveChangesAsync();
                return (true, null);
            },

            GetDisplayNameForDeleteAsync = async (ctx, id) =>
            {
                var s = await ctx.Services.FindAsync(id);
                return s?.ServiceName ?? $"Услуга #{id}";
            },

            DropdownProviders = { }
        };

        // ────────────────────────────────────────────────
        // Bookings
        // ────────────────────────────────────────────────
        _registry["Bookings"] = new EntityAdminMetadata
        {
            Name = "Bookings",
            PluralDisplayName = "Записи",
            EntityType = typeof(Booking),
            KeyPropertyName = "BookingId",

            // ✅ Явно указываем свойства для отображения
            GetDisplayPropertiesFunc = type =>
                Helper.GetFormProperties(type)
                    .Where(p => p.Name != "CreatedDate")
                    .Concat(type.GetProperty("TotalPrice") != null ? new[] { type.GetProperty("TotalPrice")! } : Array.Empty<PropertyInfo>()),

            GetDisplayNameFunc = entity =>
            {
                if (entity is Booking b)
                {
                    var client = b.Client?.FullName ?? "Клиент";
                    var service = b.TrainerService?.Service?.ServiceName ?? "Услуга";
                    return $"{client} — {service} ({b.BookingDateTime:dd.MM.yyyy HH:mm})";
                }
                return $"Запись #{(entity as Booking)?.BookingId}";
            },

            PropertyFormatters = new()
            {
                ["BookingDateTime"] = (value, type) =>
                    value is DateTime dt ? $"<span>{dt:dd.MM.yyyy HH:mm}</span>" : "—",
                ["UnitPrice"] = (value, type) =>
                    $"<span class='text-success fw-bold'>{((decimal)value):N2} ₽</span>",
                ["TotalPrice"] = (value, type) =>
                    value is decimal total ? $"<span class='fw-bold'>{total:N2} ₽</span>" : "—",
                ["Status"] = (value, type) =>
                {
                    var status = value?.ToString();
                    var badgeClass = status switch
                    {
                        "Запланировано" => "bg-primary",
                        "Перенесено" => "bg-warning text-dark",
                        "Завершено" => "bg-success",
                        "Отменено" => "bg-danger",
                        _ => "bg-secondary"
                    };
                    return $"<span class='badge {badgeClass}'>{status}</span>";
                },
            },

            // Для Bookings важно загружать навигационные свойства
            QueryFactory = ctx =>
            {
                var query = ctx.Bookings
                    .Include(b => b.Client)
                    .Include(b => b.TrainerService)
                    .ThenInclude(ts => ts!.Service)
                    .Include(b => b.TrainerService)
                    .ThenInclude(ts => ts!.Trainer);

                return query.Cast<object>();
            },

            HiddenProperties = { "CreatedDate" }, // дата создания не редактируется
            ReadOnlyProperties = { "TotalPrice", "UnitPrice" }, // рассчитываются автоматически

            CustomInputGenerators = new()
            {
                // 🔹 Status — выпадающий список с валидными значениями
                ["Status"] = (prop, value, fieldName, dropdowns) =>
                {
                    var validStatuses = new[] { "Запланировано", "Перенесено", "Завершено", "Отменено" };
                    var currentValue = value?.ToString() ?? "";
                    var sb = new System.Text.StringBuilder();
                    sb.Append($"<select name='{prop.Name}' class='form-select'>");
                    sb.Append("<option value=''>— Выберите статус —</option>");
                    foreach (var status in validStatuses)
                    {
                        var selected = currentValue == status ? "selected" : "";
                        sb.Append($"<option value='{status}' {selected}>{status}</option>");
                    }
                    sb.Append("</select>");
                    return sb.ToString();
                },

                // 🔹 TotalPrice — только для чтения, с подсветкой
                ["TotalPrice"] = (prop, value, fieldName, dropdowns) =>
                {
                    var displayValue = value is decimal d ? $"{d:N2} ₽" : "—";
                    return $"<input type='text' class='form-control bg-light' value='{displayValue}' readonly />" +
                           $"<small class='text-muted'>Рассчитывается автоматически</small>";
                },

                // 🔹 UnitPrice — только для чтения (берётся из услуги)
                ["UnitPrice"] = (prop, value, fieldName, dropdowns) =>
                {
                    var displayValue = value is decimal d ? $"{d:N2} ₽" : "—";
                    return $"<input type='text' class='form-control bg-light' value='{displayValue}' readonly />" +
                           $"<small class='text-muted'>Берётся из выбранной услуги</small>";
                },

                // 🔹 Notes — многострочное поле
                ["Notes"] = (prop, value, fieldName, dropdowns) =>
                    $"<textarea name='{prop.Name}' class='form-control' rows='3' maxlength='500'>{value?.ToString()}</textarea>",
            },

            // 🔹 Логика перед сохранением: пересчёт TotalPrice
            BeforeSaveAsync = async (ctx, entity) =>
            {
                if (entity is Booking booking)
                {
                    if (booking.TrainerServiceId > 0)
                    {
                        // Загружаем TrainerService вместе с Service
                        var trainerService = await ctx.TrainerServices
                            .Include(ts => ts.Service)
                            .FirstOrDefaultAsync(ts => ts.TrainerServiceId == booking.TrainerServiceId);

                        if (trainerService != null)
                        {
                            booking.UnitPrice = trainerService.Service.BasePrice;
                        }
                    }

                    // Пересчитываем общую стоимость
                    booking.TotalPrice = booking.UnitPrice * booking.SessionsCount;

                //Опционально: можно обновить UnitPrice из связанной услуги, если нужно
                     if (booking.TrainerService?.Service?.BasePrice is decimal basePrice)
                        booking.UnitPrice = basePrice;
                }
                await Task.CompletedTask;
            },

            // 🔹 Для Bookings важно загружать навигационные свойства при редактировании
            GetByIdAsync = async (ctx, id) =>
                await ctx.Bookings
                    .Include(b => b.Client)
                    .Include(b => b.TrainerService)
                        .ThenInclude(ts => ts!.Service)
                    .Include(b => b.TrainerService)
                        .ThenInclude(ts => ts!.Trainer)
                    .FirstOrDefaultAsync(b => b.BookingId == id),

            ExistsAsync = (ctx, id) => ctx.Bookings.AnyAsync(b => b.BookingId == id),

            DeleteAsync = async (ctx, id) =>
            {
                var booking = await ctx.Bookings.FindAsync(id);
                if (booking == null) return (false, "Запись не найдена");

                ctx.Bookings.Remove(booking);
                await ctx.SaveChangesAsync();
                return (true, null);
            },

            GetDisplayNameForDeleteAsync = async (ctx, id) =>
            {
                var b = await ctx.Bookings
                    .Include(b => b.Client)
                    .Include(b => b.TrainerService)
                    .ThenInclude(ts => ts!.Service)
                    .FirstOrDefaultAsync(b => b.BookingId == id);

                if (b == null) return $"Запись #{id}";

                var client = b.Client?.FullName ?? "Клиент";
                var service = b.TrainerService?.Service?.ServiceName ?? "Услуга";
                return $"{client} — {service} ({b.BookingDateTime:dd.MM.yyyy HH:mm})";
            },

            DropdownProviders =
            {
                ["ClientId"] = async ctx => await ctx.Clients
                    .Select(c => new SelectListItem(c.FullName, c.ClientId.ToString())) // ✅ Текст, затем Значение
                    .ToListAsync(),

                ["TrainerServiceId"] = async ctx => await ctx.TrainerServices
                    .Include(ts => ts.Trainer)
                    .Include(ts => ts.Service)
                    .Select(ts => new SelectListItem(
                        $"{ts.Trainer!.FullName} — {ts.Service!.ServiceName}", // Текст (что видим)
                        ts.TrainerServiceId.ToString()))                       // Значение (что отправляем)
                    .ToListAsync()
            },

            HiddenInCreate = { "BookingId", "CreatedDate", "TotalPrice" },

            DefaultValues = new()
            {
                ["CreatedDate"] = () => DateTime.Now,
                ["Status"] = () => "Запланировано",
            },
        };

        // ────────────────────────────────────────────────
        // TrainerServices
        // ────────────────────────────────────────────────
        _registry["TrainerServices"] = new EntityAdminMetadata
        {
            Name = "TrainerServices",
            PluralDisplayName = "Услуги тренеров",
            EntityType = typeof(TrainerService),
            KeyPropertyName = "TrainerServiceId",

            QueryFactory = ctx => ctx.TrainerServices
                .Include(ts => ts.Trainer)
                .Include(ts => ts.Service)
                .AsQueryable<object>(),

            GetByIdAsync = async (ctx, id) =>
                await ctx.TrainerServices
                    .Include(ts => ts.Trainer)
                    .Include(ts => ts.Service)
                    .FirstOrDefaultAsync(ts => ts.TrainerServiceId == id),

            ExistsAsync = (ctx, id) => ctx.TrainerServices.AnyAsync(ts => ts.TrainerServiceId == id),

            DeleteAsync = async (ctx, id) =>
            {
                var ts = await ctx.TrainerServices.FindAsync(id);
                if (ts == null) return (false, "Связь не найдена");

                if (await ctx.Bookings.AnyAsync(b => b.TrainerServiceId == id))
                    return (false, "Нельзя удалить связь — есть активные бронирования");

                ctx.TrainerServices.Remove(ts);
                await ctx.SaveChangesAsync();
                return (true, null);
            },

            GetDisplayNameForDeleteAsync = async (ctx, id) =>
            {
                var ts = await ctx.TrainerServices
                    .Include(ts => ts.Trainer)
                    .Include(ts => ts.Service)
                    .FirstOrDefaultAsync(ts => ts.TrainerServiceId == id);

                if (ts == null) return $"Связь #{id}";

                return $"{ts.Trainer?.FullName} — {ts.Service?.ServiceName}";
            },

            DropdownProviders =
            {
                ["TrainerId"] = async ctx => await ctx.Trainers
                    .Select(t => new SelectListItem(
                        t.FullName,              // ✅ 1. Текст (что видит пользователь)
                        t.TrainerId.ToString())) // ✅ 2. Значение (что сохраняется в БД)
                    .ToListAsync(),

                ["ServiceId"] = async ctx => await ctx.Services
                    .Select(s => new SelectListItem(
                        s.ServiceName,           // ✅ 1. Текст
                        s.ServiceId.ToString())) // ✅ 2. Значение
                    .ToListAsync()
            }
        };
    }

    public static EntityAdminMetadata? Get(string? entityName)
    {
        if (string.IsNullOrWhiteSpace(entityName)) return null;
        _registry.TryGetValue(entityName, out var meta);
        return meta;
    }

    public static IEnumerable<EntityAdminMetadata> All => _registry.Values;

    public static string GeneratePhotoInput(PropertyInfo prop, object? currentValue,
        string fieldName, PhotoUploadConfig config)
    {
        var currentUrl = currentValue?.ToString() ?? "";
        var displayUrl = string.IsNullOrWhiteSpace(currentUrl)
            ? config.DefaultImagePath
            : (currentUrl.StartsWith("http") ? currentUrl : "/" + currentUrl.TrimStart('~', '/'));

        var sb = new StringBuilder();

        // Превью
        sb.Append("<div class='current-photo mb-2'>");
        sb.Append($"<label class='form-label small'>{config.PreviewLabel}:</label><br />");
        sb.Append($"<img src='{System.Web.HttpUtility.HtmlAttributeEncode(displayUrl)}' " +
                  $"alt='Превью' " +
                  $"style='max-width:240px; max-height:240px; object-fit:contain;' " +
                  $"class='img-thumbnail mb-2 rounded border' " +
                  $"onerror=\"this.src='{config.DefaultImagePath}';\" />");
        sb.Append("</div>");

        // Инпут загрузки
        sb.Append("<div class='mb-2'>");
        sb.Append($"<label class='form-label small'>{config.UploadLabel}:</label><br />");
        sb.Append($"<label class='btn btn-outline-primary btn-sm' for='file_{fieldName}' id='label_{fieldName}'>");
        sb.Append("<i class='bi bi-upload me-1'></i>📁 Выбрать файл");
        sb.Append("</label>");
        sb.Append($"<input type='file' name='UploadedFile' id='file_{fieldName}' " +  // ✅ name='UploadedFile' для биндинга
                  $"accept='{string.Join(",", config.AllowedExtensions)}' " +
                  $"class='d-none' " +
                  $"onchange=\"document.getElementById('label_{fieldName}').innerHTML = this.files[0] ? '<i class=\\'bi bi-check me-1\\'></i>' + this.files[0].name : '<i class=\\'bi bi-upload me-1\\'></i>Выбрать файл';\" />");
        sb.Append("</div>");
        sb.Append("<small class='text-muted'>");
        sb.Append($"Разрешены: {string.Join(", ", config.AllowedExtensions)} | Макс. {config.MaxSizeBytes / (1024*1024)} МБ");
        sb.Append("</small>");

        // ✅ Скрытое поле с универсальным именем для биндинга
        sb.Append($"<input type='hidden' name='OldFileUrl' value='{System.Web.HttpUtility.HtmlAttributeEncode(currentUrl)}' />");

        return sb.ToString();
    }

    public static string GeneratePhotoInputForCreate(PropertyInfo prop, string fieldName, PhotoUploadConfig config)
    {
        var sb = new StringBuilder();

        sb.Append("<div class='mb-3'>");
        sb.Append($"<label class='form-label'>{config.UploadLabel ?? "Загрузить изображение"}:</label><br />");

        // Стилизованная кнопка + скрытый input
        sb.Append($"<label class='btn btn-outline-primary btn-sm' for='file_{fieldName}' id='label_{fieldName}'>");
        sb.Append("<i class='bi bi-upload me-1'></i>📁 Выбрать файл");
        sb.Append("</label>");

        // ✅ name='UploadedFile' для биндинга в CreateModel
        sb.Append($"<input type='file' name='UploadedFile' id='file_{fieldName}' " +
                  $"accept='{string.Join(",", config.AllowedExtensions)}' " +
                  $"class='d-none' " +
                  $"onchange=\"document.getElementById('label_{fieldName}').innerHTML = this.files[0] ? '<i class=\\'bi bi-check me-1\\'></i>' + this.files[0].name : '<i class=\\'bi bi-upload me-1\\'></i>Выбрать файл';\" />");

        sb.Append("</div>");
        sb.Append("<small class='text-muted'>");
        sb.Append($"Разрешены: {string.Join(", ", config.AllowedExtensions)} | Макс. {config.MaxSizeBytes / (1024*1024)} МБ");
        sb.Append("</small>");

        return sb.ToString();
    }
}
