using FitRazor.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace FitRazor.Web.Pages.Account;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<LoginModel> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IList<AuthenticationScheme>? ExternalLogins { get; set; }

    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Введите логин")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Логин должен быть от 3 до 50 символов")]
        [Display(Name = "Логин")]
        public string Login { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введите пароль")]
        [DataType(DataType.Password)]
        [Display(Name = "Пароль")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Запомнить меня")]
        public bool RememberMe { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        // Если пользователь уже авторизован — редирект по роли
        if (User.Identity?.IsAuthenticated == true)
            return LocalRedirect(returnUrl ?? "/");

        if (!string.IsNullOrEmpty(ErrorMessage))
            ModelState.AddModelError(string.Empty, ErrorMessage);

        ReturnUrl = returnUrl;

        // External logins (если понадобятся позже)
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        if (!ModelState.IsValid)
            return Page();

        // 🔹 Поиск пользователя по логину (UserName)
        var user = await _userManager.FindByNameAsync(Input.Login);

        if (user == null)
        {
            // 🔐 Не указываем конкретно, что не так — безопасность
            ModelState.AddModelError(string.Empty, "Неверный логин или пароль");
            _logger.LogWarning("Попытка входа с несуществующим логином: {Login}", Input.Login);
            return Page();
        }

        // 🔹 Проверка пароля
        var result = await _signInManager.PasswordSignInAsync(
            user,
            Input.Password,
            Input.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            _logger.LogInformation("Пользователь {UserName} успешно вошёл в систему", user.UserName);

            // 🔹 Обновляем дату последнего входа
            user.LastLoginDate = DateTime.Now;
            await _userManager.UpdateAsync(user);

            // 🔹 Редирект по роли
            return LocalRedirect(ReturnUrl ?? "/");
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("Аккаунт {UserName} заблокирован", user.UserName);
            ModelState.AddModelError(string.Empty, "Аккаунт временно заблокирован. Попробуйте позже.");
            return Page();
        }

        if (result.IsNotAllowed)
        {
            _logger.LogWarning("Вход для {UserName} запрещён", user.UserName);
            ModelState.AddModelError(string.Empty, "Вход в аккаунт запрещён");
            return Page();
        }

        // 🔹 Неверный пароль
        _logger.LogWarning("Неверная попытка входа для пользователя {UserName}", user.UserName);
        ModelState.AddModelError(string.Empty, "Неверный логин или пароль");
        return Page();
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("Пользователь вышел из системы");
        return RedirectToPage("/Index");
    }
}