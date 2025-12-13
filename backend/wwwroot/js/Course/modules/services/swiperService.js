// /js/modules/services/swiperService.js

let swiperInstance;

/**
 * Inicializa ou reinicializa a instância do carrossel Swiper.js.
 */
export function initializeCarousel() {
    if (swiperInstance) {
        swiperInstance.destroy(true, true);
    }

    swiperInstance = new Swiper('.swiper', {
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
        effect: 'fade',
        fadeEffect: {
            crossFade: true
        },
        grabCursor: true,
    });
}