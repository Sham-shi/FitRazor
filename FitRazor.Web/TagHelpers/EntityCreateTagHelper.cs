using FitRazor.Data.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace FitRazor.Web.TagHelpers
{
    [HtmlTargetElement("entity-create-form")]
    public class EntityCreateTagHelper : TagHelper
    {
        private readonly FitRazorContext _context;

        private static readonly Dictionary<string, Type> EntityTypes = new()
        {
            { "Trainers", typeof(Trainer) },
            { "Clients", typeof(Client) },
            { "Services", typeof(Service) },
            { "Bookings", typeof(Booking) },
            { "TrainerServices", typeof(TrainerService) }
        };

        [HtmlAttributeName("entity-name")]
        public string EntityName { get; set; } = "Trainers";

        [HtmlAttributeName("submit-text")]
        public string SubmitText { get; set; } = "Создать";

        [HtmlAttributeName("cancel-page")]
        public string CancelPage { get; set; } = "/Entities/Index";

        public EntityCreateTagHelper(FitRazorContext context)
        {
            _context = context;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var modelType = GetEntityType();
            var properties = GetFormProperties(modelType);
            var dropdownData = await GetDropdownDataAsync();

            output.TagName = "form";
            output.Attributes.SetAttribute("method", "post");
            output.Attributes.SetAttribute("class", "entity-create-form");

            var html = GenerateHtml(modelType, properties, dropdownData);
            output.Content.SetHtmlContent(html);
        }

        private Type GetEntityType()
        {
            return EntityName switch
            {
                "Trainers" => typeof(Trainer),
                "Clients" => typeof(Client),
                "Services" => typeof(Service),
                "Bookings" => typeof(Booking),
                "TrainerServices" => typeof(TrainerService),
                _ => throw new ArgumentException($"Неизвестная сущность: {EntityName}")
            };
        }

        private IEnumerable<PropertyInfo> GetFormProperties(Type type)
        {
            return type.GetProperties()
                .Where(p =>
                    p.CanWrite &&
                    p.CanRead &&
                    !p.PropertyType.IsGenericType &&
                    !p.PropertyType.IsCollection() &&
                    // Исключаем только PK, но оставляем FK (ClientId, TrainerId...)
                    p.Name != "Id" &&
                    p.Name != $"{type.Name}Id")
                .OrderBy(p =>
                {
                    var displayAttr = p.GetCustomAttribute<DisplayAttribute>();
                    return displayAttr?.GetOrder() ?? 1000;
                });
        }

        private async Task<Dictionary<string, IEnumerable<SelectListItem>>> GetDropdownDataAsync()
        {
            var dropdowns = new Dictionary<string, IEnumerable<SelectListItem>>();

            // Данные для выпадающих списков
            dropdowns["ClientId"] = await _context.Clients
                .Select(c => new SelectListItem
                {
                    Value = c.ClientId.ToString(),
                    Text = c.FullName
                })
                .ToListAsync();

            dropdowns["TrainerId"] = await _context.Trainers
                .Select(t => new SelectListItem
                {
                    Value = t.TrainerId.ToString(),
                    Text = t.FullName
                })
                .ToListAsync();

            dropdowns["ServiceId"] = await _context.Services
                .Select(s => new SelectListItem
                {
                    Value = s.ServiceId.ToString(),
                    Text = s.ServiceName
                })
                .ToListAsync();

            dropdowns["TrainerServiceId"] = await _context.TrainerServices
                .Include(ts => ts.Trainer)
                .Include(ts => ts.Service)
                .Select(ts => new SelectListItem
                {
                    Value = ts.TrainerServiceId.ToString(),
                    Text = $"{ts.Trainer.FullName} — {ts.Service.ServiceName}"
                })
                .ToListAsync();

            return dropdowns;
        }

        private string GenerateHtml(Type modelType, IEnumerable<PropertyInfo> properties,
            Dictionary<string, IEnumerable<SelectListItem>> dropdownData)
        {
            var html = new System.Text.StringBuilder();

            // Скрытое поле для имени сущности (нужно для POST)
            html.Append($"<input type='hidden' name='EntityName' value='{EntityName}' />");

            html.Append("<div class='row'>");

            foreach (var prop in properties)
            {
                var displayName = prop.GetCustomAttribute<DisplayAttribute>()?.Name ?? prop.Name;
                var isRequired = prop.GetCustomAttribute<RequiredAttribute>() != null;
                var placeholder = prop.GetCustomAttribute<DisplayAttribute>()?.Prompt ?? "";

                html.Append("<div class='col-md-6 mb-3'>");
                html.Append($"<label class='form-label'>");
                html.Append(displayName);
                if (isRequired) html.Append(" <span class='text-danger'>*</span>");
                html.Append("</label>");

                // Определяем тип поля
                var inputHtml = GenerateInput(prop, displayName, dropdownData);
                html.Append(inputHtml);

                // Валидация
                html.Append($"<span asp-validation-for='{prop.Name}' class='text-danger'></span>");

                html.Append("</div>");
            }

            html.Append("</div>");

            // Кнопки
            html.Append("<div class='row mt-4'>");
            html.Append("<div class='col-12'>");
            html.Append($"<button type='submit' class='btn btn-success'>{SubmitText}</button>");
            html.Append($" <a href='{CancelPage}/{EntityName}' class='btn btn-secondary'>Отмена</a>");
            html.Append("</div>");
            html.Append("</div>");

            return html.ToString();
        }

        private string GenerateInput(PropertyInfo prop, string fieldName,
            Dictionary<string, IEnumerable<SelectListItem>> dropdownData)
        {
            var propType = prop.PropertyType;
            var propName = prop.Name;

            // Выпадающий список для foreign keys
            if (propName.EndsWith("Id") && dropdownData.ContainsKey(propName))
            {
                var options = dropdownData[propName];
                var sb = new System.Text.StringBuilder();
                sb.Append($"<select name='{propName}' class='form-select'>");
                sb.Append("<option value=''>— Выберите —</option>");
                foreach (var opt in options)
                {
                    sb.Append($"<option value='{opt.Value}'>{opt.Text}</option>");
                }
                sb.Append("</select>");
                return sb.ToString();
            }

            // Текстовые поля
            if (propType == typeof(string))
            {
                var maxLength = prop.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength ?? 500;
                var isEmail = propName.Contains("Email", StringComparison.OrdinalIgnoreCase);
                var isPhone = propName.Contains("Phone", StringComparison.OrdinalIgnoreCase);
                var isUrl = propName.Contains("Url", StringComparison.OrdinalIgnoreCase) ||
                           propName.Contains("Photo", StringComparison.OrdinalIgnoreCase);

                if (isEmail)
                {
                    return $"<input type='email' name='{propName}' class='form-control' maxlength='{maxLength}' />";
                }
                if (isPhone)
                {
                    return $"<input type='tel' name='{propName}' class='form-control' maxlength='{maxLength}' />";
                }
                if (isUrl)
                {
                    return $"<input type='url' name='{propName}' class='form-control' maxlength='{maxLength}' />";
                }

                // Многострочное поле для длинного текста
                if (maxLength > 200)
                {
                    return $"<textarea name='{propName}' class='form-control' rows='3' maxlength='{maxLength}'></textarea>";
                }

                return $"<input type='text' name='{propName}' class='form-control' maxlength='{maxLength}' />";
            }

            // Числовые поля
            if (propType == typeof(int) || propType == typeof(int?))
            {
                return $"<input type='number' name='{propName}' class='form-control' />";
            }

            if (propType == typeof(decimal) || propType == typeof(decimal?))
            {
                return $"<input type='number' name='{propName}' class='form-control' step='0.01' />";
            }

            // Дата и время
            if (propType == typeof(DateTime) || propType == typeof(DateTime?))
            {
                return $"<input type='datetime-local' name='{propName}' class='form-control' />";
            }

            if (propType == typeof(DateOnly) || propType == typeof(DateOnly?))
            {
                return $"<input type='date' name='{propName}' class='form-control' />";
            }

            // По умолчанию — текст
            return $"<input type='text' name='{propName}' class='form-control' />";
        }
    }

    // Extension method для проверки на коллекцию
    public static class TypeExtensions
    {
        public static bool IsCollection(this Type type)
        {
            return typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string);
        }
    }
}