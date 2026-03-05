using FitRazor.Data.Models;
using FitRazor.Web.Services.Admin;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace FitRazor.Web.TagHelpers
{
    [HtmlTargetElement("entity-edit-form")]
    public class EntityEditTagHelper : TagHelper
    {
        private readonly FitRazorContext _context;

        [HtmlAttributeName("entity-name")]
        public string EntityName { get; set; } = "Trainers";

        [HtmlAttributeName("entity-id")]
        public int EntityId { get; set; }

        [HtmlAttributeName("submit-text")]
        public string SubmitText { get; set; } = "Сохранить";

        [HtmlAttributeName("cancel-page")]
        public string CancelPage { get; set; } = "/Entities/Index";

        public EntityEditTagHelper(FitRazorContext context)
        {
            _context = context;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            // Получаем метаданные
            var meta = EntityAdminRegistry.Get(EntityName);
            if (meta == null)
            {
                output.TagName = "div";
                output.Content.SetHtmlContent("<div class='alert alert-warning'>Неизвестная сущность</div>");
                return;
            }

            var entity = await meta.GetByIdAsync(_context, EntityId);
            if (entity == null)
            {
                output.TagName = "div";
                output.Content.SetHtmlContent("<div class='alert alert-warning'>Запись не найдена</div>");
                return;
            }

            var modelType = entity.GetType();
            var properties = GetFormProperties(modelType);

            var dropdownData = new Dictionary<string, IEnumerable<SelectListItem>>();
            foreach (var provider in meta.DropdownProviders)
            {
                dropdownData[provider.Key] = await provider.Value(_context);
            }

            output.Attributes.SetAttribute("class", "entity-edit-form");
            var html = GenerateHtml(entity, modelType, properties, dropdownData);
            output.Content.SetHtmlContent(html);
        }

        private IEnumerable<PropertyInfo> GetFormProperties(Type type)
        {
            return type.GetProperties()
                .Where(p =>
                    p.CanWrite &&
                    p.CanRead &&
                    !p.PropertyType.IsGenericType &&
                    !p.PropertyType.IsCollection() &&
                    p.Name != "Id" &&
                    p.Name != $"{type.Name}Id" &&
                    // 👇 Добавляем проверку на ScaffoldColumn
                    p.GetCustomAttribute<ScaffoldColumnAttribute>()?.Scaffold != false)
                .OrderBy(p =>
                {
                    var displayAttr = p.GetCustomAttribute<DisplayAttribute>();
                    return displayAttr?.GetOrder() ?? 1000;
                });
        }

        private string GenerateHtml(object entity, Type modelType, IEnumerable<PropertyInfo> properties,
            Dictionary<string, IEnumerable<SelectListItem>> dropdownData)
        {
            var html = new System.Text.StringBuilder();

            // Добавляем скрытое поле с ID сущности (нужно для EF Core)
            var idProp = modelType.GetProperties()
                .FirstOrDefault(p => p.Name.EndsWith("Id") || p.Name == "Id");
            if (idProp != null)
            {
                html.Append($"<input type='hidden' name='{idProp.Name}' value='{EntityId}' />");
            }

            html.Append("<div class='row'>");

            foreach (var prop in properties)
            {
                var displayName = prop.GetCustomAttribute<DisplayAttribute>()?.Name ?? prop.Name;
                var isRequired = prop.GetCustomAttribute<RequiredAttribute>() != null;
                var currentValue = prop.GetValue(entity);

                html.Append("<div class='col-md-6 mb-3'>");
                html.Append($"<label class='form-label'>");
                html.Append(displayName);
                if (isRequired) html.Append(" <span class='text-danger'>*</span>");
                html.Append("</label>");

                var inputHtml = GenerateInput(prop, displayName, currentValue, dropdownData);
                html.Append(inputHtml);

                html.Append($"<span asp-validation-for='{prop.Name}' class='text-danger'></span>");
                html.Append("</div>");
            }

            html.Append("</div>");

            // Кнопки
            html.Append("<div class='row mt-4'>");
            html.Append("<div class='col-12'>");
            html.Append($"<button type='submit' class='btn btn-primary'>{SubmitText}</button>");
            html.Append($" <a href='{CancelPage}/{EntityName}' class='btn btn-secondary'>Отмена</a>");
            html.Append("</div>");
            html.Append("</div>");

            return html.ToString();
        }

        private string GenerateInput(PropertyInfo prop, string fieldName, object? currentValue,
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
                    var isSelected = currentValue?.ToString() == opt.Value;
                    sb.Append($"<option value='{opt.Value}' {(isSelected ? "selected" : "")}>{opt.Text}</option>");
                }
                sb.Append("</select>");
                return sb.ToString();
            }

            // Текстовые поля
            if (propType == typeof(string))
            {
                var maxLength = prop.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength ?? 500;
                var value = currentValue?.ToString() ?? "";
                var isEmail = propName.Contains("Email", StringComparison.OrdinalIgnoreCase);
                var isPhone = propName.Contains("Phone", StringComparison.OrdinalIgnoreCase);
                var isUrl = propName.Contains("Url", StringComparison.OrdinalIgnoreCase) ||
                           propName.Contains("Photo", StringComparison.OrdinalIgnoreCase);

                if (isEmail)
                {
                    return $"<input type='email' name='{propName}' class='form-control' value='{value}' maxlength='{maxLength}' />";
                }
                if (isPhone)
                {
                    return $"<input type='tel' name='{propName}' class='form-control' value='{value}' maxlength='{maxLength}' />";
                }
                if (isUrl)
                {
                    return $"<input type='url' name='{propName}' class='form-control' value='{value}' maxlength='{maxLength}' />";
                }

                if (maxLength > 200)
                {
                    return $"<textarea name='{propName}' class='form-control' rows='3' maxlength='{maxLength}'>{value}</textarea>";
                }

                return $"<input type='text' name='{propName}' class='form-control' value='{value}' maxlength='{maxLength}' />";
            }

            // Числовые поля
            if (propType == typeof(int) || propType == typeof(int?))
            {
                var value = currentValue?.ToString() ?? "0";
                return $"<input type='number' name='{propName}' class='form-control' value='{value}' />";
            }

            if (propType == typeof(decimal) || propType == typeof(decimal?))
            {
                var value = currentValue != null ? ((decimal)currentValue).ToString("F2") : "0.00";
                return $"<input type='number' name='{propName}' class='form-control' value='{value}' step='0.01' />";
            }

            // Дата и время
            if (propType == typeof(DateTime) || propType == typeof(DateTime?))
            {
                var value = currentValue != null ? ((DateTime)currentValue).ToString("yyyy-MM-ddTHH:mm") : "";
                return $"<input type='datetime-local' name='{propName}' class='form-control' value='{value}' />";
            }

            if (propType == typeof(DateOnly) || propType == typeof(DateOnly?))
            {
                var value = currentValue != null ? ((DateOnly)currentValue).ToString("yyyy-MM-dd") : "";
                return $"<input type='date' name='{propName}' class='form-control' value='{value}' />";
            }

            // По умолчанию
            var defaultValue = currentValue?.ToString() ?? "";
            return $"<input type='text' name='{propName}' class='form-control' value='{defaultValue}' />";
        }
    }
}
