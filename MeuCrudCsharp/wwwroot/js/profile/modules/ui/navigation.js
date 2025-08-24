// Função para navegação principal por abas (código original, sem alterações)
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

// === CÓDIGO ADICIONADO ===
// Função para controlar a UI do Accordion (sanfona)
export function initializeAccordion() {
    const accordionHeaders = document.querySelectorAll('.accordion-header');

    accordionHeaders.forEach(header => {
        header.addEventListener('click', () => {
            const accordionItem = header.parentElement;
            
            // Simplesmente alterna a classe 'active'. O CSS cuida de mostrar/esconder.
            accordionItem.classList.toggle('active');
        });
    });
}