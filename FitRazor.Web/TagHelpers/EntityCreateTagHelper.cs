// FitRazor.Web/TagHelpers/EntityCreateTagHelper.cs
using FitRazor.Data.Models;
using FitRazor.Web.Helpers;
using FitRazor.Web.Services.Admin;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;

namespace FitRazor.Web.TagHelpers
{
    [HtmlTargetElement("entity-create-form")]
    public class EntityCreateTagHelper : TagHelper
    {
        private readonly FitRazorContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHtmlGenerator _htmlGenerator;
        private readonly IModelMetadataProvider _metadataProvider;

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext? ViewContext { get; set; }

        [HtmlAttributeName("entity-name")]
        public string EntityName { get; set; } = "Trainers";

        [HtmlAttributeName("submit-text")]
        public string SubmitText { get; set; } = "Создать";

        [HtmlAttributeName("cancel-page")]
        public string CancelPage { get; set; }

        public EntityCreateTagHelper(
            FitRazorContext context,
            IHttpContextAccessor httpContextAccessor,
            IHtmlGenerator htmlGenerator,
            IModelMetadataProvider metadataProvider)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _htmlGenerator = htmlGenerator;
            _metadataProvider = metadataProvider;
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

            var summary = _htmlGenerator.GenerateValidationSummary(
                ViewContext!,
                excludePropertyErrors: false,
                message: "",
                headerTag: null,
                htmlAttributes: new { @class = "text-danger mb-3" }
            );
            if (summary != null)
            {
                html.Insert(0, GetHtml(summary));
            }

            var entity = Activator.CreateInstance(modelType)!;
            var modelExplorer = _metadataProvider.GetModelExplorerForType(modelType, entity);

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
                    var inputTag = GenerateStandardInput(prop, dropdownData, meta);
                    html.Append(GetHtml(inputTag));
                }

                var validation = _htmlGenerator.GenerateValidationMessage(
                    ViewContext!,
                    modelExplorer.GetExplorerForProperty(prop.Name),
                    prop.Name,
                    message: null,
                    tag: null,
                    htmlAttributes: new { @class = "text-danger small mt-1" }
                );
                html.Append(GetHtml(validation));

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

        private string GetHtml(TagBuilder tag)
        {
            using var writer = new StringWriter();
            tag.WriteTo(writer, HtmlEncoder.Default);
            return writer.ToString();
        }

        private TagBuilder GenerateStandardInput(PropertyInfo prop,
            Dictionary<string, IEnumerable<SelectListItem>> dropdownData, EntityAdminMetadata meta)
        {
            var propName = prop.Name;

            // Фото
            if (meta.PhotoUploadConfigs.TryGetValue(propName, out var photoConfig) ||
                propName.EndsWith("PhotoUrl", StringComparison.OrdinalIgnoreCase) ||
                propName.EndsWith("ImageUrl", StringComparison.OrdinalIgnoreCase) ||
                propName.EndsWith("AvatarUrl", StringComparison.OrdinalIgnoreCase))
            {
                var config = photoConfig ?? new PhotoUploadConfig { Subfolder = "Uploads" };
                var htmlString = EntityAdminRegistry.GeneratePhotoInputForCreate(prop, propName, config);
                var div = new TagBuilder("div");
                div.InnerHtml.AppendHtml(new HtmlString(htmlString));
                return div;
            }

            // FK dropdown
            if (propName.EndsWith("Id") && dropdownData.ContainsKey(propName))
            {
                var select = new TagBuilder("select");
                select.Attributes["name"] = propName;
                select.AddCssClass("form-select");
                select.InnerHtml.AppendHtml("<option value=''>— Выберите —</option>");
                foreach (var opt in dropdownData[propName])
                {
                    var option = new TagBuilder("option");
                    option.Attributes["value"] = opt.Value;
                    option.InnerHtml.Append(opt.Text);
                    select.InnerHtml.AppendHtml(option);
                }
                return select;
            }

            // Прочие типы
            return GenerateTextInput(prop, "", "form-control", "");
        }

        private TagBuilder GenerateTextInput(PropertyInfo prop, string value, string classAttr, string readOnlyAttr)
        {
            var propName = prop.Name;
            var modelMetadata = _metadataProvider.GetMetadataForProperty(prop.DeclaringType!, propName);

            var input = new TagBuilder("input");
            input.Attributes["name"] = propName;
            input.AddCssClass(classAttr);

            // Тип по имени свойства
            if (propName.Contains("Email", StringComparison.OrdinalIgnoreCase))
                input.Attributes["type"] = "email";
            else if (propName.Contains("Phone", StringComparison.OrdinalIgnoreCase))
                input.Attributes["type"] = "tel";
            else
                input.Attributes["type"] = "text";

            input.Attributes["value"] = value;

            if (prop.GetCustomAttribute<RequiredAttribute>() != null)
            {
                input.Attributes["required"] = "required";
                input.Attributes["data-val"] = "true";
                input.Attributes["data-val-required"] = "Это поле обязательно";
            }

            var maxLength = prop.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength ?? 500;
            if (maxLength < 500)
            {
                input.Attributes["maxlength"] = maxLength.ToString();
                input.Attributes["data-val-length-max"] = maxLength.ToString();
            }

            return input;
        }
    }
}