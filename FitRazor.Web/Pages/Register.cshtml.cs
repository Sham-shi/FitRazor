using System.ComponentModel.DataAnnotations;
using FitRazor.Data;
using FitRazor.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FitRazor.Web.Pages;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly FitRazorContext _context;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        FitRazorContext context,
        ILogger<RegisterModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public IActionResult OnGet(string? returnUrl = null)
    {
        // Если пользователь уже авторизован — перенаправляем
        if (User.Identity?.IsAuthenticated == true)
            return LocalRedirect(returnUrl ?? "/");

        ReturnUrl = returnUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        // 🔹 Валидация модели
        if (!ModelState.IsValid)
            return Page();

        // 🔹 Проверка согласия с правилами
        if (!Input.Consent)
        {
            ModelState.AddModelError("Input.Consent", "Необходимо согласиться с правилами сервиса");
            return Page();
        }

        // 🔹 Проверка, что пользователь с таким логином ещё не существует
        var existingUser = await _userManager.FindByNameAsync(Input.Login);
        if (existingUser != null)
        {
            ModelState.AddModelError("Input.Login", "Пользователь с таким логином уже существует");
            return Page();
        }

        // 🔹 Создаём Identity-пользователя
        var user = new ApplicationUser
        {
            UserName = Input.Login,
            FullName = Input.Login, // Можно позже добавить поле для полного имени
            Email = null, // Email не используем
            EmailConfirmed = true // Подтверждаем сразу (т.к. email не требуется)
        };

        var result = await _userManager.CreateAsync(user, Input.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("Пользователь {UserName} зарегистрирован", user.UserName);

            // 🔹 Назначаем роль "Client" по умолчанию
            await _userManager.AddToRoleAsync(user, "Client");

            // 🔹 Создаём запись в доменной таблице Client (связь с Identity)
            //var client = new Client
            //{
            //    Name = Input.Login, // Или добавьте отдельное поле FullName в форму
            //    Phone = string.Empty, // Можно добавить поле в форму
            //    ApplicationUserId = user.Id, // 🔗 Ключевая связь!
            //    RegistrationDate = DateTime.Now
            //};

            //_context.Clients.Add(client);
            //await _context.SaveChangesAsync();

            // 🔹 Автоматический вход после регистрации
            await _signInManager.SignInAsync(user, isPersistent: false);

            _logger.LogInformation("Пользователь {UserName} автоматически вошёл в систему", user.UserName);

            // 🔹 Перенаправление
            return LocalRedirect(returnUrl ?? "/");
        }

        // 🔹 Ошибки создания пользователя
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return Page();
    }
}

public class InputModel
{
    [Required(ErrorMessage = "Введите логин")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Логин должен быть от 3 до 50 символов")]
    [RegularExpression(@"^[a-zA-Z0-9._@+\- ]+$", ErrorMessage = "Логин может содержать только латинские буквы, цифры и символы: . _ @ + -")]
    [Display(Name = "Логин")]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите пароль")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Пароль должен быть не менее 1 символов")]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Подтвердите пароль")]
    [DataType(DataType.Password)]
    [Display(Name = "Подтверждение пароля")]
    [Compare("Password", ErrorMessage = "Пароли не совпадают")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Необходимо согласие с правилами")]
    [Display(Name = "Согласие с правилами")]
    public bool Consent { get; set; }
}