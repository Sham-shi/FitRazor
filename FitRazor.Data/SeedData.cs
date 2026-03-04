using FitRazor.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace FitRazor.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(FitRazorContext context)
        {
            // EnsureCreated создает БД и таблицы, если их нет
            // Это альтернатива миграциям для простых сценариев
            context.Database.EnsureCreated();

            // Проверяем, есть ли уже данные (чтобы не дублировать при каждом запуске)
            if (!await context.Trainers.AnyAsync())
            {
                // Добавляем тестовые данные
                var trainers = new List<Trainer>
                {
                    new Trainer
                    {
                        FullName = "Максим «Скала»",
                        Phone = "+7 (999) 100-01-01",
                        Email = "maksim.skala@fitnesscenter.ru",
                        Slogan = "Сила. Основа. Контроль.",
                        Specialization = "Пауэрлифтинг",
                        SpecializationDescription = "Фундаментальная сила. Постановка техники в базе (жим, присед, тяга) и построение силы, которая останется с тобой навсегда. Его атлеты не боятся весов.",
                        Motto = "«Железо не лжёт. Или ты можешь, или нет. Я научу тебя МОЧЬ.»",
                        Education = "РГУФКСМиТ, факультет силовых видов спорта. Сертификат IPF Level 2.",
                        WorkExperience = "5 лет тренерского стажа.",
                        SportsAchievements = "КМС по пауэрлифтингу. Рекордсмен клуба по становой тяге.",
                        Salary = 85000.00m,
                        PhotoUrl = "/Images/Trainers/trainer-maxim.jpg"
                    },
                    new Trainer
                    {
                        FullName = "Кирилл «Движ»",
                        Phone = "+7 (999) 100-02-02",
                        Email = "kirill.dvizh@fitnesscenter.ru",
                        Slogan = "Выносливость. Взрыв. Адреналин.",
                        Specialization = "Функциональный тренинг и HIIT",
                        SpecializationDescription = "Выносливость, взрыв, адреналин. Специалист по высокоинтенсивным интервальным тренировкам (HIIT), работе с собственным весом и функциональному развитию.",
                        Motto = "«Усталость начинается в голове. Тело может больше. Заставь его работать.»",
                        Education = "МГПУ, кафедра физической культуры. Сертификат NASM HIIT Specialist.",
                        WorkExperience = "4 года тренерского стажа.",
                        SportsAchievements = "МС по лёгкой атлетике (бег на 400м). Победитель этапа Кубка Москвы.",
                        Salary = 75000.00m,
                        PhotoUrl = "/Images/Trainers/trainer-kirill.png"
                    },
                    new Trainer
                    {
                        FullName = "Артем «Тесла»",
                        Phone = "+7 (999) 100-03-03",
                        Email = "artem.tesla@fitnesscenter.ru",
                        Slogan = "Деталь. Объём. Эстетика.",
                        Specialization = "Бодибилдинг",
                        SpecializationDescription = "Деталь, объём, эстетика. Эксперт в построении мышечной массы, работе на рельеф и изоляции мышц. Он знает, как «добить» каждую мышечную группу для идеальной формы.",
                        Motto = "«Каждая капля пота — это кирпичик в скульптуре твоего тела. Будь архитектором себя.»",
                        Education = "Колледж бодибилдинга и фитнеса. Курсы по спортивной диетологии.",
                        WorkExperience = "7 лет в спорте.",
                        SportsAchievements = "Призёр чемпионата России по бодибилдингу (категория до 80 кг).",
                        Salary = 90000.00m,
                        PhotoUrl = "/Images/Trainers/trainer-artem.jpg"
                    },
                    new Trainer
                    {
                        FullName = "Анна «Сталь»",
                        Phone = "+7 (999) 100-04-04",
                        Email = "anna.stal@fitnesscenter.ru",
                        Slogan = "Мощь. Техника. Воля.",
                        Specialization = "Тяжёлая атлетика",
                        SpecializationDescription = "Мощь, техника, воля. Специализация: постановка техники в базовых движениях (присед, тяга, жим, рывок, толчок) и развитие прикладной силы. Особый фокус — тренировки для женщин.",
                        Motto = "«Сила не имеет пола. Имеет только результат. Покажу, как поднимать серьёзное железо правильно и без страха.»",
                        Education = "РГУФКСМиТ, тяжёлая атлетика. Сертификат FPA (Персональный тренер).",
                        WorkExperience = "6 лет тренерского стажа.",
                        SportsAchievements = "КМС по тяжёлой атлетике. Призёр чемпионата ЦФО.",
                        Salary = 80000.00m,
                        PhotoUrl = "/Images/Trainers/trainer-anna.jpg"
                    },
                    new Trainer
                    {
                        FullName = "Алиса «Феникс»",
                        Phone = "+7 (999) 100-05-05",
                        Email = "alisa.fenix@fitnesscenter.ru",
                        Slogan = "Дисциплина. Цель. Преображение.",
                        Specialization = "Трансформация тела и нутрициология",
                        SpecializationDescription = "Дисциплина, цель, преображение. Эксперт по экстремальной «сушке», работе на рельеф, ментальной подготовке и соревновательной диетологии.",
                        Motto = "«Самый сложный поединок — с самим собой. Я прошла этот путь от нуля до пьедестала. Проведу и тебя.»",
                        Education = "Курс нутрициологии (School of Nutrition). Сертификат по фитнес-бикини.",
                        WorkExperience = "3 года тренерского стажа.",
                        SportsAchievements = "Чемпионка региона по фитнес-бикини. Победительница Moscow Cup 2023.",
                        Salary = 95000.00m,
                        PhotoUrl = "/Images/Trainers/trainer-alisa.jpg"
                    }
                };

                // Добавляем данные и сохраняем
                await context.Trainers.AddRangeAsync(trainers);
                await context.SaveChangesAsync();
            }

            if (!await context.Clients.AnyAsync())
            {
                var clients = new List<Client>
                {
                    new Client
                    {
                        FullName = "Иванов Дмитрий Александрович",
                        Phone = "+7 (900) 111-22-33",
                        Email = "dmitry.ivanov@mail.ru",
                        BirthDate = DateOnly.Parse("1990-05-15"), // или new DateOnly(1990, 5, 15)
                        RegistrationDate = DateOnly.Parse("2025-01-10")
                    },
                    new Client
                    {
                        FullName = "Петрова Елена Сергеевна",
                        Phone = "+7 (900) 222-33-44",
                        Email = "elena.petrova@gmail.com",
                        BirthDate = DateOnly.Parse("1988-11-22"),
                        RegistrationDate = DateOnly.Parse("2025-02-05")
                    },
                    new Client
                    {
                        FullName = "Смирнов Алексей Игоревич",
                        Phone = "+7 (900) 333-44-55",
                        Email = "alexey.smirnov@yandex.ru",
                        BirthDate = DateOnly.Parse("1995-03-08"),
                        RegistrationDate = DateOnly.Parse("2025-03-12")
                    },
                    new Client
                    {
                        FullName = "Козлова Мария Владимировна",
                        Phone = "+7 (900) 444-55-66",
                        Email = "maria.kozlova@mail.ru",
                        BirthDate = DateOnly.Parse("1992-07-30"),
                        RegistrationDate = DateOnly.Parse("2025-04-20")
                    },
                    new Client
                    {
                        FullName = "Новиков Павел Андреевич",
                        Phone = "+7 (900) 555-66-77",
                        Email = "pavel.novikov@gmail.com",
                        BirthDate = DateOnly.Parse("1985-12-01"),
                        RegistrationDate = DateOnly.Parse("2025-05-15")
                    }
                };

                await context.Clients.AddRangeAsync(clients);
                await context.SaveChangesAsync();
            }

            if (!await context.Services.AnyAsync())
            {
                var services = new List<Service>
                {
                    new Service
                    {
                        ServiceName = "Персональная тренировка по пауэрлифтингу",
                        DurationMinutes = 60,
                        BasePrice = 2500.00m,
                        Description = "Индивидуальная работа над техникой в базовых упражнениях: жим лёжа, приседания, становая тяга. Подходит для всех уровней подготовки."
                    },
                    new Service
                    {
                        ServiceName = "Групповая тренировка HIIT",
                        DurationMinutes = 45,
                        BasePrice = 800.00m,
                        Description = "Высокоинтенсивная интервальная тренировка для развития выносливости и сжигания калорий. Максимум энергии за минимальное время."
                    },
                    new Service
                    {
                        ServiceName = "Персональная тренировка по бодибилдингу",
                        DurationMinutes = 75,
                        BasePrice = 3000.00m,
                        Description = "Индивидуальная программа по построению мышечной массы, работе на рельеф и изоляции мышечных групп."
                    },
                    new Service
                    {
                        ServiceName = "Персональная тренировка по тяжёлой атлетике",
                        DurationMinutes = 60,
                        BasePrice = 2700.00m,
                        Description = "Постановка техники в рывке, толчке, приседе и тяге. Развитие взрывной силы и координации."
                    },
                    new Service
                    {
                        ServiceName = "Программа трансформации тела",
                        DurationMinutes = 90,
                        BasePrice = 3500.00m,
                        Description = "Комплексный подход: тренировка + нутрициология. Индивидуальный план питания и тренировок для максимальных результатов."
                    },
                    new Service
                    {
                        ServiceName = "Консультация нутрициолога",
                        DurationMinutes = 60,
                        BasePrice = 2000.00m,
                        Description = "Разработка индивидуального плана питания, расчёт КБЖУ, рекомендации по спортивным добавкам."
                    },
                    new Service
                    {
                        ServiceName = "Групповая тренировка по функциональному тренингу",
                        DurationMinutes = 50,
                        BasePrice = 700.00m,
                        Description = "Развитие силы, выносливости, координации и гибкости через функциональные движения."
                    }
                };

                await context.Services.AddRangeAsync(services);
                await context.SaveChangesAsync();
            }

            if (!await context.TrainerServices.AnyAsync())
            {
                var trainerServices = new List<TrainerService>
                {
                    // Максим (1) — Пауэрлифтинг (1)
                    new TrainerService { TrainerId = 1, ServiceId = 1 },

                    // Кирилл (2) — HIIT (2), Функциональный тренинг (7)
                    new TrainerService { TrainerId = 2, ServiceId = 2 },
                    new TrainerService { TrainerId = 2, ServiceId = 7 },

                    // Артем (3) — Бодибилдинг (3)
                    new TrainerService { TrainerId = 3, ServiceId = 3 },

                    // Анна (4) — Тяжёлая атлетика (4)
                    new TrainerService { TrainerId = 4, ServiceId = 4 },

                    // Алиса (5) — Трансформация (5), Консультация нутрициолога (6)
                    new TrainerService { TrainerId = 5, ServiceId = 5 },
                    new TrainerService { TrainerId = 5, ServiceId = 6 }
                };

                await context.TrainerServices.AddRangeAsync(trainerServices);
                await context.SaveChangesAsync();
            }

            if (!await context.Bookings.AnyAsync())
            {
                var bookings = new List<Booking>
                {
                    // Запись 1: Дмитрий на пауэрлифтинг к Максиму (TrainerServiceID = 1)
                    new Booking
                    {
                        ClientId = 1,
                        TrainerServiceId = 1,
                        BookingDateTime = DateTime.Parse("2025-08-05 10:00:00"),
                        SessionsCount = 10,
                        UnitPrice = 2500.00m,
                        TotalPrice = null, // Можно рассчитать как UnitPrice * SessionsCount при необходимости
                        Status = "Запланировано",
                        Notes = "Начинающий, акцент на технику приседа",
                        CreatedDate = DateTime.Parse("2025-08-01 14:20:00")
                    },
                    // Запись 2: Елена на трансформацию к Алисе (TrainerServiceID = 6)
                    new Booking
                    {
                        ClientId = 2,
                        TrainerServiceId = 6,
                        BookingDateTime = DateTime.Parse("2025-08-06 18:00:00"),
                        SessionsCount = 12,
                        UnitPrice = 3500.00m,
                        TotalPrice = null,
                        Status = "Запланировано",
                        Notes = "Подготовка к лету, нужна сушка",
                        CreatedDate = DateTime.Parse("2025-08-01 15:30:00")
                    },
                    // Запись 3: Алексей на бодибилдинг к Артему (TrainerServiceID = 3)
                    new Booking
                    {
                        ClientId = 3,
                        TrainerServiceId = 3,
                        BookingDateTime = DateTime.Parse("2025-08-07 19:00:00"),
                        SessionsCount = 8,
                        UnitPrice = 3000.00m,
                        TotalPrice = null,
                        Status = "Запланировано",
                        Notes = "Набор массы, опыт 2 года",
                        CreatedDate = DateTime.Parse("2025-08-01 16:45:00")
                    },
                    // Запись 4: Мария на тяжёлую атлетику к Анне (TrainerServiceID = 4)
                    new Booking
                    {
                        ClientId = 4,
                        TrainerServiceId = 4,
                        BookingDateTime = DateTime.Parse("2025-08-08 17:00:00"),
                        SessionsCount = 6,
                        UnitPrice = 2700.00m,
                        TotalPrice = null,
                        Status = "Запланировано",
                        Notes = "Новичок, хочет развить силу",
                        CreatedDate = DateTime.Parse("2025-08-01 17:10:00")
                    },
                    // Запись 5: Павел на HIIT к Кириллу (TrainerServiceID = 2)
                    new Booking
                    {
                        ClientId = 5,
                        TrainerServiceId = 2,
                        BookingDateTime = DateTime.Parse("2025-08-09 08:00:00"),
                        SessionsCount = 15,
                        UnitPrice = 800.00m,
                        TotalPrice = null,
                        Status = "Запланировано",
                        Notes = "Снижение веса, высокая интенсивность",
                        CreatedDate = DateTime.Parse("2025-08-01 18:00:00")
                    },
                    // Запись 6: Дмитрий на консультацию к Алисе (TrainerServiceID = 7)
                    new Booking
                    {
                        ClientId = 1,
                        TrainerServiceId = 7,
                        BookingDateTime = DateTime.Parse("2025-08-10 12:00:00"),
                        SessionsCount = 1,
                        UnitPrice = 2000.00m,
                        TotalPrice = null,
                        Status = "Завершено",
                        Notes = "Расчёт КБЖУ для набора массы",
                        CreatedDate = DateTime.Parse("2025-08-02 09:15:00")
                    },
                    // Запись 7: Елена на функциональный тренинг к Кириллу (TrainerServiceID = 3)
                    new Booking
                    {
                        ClientId = 2,
                        TrainerServiceId = 3,
                        BookingDateTime = DateTime.Parse("2025-08-11 18:30:00"),
                        SessionsCount = 8,
                        UnitPrice = 700.00m,
                        TotalPrice = null,
                        Status = "Завершено",
                        Notes = "Общее развитие выносливости",
                        CreatedDate = DateTime.Parse("2025-08-02 10:30:00")
                    },
                    // Запись 8: Алексей на пауэрлифтинг к Максиму (TrainerServiceID = 1)
                    new Booking
                    {
                        ClientId = 3,
                        TrainerServiceId = 1,
                        BookingDateTime = DateTime.Parse("2025-08-12 20:00:00"),
                        SessionsCount = 5,
                        UnitPrice = 2500.00m,
                        TotalPrice = null,
                        Status = "Отменено",
                        Notes = "Травма плеча, перенос на сентябрь",
                        CreatedDate = DateTime.Parse("2025-08-02 11:45:00")
                    },
                    // Запись 9: Мария на трансформацию к Алисе (TrainerServiceID = 6)
                    new Booking
                    {
                        ClientId = 4,
                        TrainerServiceId = 6,
                        BookingDateTime = DateTime.Parse("2025-08-13 16:00:00"),
                        SessionsCount = 10,
                        UnitPrice = 3500.00m,
                        TotalPrice = null,
                        Status = "Запланировано",
                        Notes = "Комплексная программа 3 месяца",
                        CreatedDate = DateTime.Parse("2025-08-02 13:20:00")
                    },
                    // Запись 10: Павел на бодибилдинг к Артему (TrainerServiceID = 4)
                    new Booking
                    {
                        ClientId = 5,
                        TrainerServiceId = 4,
                        BookingDateTime = DateTime.Parse("2025-08-14 19:30:00"),
                        SessionsCount = 6,
                        UnitPrice = 3000.00m,
                        TotalPrice = null,
                        Status = "Перенесено",
                        Notes = "Рабочая командировка, новая дата уточняется",
                        CreatedDate = DateTime.Parse("2025-08-02 14:50:00")
                    }
                };

                await context.Bookings.AddRangeAsync(bookings);
                await context.SaveChangesAsync();
            }
        }
    }
}
