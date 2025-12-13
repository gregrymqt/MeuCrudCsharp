/**
 * @file Initializes interactive elements on the main page.
 *
 * This script handles the setup for:
 * 1. The Swiper.js carousel in the hero section.
 * 2. A scroll-based opacity animation that fades out the hero section as the user scrolls down.
 */
document.addEventListener('DOMContentLoaded', function() {
    // --- 1. Swiper (Carousel) Initialization ---
    // Initializes the main carousel with options for looping, autoplay, and navigation controls.
    const swiper = new Swiper('.swiper', {
        // Options
        loop: true, // Enables continuous looping of slides.
        autoplay: {
            delay: 5000, // Time between slide transitions in milliseconds.
            disableOnInteraction: false, // Autoplay will not be disabled after user interactions.
        },
        pagination: {
            el: '.swiper-pagination', // The element for pagination bullets.
            clickable: true, // Allows clicking on pagination bullets to switch slides.
        },
        navigation: {
            nextEl: '.swiper-button-next', // The element for the "next" button.
            prevEl: '.swiper-button-prev', // The element for the "previous" button.
        },
    });

    // --- 2. Scroll-based Opacity Animation ---
    const heroSection = document.querySelector('.hero-section');

    // Only add the scroll listener if the hero section exists on the page.
    if (!heroSection) return;

    window.addEventListener('scroll', () => {
        // Get the current vertical scroll position.
        const scrollPosition = window.scrollY;
        const heroHeight = heroSection.offsetHeight;

        // Calculate the opacity. It will be 1 at the top and fade to 0 as the user
        // scrolls down 80% of the hero section's height.
        const opacity = 1 - (scrollPosition / (heroHeight * 0.8));

        // Apply the calculated opacity, ensuring it doesn't go below 0.
        heroSection.style.opacity = Math.max(0, opacity);
    });
});