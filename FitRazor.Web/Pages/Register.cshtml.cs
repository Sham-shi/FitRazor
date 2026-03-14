using FitRazor.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

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

        // 🔹 Проверка согласия
        if (!Input.Consent)
        {
            ModelState.AddModelError("Input.Consent", "Необходимо согласиться с правилами сервиса");
            return Page();
        }

        // 🔹 Проверка уникальности логина
        if (await _userManager.FindByNameAsync(Input.Login) != null)
        {
            ModelState.AddModelError("Input.Login", "Пользователь с таким логином уже существует");
            return Page();
        }

        // 🔹 Проверка уникальности телефона (если указан)
        if (!string.IsNullOrEmpty(Input.Phone))
        {
            var normalizedInput = NormalizePhone(Input.Phone);
            var existingClientByPhone = await _context.Clients
                .AnyAsync(c => c.Phone == normalizedInput);

            if (existingClientByPhone)
            {
                ModelState.AddModelError("Input.Phone", "Клиент с таким телефоном уже зарегистрирован");
                return Page();
            }
        }

        // 🔹 Проверка уникальности email (если указан)
        if (!string.IsNullOrEmpty(Input.Email))
        {
            var existingClientByEmail = await _context.Clients
                .AnyAsync(c => c.Email == Input.Email);
            if (existingClientByEmail)
            {
                ModelState.AddModelError("Input.Email", "Клиент с таким email уже зарегистрирован");
                return Page();
            }
        }

        // 🔹 🔥 Начинаем транзакцию для атомарного создания пользователя и профиля
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1️⃣ Создаём ApplicationUser
            var appUser = new ApplicationUser
            {
                UserName = Input.Login,
                FullName = Input.FullName,
                Email = string.IsNullOrWhiteSpace(Input.Email) ? null : Input.Email,
                PhoneNumber = Input.Phone,
                EmailConfirmed = true, // 🔥 Для упрощения (в продакшене — отправлять письмо)
                LastLoginDate = DateTime.Now
            };

            var createUserResult = await _userManager.CreateAsync(appUser, Input.Password);
            if (!createUserResult.Succeeded)
            {
                foreach (var error in createUserResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return Page();
            }

            // 2️⃣ Назначаем роль "Client"
            var addRoleResult = await _userManager.AddToRoleAsync(appUser, "Client");
            if (!addRoleResult.Succeeded)
            {
                // 🔥 Откат: удаляем пользователя, если не удалось добавить роль
                await _userManager.DeleteAsync(appUser);
                ModelState.AddModelError(string.Empty, "Не удалось назначить роль пользователю");
                return Page();
            }

            var normalizedPhone = NormalizePhone(Input.Phone);

            // 3️⃣ Создаём профиль Client с привязкой к ApplicationUser
            var client = new Client
            {
                FullName = Input.FullName,
                Phone = normalizedPhone,
                Email = string.IsNullOrWhiteSpace(Input.Email) ? null : Input.Email,
                BirthDate = Input.BirthDate,
                RegistrationDate = DateOnly.FromDateTime(DateTime.Now),
                ApplicationUserId = appUser.Id // 🔗 Ключевая связь!
            };

            _context.Clients.Add(client);
            await _context.SaveChangesAsync(); // 🔥 Сохраняем, чтобы получить ClientId

            // 4️⃣ Обновляем ApplicationUser с ссылкой на профиль (для обратной навигации)
            appUser.ClientId = client.ClientId;
            var updateResult = await _userManager.UpdateAsync(appUser);
            if (!updateResult.Succeeded)
            {
                // 🔥 Логгируем, но не откатываем — связь не критична для работы
                _logger.LogWarning("Не удалось обновить ApplicationUser.ClientId для пользователя {UserId}", appUser.Id);
            }

            // 5️⃣ Фиксируем транзакцию
            await transaction.CommitAsync();

            _logger.LogInformation("Пользователь {UserName} зарегистрирован с профилем клиента {ClientId}",
                appUser.UserName, client.ClientId);

            // 6️⃣ Автоматический вход
            await _signInManager.SignInAsync(appUser, isPersistent: false);
            _logger.LogInformation("Пользователь {UserName} автоматически вошёл в систему", appUser.UserName);

            // 7️⃣ Перенаправление
            return LocalRedirect(returnUrl ?? "/");
        }
        catch (Exception ex)
        {
            // 🔥 Откат при любой ошибке
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Ошибка при регистрации пользователя {Login}", Input.Login);
            ModelState.AddModelError(string.Empty, "Произошла ошибка при регистрации. Попробуйте позже.");
            return Page();
        }
    }

    private string NormalizePhone(string phone)
    {
        // Оставляем только цифры и + в начале
        var cleaned = new string(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());

        // Если начинается с 8, заменяем на +7
        if (cleaned.StartsWith("8"))
            cleaned = "+7" + cleaned.Substring(1);
        else if (cleaned.StartsWith("7") && !cleaned.StartsWith("+"))
            cleaned = "+" + cleaned;

        return cleaned;
    }
}

public class InputModel
{

    [Required(ErrorMessage = "Имя обязательно")]
    [StringLength(100, ErrorMessage = "ФИО должно быть не длиннее 100 символов")]
    [Display(Name = "ФИО")]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Телефон обязателен")]
    [RegularExpression(
        @"^(\+7|8)\s?\(?\d{3}\)?[\s\-]?\d{3}[\s\-]?\d{2}[\s\-]?\d{2}$|^(\+7|8)\d{10}$",
        ErrorMessage = "Неверный формат телефона. Пример: +7 (999) 123-45-67 или 89991234567")]
    [StringLength(20, MinimumLength = 11, ErrorMessage = "Телефон должен состоять минимум из 11 цифр")]
    [Display(Name = "Телефон")]
    public string Phone { get; set; } = null!;

    [StringLength(100, ErrorMessage = "Email должен быть не длиннее 100 символов")]
    [EmailAddress(ErrorMessage = "Неверный формат email")]
    public string? Email { get; set; }

    [Display(Name = "День рождения")]
    public DateOnly? BirthDate { get; set; }

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