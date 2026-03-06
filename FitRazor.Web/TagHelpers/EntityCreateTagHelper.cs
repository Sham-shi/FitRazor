using FitRazor.Data.Models;
using FitRazor.Web.Helpers;
using FitRazor.Web.Services.Admin;
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

        [HtmlAttributeName("entity-name")]
        public string EntityName { get; set; } = "Trainers";

        [HtmlAttributeName("submit-text")]
        public string SubmitText { get; set; } = "Создать";

        [HtmlAttributeName("cancel-page")]
        public string CancelPage { get; set; } = "/Entities/Index";


        private readonly string[] _validStatuses = { "Запланировано", "Перенесено", "Завершено", "Отменено" };

        public EntityCreateTagHelper(FitRazorContext context)
        {
            _context = context;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var meta = EntityAdminRegistry.Get(EntityName);
            if (meta == null)
            {
                output.TagName = "div";
                output.Content.SetHtmlContent("<div class='alert alert-warning'>Неизвестная сущность</div>");
                return;
            }

            // Тип сущности
            var modelType = meta.EntityType;

            var properties = Helper.GetFormProperties(modelType);

            // Загрузка dropdown
            var dropdownData = new Dictionary<string, IEnumerable<SelectListItem>>();
            foreach (var provider in meta.DropdownProviders)
            {
                dropdownData[provider.Key] = await provider.Value(_context);
            }

            output.Attributes.SetAttribute("class", "entity-create-form");
            var html = GenerateHtml(modelType, properties, dropdownData);
            output.Content.SetHtmlContent(html);
        }

        private string GenerateHtml(Type modelType, IEnumerable<PropertyInfo> properties,
            Dictionary<string, IEnumerable<SelectListItem>> dropdownData)
        {
            var html = new System.Text.StringBuilder();

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

            // ────────────────────────────────────────────────
            // Блок для загрузки фото при создании
            // ────────────────────────────────────────────────
            bool isPhotoField = propName.EndsWith("PhotoUrl", StringComparison.OrdinalIgnoreCase) ||
                                propName.EndsWith("ImageUrl", StringComparison.OrdinalIgnoreCase) ||
                                propName.EndsWith("AvatarUrl", StringComparison.OrdinalIgnoreCase);

            if (isPhotoField && (propType == typeof(string)))
            {
                var sb = new System.Text.StringBuilder();

                sb.Append("<div class='mb-3'>");
                sb.Append("<label class='form-label'>Фото (jpg, png, до 5 МБ)</label>");

                // Стилизованная кнопка + скрытый input
                sb.Append($"<label class='btn btn-primary text-white d-inline-block' for='file_{propName}'>");
                sb.Append("<i class='bi bi-image me-2'></i>Выбрать фото");
                sb.Append($"<input type='file' name='{propName}' id='file_{propName}' accept='image/jpeg,image/png' class='d-none' onchange=\"document.getElementById('file_label_{propName}').textContent = this.files[0]?.name || 'Файл не выбран';\" />");
                sb.Append("</label>");

                // Место для имени выбранного файла
                sb.Append($"<div class='mt-2 text-muted small' id='selected-file-{propName}'>Файл не выбран</div>");

                sb.Append("</div>");

                return sb.ToString();
            }

            if (propName == "Status")
            {
                // Определяем допустимые статусы (можно вынести в константу или конфиг)
                var validStatuses = new[] { "Запланировано", "Перенесено", "Завершено", "Отменено" };

                var sb = new System.Text.StringBuilder();
                sb.Append($"<select name='{propName}' class='form-select'>");
                sb.Append("<option value=''>— Выберите статус —</option>");

                foreach (var status in validStatuses)
                {
                    // По умолчанию можно выбрать первый элемент, если нужно:
                    // var isSelected = status == "Запланировано"; 
                    var encodedStatus = System.Web.HttpUtility.HtmlAttributeEncode(status);
                    sb.Append($"<option value='{encodedStatus}'>{status}</option>");
                }
                sb.Append("</select>");
                return sb.ToString();
            }

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
                var requiredAttr = prop.GetCustomAttribute<RequiredAttribute>() != null ? "required" : "";
                // step='0.01' важен для корректной валидации в браузере
                // placeholder помогает пользователю понять формат (точка!)
                return $"<input type='number' name='{propName}' class='form-control' step='0.01' {requiredAttr} placeholder='0.00' />";
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