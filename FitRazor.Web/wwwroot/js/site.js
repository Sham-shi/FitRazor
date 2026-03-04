// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener('DOMContentLoaded', function () {
    // ==== Скрипт для переключения видимости пароля ====
    const toggle = document.getElementById('togglePassword');
    const input = document.getElementById('password');
    const eye = document.getElementById('eye');
    const eyeSlash = document.getElementById('eyeSlash');

    if (toggle && input) {
        toggle.addEventListener('click', function () {
            const isPassword = input.type === 'password';
            input.type = isPassword ? 'text' : 'password';
            eye.classList.toggle('d-none', !isPassword);
            eyeSlash.classList.toggle('d-none', isPassword);
        });
    }

    // Intersection Observer для анимации появления элементов
    // ===== Анимация титула для карточек =====
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

    const mainTitle = document.querySelector('.philosophy-main-title');
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