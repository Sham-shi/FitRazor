using FitRazor.Data.Models;
using FitRazor.Web.Helpers;
using FitRazor.Web.Services.Admin;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace FitRazor.Web.TagHelpers
{
    [HtmlTargetElement("entity-list")]
    public class EntityListTagHelper : TagHelper
    {
        private readonly FitRazorContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Название сущности (Trainers, Clients, Services...)
        [HtmlAttributeName("entity-name")]
        public string EntityName { get; set; } = "Trainers";

        // Заголовок таблицы
        [HtmlAttributeName("table-title")]
        public string? TableTitle { get; set; }

        // Показывать кнопки действий (Edit, Delete)
        [HtmlAttributeName("show-actions")]
        public bool ShowActions { get; set; } = true;

        // Страница для деталей
        [HtmlAttributeName("details-page")]
        public string? DetailsPage { get; set; } = "/Entities/Details";

        // Страница для редактирования
        [HtmlAttributeName("edit-page")]
        public string? EditPage { get; set; } = "/Entities/Edit";

        public EntityListTagHelper(FitRazorContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor=httpContextAccessor;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var meta = EntityAdminRegistry.Get(EntityName);
            if (meta == null)
            {
                output.TagName = "div";
                output.Attributes.SetAttribute("class", "alert alert-warning");
                output.Content.SetHtmlContent($"⚠️ Метаданные для сущности '{EntityName}' не найдены");
                return;
            }

            var query = meta.QueryFactory(_context);

            // 🔹 ФИЛЬТРАЦИЯ ПО РОЛИ ПОЛЬЗОВАТЕЛЯ
            var httpContext = _httpContextAccessor.HttpContext; // или через IHttpContextAccessor
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var userManager = httpContext.RequestServices.GetService<UserManager<ApplicationUser>>();
                var user = await userManager?.GetUserAsync(httpContext.User);

                if (user != null)
                {
                    if (await userManager.IsInRoleAsync(user, "Client") && EntityName == "Bookings")
                    {
                        // Клиент видит только свои бронирования
                        query = query.Cast<Booking>()
                            .Where(b => b.ClientId == user.ClientId)
                            .Cast<object>();
                    }
                    else if (await userManager.IsInRoleAsync(user, "Trainer") && EntityName == "Bookings")
                    {
                        // Тренер видит записи через свои услуги
                        var trainerServiceIds = await _context.TrainerServices
                            .Where(ts => ts.TrainerId == user.TrainerId)
                            .Select(ts => ts.TrainerServiceId)
                            .ToListAsync();

                        query = query.Cast<Booking>()
                            .Where(b => trainerServiceIds.Contains(b.TrainerServiceId))
                            .Cast<object>();
                    }
                    // Админ видит всё — без фильтрации
                }
            }

            var data = await meta.QueryFactory(_context).ToListAsync();

            output.TagName = "div";
            output.Attributes.SetAttribute("class", "entity-list-container");
            output.Content.SetHtmlContent(GenerateHtml(data, meta));
        }

        private string GenerateHtml(IEnumerable<object> data, EntityAdminMetadata meta)
        {
            var items = data.ToList();
            if (!items.Any())
            {
                return "<div class='alert alert-info'>Записей не найдено</div>";
            }

            // Получаем свойства для отображения
            var properties = meta.GetDisplayPropertiesFunc?.Invoke(meta.EntityType)
                           ?? Helper.GetFormProperties(meta.EntityType);

            var html = new StringBuilder();

            // Заголовок
            if (!string.IsNullOrEmpty(TableTitle))
            {
                html.Append($"<h2 class='mb-3'>{TableTitle}</h2>");
            }

            // Таблица
            html.Append("<div class='table-responsive'>");
            html.Append("<table class='table table-hover table-bordered'>");

            // Заголовки колонок
            html.Append("<thead class='table-dark borderer'>");
            html.Append("<tr>");
            html.Append("<th class='text-center' style='width: 50px;'>#</th>");

            foreach (var prop in properties)
            {
                var displayName = prop.GetCustomAttribute<DisplayAttribute>()?.Name ?? prop.Name;
                html.Append($"<th>{displayName}</th>");
            }

            if (ShowActions)
            {
                html.Append("<th class='text-center' style='width: 200px;'>Действия</th>");
            }

            html.Append("</tr>");
            html.Append("</thead>");

            // Тело таблицы
            html.Append("<tbody>");
            int index = 1;
            foreach (var item in items)
            {
                html.Append("<tr>");
                html.Append($"<td class='text-center'>{index}</td>");

                foreach (var prop in properties)
                {
                    var value = prop.GetValue(item);
                    html.Append("<td>");
                    html.Append(FormatValue(value, prop.PropertyType, prop.Name, item, meta));
                    html.Append("</td>");
                }

                if (ShowActions)
                {
                    var id = GetIdValue(item, meta);
                    var displayName = meta.GetDisplayNameFunc?.Invoke(item)
                                   ?? $"{meta.PluralDisplayName} #{id}";

                    var request = _httpContextAccessor.HttpContext?.Request;
                    var returnUrl = request != null ? request.Path + request.QueryString : "";

                    html.Append("<td class='text-center'>");
                    html.Append($@"
                        <a href='{DetailsPage}/{EntityName}/{id}?returnUrl={System.Net.WebUtility.UrlEncode(returnUrl)}'
                            data-bs-toggle='tooltip'
                            data-bs-title='Детали' 
                            class='btn btn-sm btn-info me-2'>📄</a>");

                    html.Append($@"
                        <a href='{EditPage}/{EntityName}/{id}?returnUrl={System.Net.WebUtility.UrlEncode(returnUrl)}'
                            data-bs-toggle='tooltip'
                            data-bs-title='Редактировать'
                            class='btn btn-sm btn-primary me-1'>✏️</a>");
                    html.Append($@"
                        <button type='button' class='btn btn-sm btn-danger'
                                data-bs-toggle='modal' data-bs-target='#deleteModal'
                                data-bs-toggle='tooltip' data-bs-title='Удалить'
                                data-entity-name='{EntityName}'
                                data-entity-id='{id}'
                                data-entity-display='{System.Web.HttpUtility.HtmlAttributeEncode(displayName)}'>
                            🗑️
                        </button>");
                    html.Append("</td>");
                }

                html.Append("</tr>");
                index++;
            }
            html.Append("</tbody>");
            html.Append("</table>");
            html.Append("</div>");

            return html.ToString();
        }

        private string FormatValue(object? value, Type type, string propertyName, object? entity, EntityAdminMetadata meta)
        {
            // 🔹 1. Проверяем кастомный форматтер из метаданных
            if (meta.PropertyFormatters?.TryGetValue(propertyName, out var formatter) == true && formatter != null)
            {
                return value != null ? formatter(value, type) : "<span class='text-muted'>—</span>";
            }

            // 🔹 2. Обработка изображений
            if (meta.ImageProperties.Contains(propertyName) ||
                propertyName.EndsWith("PhotoUrl", StringComparison.OrdinalIgnoreCase) ||
                propertyName.EndsWith("ImageUrl", StringComparison.OrdinalIgnoreCase) ||
                propertyName.EndsWith("AvatarUrl", StringComparison.OrdinalIgnoreCase))
            {
                return FormatImage(value?.ToString(), meta.DefaultImagePath ?? "/Images/no-photo.jpg");
            }

            // 🔹 3. Обработка навигационных свойств (Foreign Keys)
            if (propertyName.EndsWith("Id", StringComparison.OrdinalIgnoreCase) && entity != null && value != null)
            {
                var navPropName = propertyName[..^2]; // убираем "Id"
                var navProp = entity.GetType().GetProperty(navPropName);

                if (navProp != null)
                {
                    var navValue = navProp.GetValue(entity);
                    if (navValue != null)
                    {
                        // 🔹 Специальная обработка для TrainerService: показываем "Тренер — Услуга"
                        if (navPropName == "TrainerService")
                        {
                            var trainerProp = navValue.GetType().GetProperty("Trainer");
                            var serviceProp = navValue.GetType().GetProperty("Service");

                            var trainer = trainerProp?.GetValue(navValue);
                            var service = serviceProp?.GetValue(navValue);

                            var trainerName = trainer?.GetType()
                                .GetProperty("FullName")?.GetValue(trainer)?.ToString();
                            var serviceName = service?.GetType()
                                .GetProperty("ServiceName")?.GetValue(service)?.ToString();

                            if (!string.IsNullOrWhiteSpace(trainerName) && !string.IsNullOrWhiteSpace(serviceName))
                            {
                                return System.Net.WebUtility.HtmlEncode($"{trainerName} — {serviceName}");
                            }
                            // Если есть только одно из значений
                            if (!string.IsNullOrWhiteSpace(trainerName))
                                return System.Net.WebUtility.HtmlEncode(trainerName);
                            if (!string.IsNullOrWhiteSpace(serviceName))
                                return System.Net.WebUtility.HtmlEncode(serviceName);
                        }

                        // Пытаемся получить имя через стандартные свойства
                        var displayName = navValue.GetType()
                            .GetProperty("FullName")?.GetValue(navValue)?.ToString()
                            ?? navValue.GetType().GetProperty("Name")?.GetValue(navValue)?.ToString()
                            ?? navValue.GetType().GetProperty("ServiceName")?.GetValue(navValue)?.ToString();

                        if (!string.IsNullOrWhiteSpace(displayName))
                        {
                            return System.Net.WebUtility.HtmlEncode(displayName);
                        }
                    }
                }
                // Если навигация не загружена — показываем ID
                return $"<span class='text-muted' title='Навигационное свойство не загружено'>#{value}</span>";
            }

            // 🔹 4. Стандартное форматирование по типу
            return value switch
            {
                null => "<span class='text-muted'>—</span>",
                decimal d => $"<span class='text-success fw-bold'>{d:N2} ₽</span>",
                DateTime dt => $"<span>{dt:dd.MM.yyyy HH:mm}</span>",
                DateOnly date => $"<span>{date:dd.MM.yyyy}</span>",
                string str => string.IsNullOrEmpty(str) ? "<span class='text-muted'>—</span>" : str,
                _ => System.Net.WebUtility.HtmlEncode(value.ToString())
            };
        }

        private string FormatImage(string? url, string defaultPath)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return $"<img src='{defaultPath}' alt='Нет фото' style='min-width:100px;min-height:100px;object-fit:cover;' class='img-thumbnail rounded' />";
            }

            var imageUrl = url;
            if (!imageUrl.StartsWith("http") && !imageUrl.StartsWith("/"))
            {
                imageUrl = "/" + imageUrl.TrimStart('~', '/');
            }

            return $"<img src='{System.Web.HttpUtility.HtmlAttributeEncode(imageUrl)}' " +
                   $"alt='Фото' " +
                   $"style='min-width:100px;min-height:100px;object-fit:cover;' " +
                   $"class='img-thumbnail rounded' " +
                   $"onerror=\"this.onerror=null; this.src='{defaultPath}';this.alt='Фото отсутствует'\" />";
        }

        private object GetIdValue(object item, EntityAdminMetadata meta)
        {
            var idProp = item.GetType().GetProperty(meta.KeyPropertyName);
            return idProp?.GetValue(item) ?? "0";
        }
    }
}