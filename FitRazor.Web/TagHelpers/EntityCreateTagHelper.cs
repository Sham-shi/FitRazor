// FitRazor.Web/TagHelpers/EntityCreateTagHelper.cs
using FitRazor.Data.Models;
using FitRazor.Web.Helpers;
using FitRazor.Web.Services.Admin;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace FitRazor.Web.TagHelpers
{
    [HtmlTargetElement("entity-create-form")]
    public class EntityCreateTagHelper : TagHelper
    {
        private readonly FitRazorContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        [HtmlAttributeName("entity-name")]
        public string EntityName { get; set; } = "Trainers";

        [HtmlAttributeName("submit-text")]
        public string SubmitText { get; set; } = "Создать";

        [HtmlAttributeName("cancel-page")]
        public string CancelPage { get; set; }

        public EntityCreateTagHelper(FitRazorContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var meta = EntityAdminRegistry.Get(EntityName);
            if (meta == null)
            {
                output.TagName = "div";
                output.Attributes.SetAttribute("class", "alert alert-warning");
                output.Content.SetHtmlContent($"⚠️ Метаданные для '{EntityName}' не найдены");
                return;
            }

            var modelType = meta.EntityType;
            var properties = meta.GetEditPropertiesFunc?.Invoke(modelType)
                           ?? Helper.GetFormProperties(modelType);

            // 🔹 Фильтруем скрытые свойства для создания
            properties = properties.Where(p =>
                !meta.HiddenProperties.Contains(p.Name) &&
                !meta.HiddenInCreate.Contains(p.Name));

            // Загружаем dropdown
            var dropdownData = new Dictionary<string, IEnumerable<SelectListItem>>();
            foreach (var provider in meta.DropdownProviders)
            {
                dropdownData[provider.Key] = await provider.Value(_context);
            }

            output.TagName = "form";
            output.Attributes.SetAttribute("method", "post");
            output.Attributes.SetAttribute("enctype", "multipart/form-data");
            output.Attributes.SetAttribute("class", "entity-create-form");

            var html = GenerateHtml(modelType, properties, dropdownData, meta);
            output.Content.SetHtmlContent(html);
        }

        private string GenerateHtml(Type modelType, IEnumerable<PropertyInfo> properties,
            Dictionary<string, IEnumerable<SelectListItem>> dropdownData, EntityAdminMetadata meta)
        {
            var html = new StringBuilder();

            // 🔐 Antiforgery token
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.RequestServices?.GetService<IAntiforgery>() is IAntiforgery antiforgery)
            {
                var tokens = antiforgery.GetAndStoreTokens(httpContext);
                html.Append($"<input name='__RequestVerificationToken' type='hidden' value='{tokens.RequestToken}' />");
            }

            html.Append("<div class='row'>");

            foreach (var prop in properties)
            {
                var displayName = prop.GetCustomAttribute<DisplayAttribute>()?.Name ?? prop.Name;
                var isRequired = prop.GetCustomAttribute<RequiredAttribute>() != null;
                var propName = prop.Name;

                html.Append("<div class='col-md-6 mb-3'>");
                html.Append($"<label class='form-label fw-semibold'>");
                html.Append(displayName);
                if (isRequired) html.Append(" <span class='text-danger'>*</span>");
                html.Append("</label>");

                // 🔹 Кастомный генератор или стандартный
                if (meta.CustomInputGenerators?.TryGetValue(propName, out var generator) == true && generator != null)
                {
                    html.Append(generator(prop, null, displayName, dropdownData));
                }
                else
                {
                    html.Append(GenerateStandardInput(prop, dropdownData, meta));
                }

                html.Append($"<span asp-validation-for='{propName}' class='text-danger small'></span>");
                html.Append("</div>");
            }

            html.Append("</div>");

            // 🔹 Кнопки
            html.Append("<div class='row mt-4 pt-3 border-top'>");
            html.Append("<div class='col-12 d-flex gap-2'>");
            html.Append($"<button type='submit' class='btn btn-success px-4'><i class='bi bi-plus-lg me-2'></i>{SubmitText}</button>");

            var cancelUrl = !string.IsNullOrEmpty(CancelPage) && CancelPage.StartsWith("/")
                ? CancelPage
                : $"/Entities/Index/{EntityName}";

            html.Append($"<a href='{cancelUrl}' class='btn btn-outline-secondary'><i class='bi bi-x-lg me-2'></i>Отмена</a>");
            html.Append("</div>");
            html.Append("</div>");

            return html.ToString();
        }

        private string GenerateStandardInput(PropertyInfo prop,
            Dictionary<string, IEnumerable<SelectListItem>> dropdownData, EntityAdminMetadata meta)
        {
            var propType = prop.PropertyType;
            var propName = prop.Name;

            // 🔹 Фото (используем общий метод из реестра)
            if (meta.PhotoUploadConfigs.TryGetValue(propName, out var photoConfig) ||
                propName.EndsWith("PhotoUrl", StringComparison.OrdinalIgnoreCase) ||
                propName.EndsWith("ImageUrl", StringComparison.OrdinalIgnoreCase) ||
                propName.EndsWith("AvatarUrl", StringComparison.OrdinalIgnoreCase))
            {
                var config = photoConfig ?? new PhotoUploadConfig { Subfolder = "Uploads" };
                return EntityAdminRegistry.GeneratePhotoInputForCreate(prop, propName, config);
            }

            // 🔹 Выпадающий список для FK
            if (propName.EndsWith("Id") && dropdownData.ContainsKey(propName))
            {
                var sb = new StringBuilder();
                sb.Append($"<select name='{propName}' class='form-select'>");
                sb.Append("<option value=''>— Выберите —</option>");
                foreach (var opt in dropdownData[propName])
                {
                    sb.Append($"<option value='{opt.Value}'>{opt.Text}</option>");
                }
                sb.Append("</select>");
                return sb.ToString();
            }

            // 🔹 Текстовые поля
            if (propType == typeof(string))
            {
                return GenerateTextInput(prop, "", "form-control", "");
            }

            // 🔹 Числовые
            if (propType == typeof(int) || propType == typeof(int?))
            {
                return $"<input type='number' name='{propName}' class='form-control' />";
            }
            if (propType == typeof(decimal) || propType == typeof(decimal?))
            {
                var required = prop.GetCustomAttribute<RequiredAttribute>() != null ? "required" : "";
                return $"<input type='number' name='{propName}' class='form-control' step='0.01' {required} placeholder='0.00' />";
            }

            // 🔹 Дата
            if (propType == typeof(DateTime) || propType == typeof(DateTime?))
            {
                return $"<input type='datetime-local' name='{propName}' class='form-control' />";
            }
            if (propType == typeof(DateOnly) || propType == typeof(DateOnly?))
            {
                return $"<input type='date' name='{propName}' class='form-control' />";
            }

            return $"<input type='text' name='{propName}' class='form-control' />";
        }

        private string GenerateTextInput(PropertyInfo prop, string value, string classAttr, string readOnlyAttr)
        {
            var propName = prop.Name;
            var maxLength = prop.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength ?? 500;
            var isEmail = propName.Contains("Email", StringComparison.OrdinalIgnoreCase);
            var isPhone = propName.Contains("Phone", StringComparison.OrdinalIgnoreCase);
            var isUrl = propName.Contains("Url", StringComparison.OrdinalIgnoreCase) && !propName.Contains("Photo", StringComparison.OrdinalIgnoreCase);

            if (isEmail)
                return $"<input type='email' name='{propName}' class='{classAttr}' maxlength='{maxLength}' {readOnlyAttr} />";
            if (isPhone)
                return $"<input type='tel' name='{propName}' class='{classAttr}' maxlength='{maxLength}' {readOnlyAttr} placeholder='+7 (___) ___-__-__' />";
            if (isUrl)
                return $"<input type='url' name='{propName}' class='{classAttr}' maxlength='{maxLength}' {readOnlyAttr} />";

            if (maxLength > 200)
                return $"<textarea name='{propName}' class='{classAttr}' rows='3' maxlength='{maxLength}' {readOnlyAttr}></textarea>";

            return $"<input type='text' name='{propName}' class='{classAttr}' maxlength='{maxLength}' {readOnlyAttr} />";
        }
    }
}