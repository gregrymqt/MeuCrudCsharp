// /js/modules/ui/navigation.js

export function initializeTabNavigation() {
    const navLinks = document.querySelectorAll('.profile-nav-link');
    const contentSections = document.querySelectorAll('.main-section');

    navLinks.forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();
            const targetId = link.getAttribute('data-target');
            if (!targetId) return;

            navLinks.forEach(nav => nav.classList.remove('active'));
            link.classList.add('active');

            contentSections.forEach(section => section.classList.remove('active'));
            document.querySelector(targetId).classList.add('active');
        });
    });
}