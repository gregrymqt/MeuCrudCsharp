document.addEventListener('DOMContentLoaded', function () {

    // --- 1. Inicialização do Swiper (Carrossel) ---
    const swiper = new Swiper('.swiper', {
        // Opções
        loop: true,
        autoplay: {
            delay: 5000,
            disableOnInteraction: false,
        },
        pagination: {
            el: '.swiper-pagination',
            clickable: true,
        },
        navigation: {
            nextEl: '.swiper-button-next',
            prevEl: '.swiper-button-prev',
        },
    });

    // --- 2. Animação de Opacidade no Scroll ---
    const heroSection = document.querySelector('.hero-section');

    window.addEventListener('scroll', () => {
        // Pega a posição atual do scroll
        const scrollPosition = window.scrollY;
        
        // Pega a altura da seção de herói
        const heroHeight = heroSection.offsetHeight;

        // Calcula a opacidade. Ela será 1 no topo e 0 quando o scroll
        // chegar ao final da seção de herói.
        const opacity = 1 - (scrollPosition / (heroHeight * 0.8));

        // Garante que a opacidade não seja menor que 0
        heroSection.style.opacity = Math.max(0, opacity);
    });

});