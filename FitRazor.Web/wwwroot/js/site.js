// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// ===== Скрипт для страницы регистрации =====
//document.addEventListener('DOMContentLoaded', function () {
//    // ==== Переключение видимости пароля (основной) ====
//    const togglePassword = document.getElementById('togglePassword');
//    const passwordInput = document.getElementById('password');
//    const eye = document.getElementById('eye');
//    const eyeSlash = document.getElementById('eyeSlash');

//    if (togglePassword && passwordInput) {
//        togglePassword.addEventListener('click', function () {
//            const isPassword = passwordInput.type === 'password';
//            passwordInput.type = isPassword ? 'text' : 'password';
//            if (eye && eyeSlash) {
//                eye.classList.toggle('d-none', !isPassword);
//                eyeSlash.classList.toggle('d-none', isPassword);
//            }
//        });
//    }

//    // ==== Переключение видимости пароля (подтверждение) ====
//    const toggleConfirmPassword = document.getElementById('toggleConfirmPassword');
//    const confirmPasswordInput = document.getElementById('confirmPassword');
//    const eyeConfirm = document.getElementById('eyeConfirm');
//    const eyeSlashConfirm = document.getElementById('eyeSlashConfirm');

//    if (toggleConfirmPassword && confirmPasswordInput) {
//        toggleConfirmPassword.addEventListener('click', function () {
//            const isPassword = confirmPasswordInput.type === 'password';
//            confirmPasswordInput.type = isPassword ? 'text' : 'password';
//            if (eyeConfirm && eyeSlashConfirm) {
//                eyeConfirm.classList.toggle('d-none', !isPassword);
//                eyeSlashConfirm.classList.toggle('d-none', isPassword);
//            }
//        });
//    }

//    // ==== Валидация совпадения паролей ====
//    const form = document.querySelector('form');
//    if (form && passwordInput && confirmPasswordInput) {
//        form.addEventListener('submit', function (e) {
//            if (passwordInput.value !== confirmPasswordInput.value) {
//                e.preventDefault();
//                alert('Пароли не совпадают!');
//                confirmPasswordInput.classList.add('is-invalid');
//            }
//        });

//        // Убираем ошибку при вводе
//        confirmPasswordInput.addEventListener('input', function () {
//            if (passwordInput.value === confirmPasswordInput.value) {
//                confirmPasswordInput.classList.remove('is-invalid');
//                confirmPasswordInput.classList.add('is-valid');
//            } else {
//                confirmPasswordInput.classList.remove('is-valid');
//            }
//        });
//    }

//    // ==== Анимация появления формы ====
//    const formDark = document.querySelector('.form-dark');
//    if (formDark) {
//        const formObserver = new IntersectionObserver((entries) => {
//            entries.forEach(entry => {
//                if (entry.isIntersecting) {
//                    entry.target.classList.add('active');
//                    formObserver.unobserve(entry.target);
//                }
//            });
//        }, { threshold: 0.1 });

//        formObserver.observe(formDark);
//    }
//});


document.addEventListener('DOMContentLoaded', function () {
    // ==== Скрипт для переключения видимости пароля ====
    const togglePassword = document.getElementById('togglePassword');
    const inputPassword = document.getElementById('password');
    const eye = document.getElementById('eye');
    const eyeSlash = document.getElementById('eyeSlash');

    if (togglePassword && inputPassword) {
        togglePassword.addEventListener('click', function () {
            const isPassword = inputPassword.type === 'password';
            inputPassword.type = isPassword ? 'text' : 'password';
            eye.classList.toggle('d-none', !isPassword);
            eyeSlash.classList.toggle('d-none', isPassword);
        });
    }

    const toogleRegPassword = document.getElementById('toogleRegPassword');
    const inputRegPassword = document.getElementById('regPassword');
    const regEye = document.getElementById('regEye');
    const regEyeSlash = document.getElementById('regEyeSlash');

    if (toogleRegPassword && inputRegPassword) {
        toogleRegPassword.addEventListener('click', function () {
            const isRegPassword = inputRegPassword.type === 'password';
            inputRegPassword.type = isRegPassword ? 'text' : 'password';
            regEye.classList.toggle('d-none', !isRegPassword);
            regEyeSlash.classList.toggle('d-none', isRegPassword);
        });
    }

    const toogleRegConfirmPassword = document.getElementById('toogleRegConfirmPassword');
    const inputRegConfirmPassword = document.getElementById('regConfirmPassword');
    const regConfirmEye = document.getElementById('regConfirmEye');
    const regConfirmEyeSlash = document.getElementById('regConfirmEyeSlash');

    if (toogleRegConfirmPassword && inputRegConfirmPassword) {
        toogleRegConfirmPassword.addEventListener('click', function () {
            const isRegConfirmPassword = inputRegConfirmPassword.type === 'password';
            inputRegConfirmPassword.type = isRegConfirmPassword ? 'text' : 'password';
            regConfirmEye.classList.toggle('d-none', !isRegConfirmPassword);
            regConfirmEyeSlash.classList.toggle('d-none', isRegConfirmPassword);
        });
    }

    // ===== Анимация заголовков =====
    const mainTitleObserver = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('active');
                mainTitleObserver.unobserve(entry.target); // Отключаем после срабатывания
            }
        });
    }, {
        threshold: 0.1,              // Достаточно 10% видимости
        rootMargin: '0px 0px -50px 0px' // Менее агрессивный отступ
    });

    const mainTitle = document.querySelector('.reveal-up');
    if (mainTitle) {
        mainTitleObserver.observe(mainTitle);
    }

    // ===== Анимация карточек =====
    const cardObserver = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('active');
                cardObserver.unobserve(entry.target); // Отключаем после срабатывания
            }
        });
    }, {
        threshold: 0.1,              // Достаточно 10% видимости
        rootMargin: '0px 0px -50px 0px' // Менее агрессивный отступ
    });

    document.querySelectorAll('.reveal-left, .reveal-right').forEach(card => {
        cardObserver.observe(card);
    });

    // ===== Анимация заголовка для описания зон =====
    const titleObserver = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('visible');
                titleObserver.unobserve(entry.target);
            }
        });
    }, {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    });

    const bottomTitle = document.querySelector('.zones-title');
    if (bottomTitle) {
        titleObserver.observe(bottomTitle);
    }
});