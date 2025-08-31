export function initializeSidebarNavigation() {
    const menuToggleBtn = document.getElementById('menu-toggle-btn');
    const sidebarLinks = document.querySelectorAll('.profile-nav-link');
    const contentSections = document.querySelectorAll('.main-section');
    const mobileHeaderTitle = document.getElementById('mobile-header-title');

    if (!menuToggleBtn || sidebarLinks.length === 0) return;

    // Abrir/fechar sidebar no mobile
    menuToggleBtn.addEventListener('click', () => {
        document.body.classList.toggle('sidebar-visible');
    });

    // Navegar entre as seções
    sidebarLinks.forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();

            const targetId = link.dataset.target;
            if (!targetId) return; // Ignora links sem 'data-target' como o de logout

            // Atualiza links e seções
            sidebarLinks.forEach(l => l.classList.remove('active'));
            contentSections.forEach(s => s.classList.remove('active'));

            link.classList.add('active');
            document.getElementById(targetId)?.classList.add('active');

            // Atualiza título mobile
            if (mobileHeaderTitle && link.querySelector('span')) {
                mobileHeaderTitle.textContent = link.querySelector('span').textContent;
            }

            // Fecha sidebar no mobile após o clique
            if (window.innerWidth < 992) {
                document.body.classList.remove('sidebar-visible');
            }
        });
    });

    // Ativa a primeira seção por padrão
    if (sidebarLinks.length > 0) {
        sidebarLinks[0].click();
    }
}

/**
 * Função para inicializar todos os componentes de acordeão na página.
 */
export function initializeAccordions() {
    const accordionItems = document.querySelectorAll('.accordion-item');

    accordionItems.forEach(item => {
        const header = item.querySelector('.accordion-header');
        const body = item.querySelector('.accordion-body');

        if (!header || !body) return;

        header.addEventListener('click', () => {
            // Fecha outros acordeões abertos para evitar poluição visual
            accordionItems.forEach(otherItem => {
                if (otherItem !== item) {
                    otherItem.querySelector('.accordion-header').classList.remove('active');
                    otherItem.querySelector('.accordion-body').style.maxHeight = null;
                }
            });

            // Abre ou fecha o acordeão clicado
            header.classList.toggle('active');
            if (header.classList.contains('active')) {
                body.style.maxHeight = body.scrollHeight + "px";
            } else {
                body.style.maxHeight = null;
            }
        });
    });
}