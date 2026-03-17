using System.ComponentModel.DataAnnotations;

namespace FitRazor.Endpoints.Dtos;

// DTO для создания тренера (только нужные поля)
public class CreateTrainerDto
{
    [Required(ErrorMessage = "ФИО обязательно")]
    [StringLength(100)]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Телефон обязателен")]
    [StringLength(20)]
    public string Phone { get; set; } = null!;

    [Required(ErrorMessage = "Email обязателен")]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = null!;

    [StringLength(100)]
    public string? Slogan { get; set; }

    [Required(ErrorMessage = "Специализация обязательна")]
    [StringLength(100)]
    public string Specialization { get; set; } = null!;

    [StringLength(1000)]
    public string? SpecializationDescription { get; set; }

    [StringLength(500)]
    public string? Motto { get; set; }

    [Required(ErrorMessage = "Образование обязательно")]
    [StringLength(500)]
    public string Education { get; set; } = null!;

    [Required(ErrorMessage = "Опыт работы обязателен")]
    [StringLength(1000)]
    public string WorkExperience { get; set; } = null!;

    [Required(ErrorMessage = "Достижения обязательны")]
    [StringLength(1000)]
    public string SportsAchievements { get; set; } = null!;

    [StringLength(500)]
    public string? PhotoUrl { get; set; }
}

// DTO для обновления (все поля опциональны, кроме ключа)
public class UpdateTrainerDto
{
    [Required]
    public int TrainerId { get; set; }

    [StringLength(100)]
    public string? FullName { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [EmailAddress]
    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(100)]
    public string? Slogan { get; set; }

    [StringLength(100)]
    public string? Specialization { get; set; }

    [StringLength(1000)]
    public string? SpecializationDescription { get; set; }

    [StringLength(500)]
    public string? Motto { get; set; }

    [StringLength(500)]
    public string? Education { get; set; }

    [StringLength(1000)]
    public string? WorkExperience { get; set; }

    [StringLength(1000)]
    public string? SportsAchievements { get; set; }

    [StringLength(500)]
    public string? PhotoUrl { get; set; }
}

// DTO для ответа (исключаем чувствительные поля)
public class TrainerResponseDto
{
    public int TrainerId { get; set; }
    public string FullName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Slogan { get; set; }
    public string Specialization { get; set; } = null!;
    public string? SpecializationDescription { get; set; }
    public string? Motto { get; set; }
    public string Education { get; set; } = null!;
    public string WorkExperience { get; set; } = null!;
    public string SportsAchievements { get; set; } = null!;
    public string? PhotoUrl { get; set; }
}