export function initializeSidebarNavigation() {
    const menuToggleBtn = document.getElementById('menu-toggle-btn');
    const sidebar = document.querySelector('.admin-sidebar'); // Seletor corrigido
    const body = document.body;

    if (!menuToggleBtn || !sidebar) {
        console.error("Elementos da sidebar não encontrados.");
        return;
    }

    // --- LÓGICA PARA ABRIR/FECHAR A SIDEBAR ---

    // 1. Abrir/Fechar com o clique no botão
    menuToggleBtn.addEventListener('click', (e) => {
        e.stopPropagation(); // Impede que o clique se propague para outros elementos
        body.classList.toggle('sidebar-visible');
    });

    // 2. Fechar ao clicar FORA da sidebar (no overlay)
    document.addEventListener('click', (e) => {
        // Se a sidebar está visível E o clique não foi nela ou em algo dentro dela
        if (body.classList.contains('sidebar-visible') && !sidebar.contains(e.target)) {
            body.classList.remove('sidebar-visible');
        }
    });


    // --- LÓGICA DE NAVEGAÇÃO INTERNA DA SIDEBAR (SEU CÓDIGO ORIGINAL) ---
    const sidebarLinks = document.querySelectorAll('.profile-nav-link');
    const contentSections = document.querySelectorAll('.main-section');
    const mobileHeaderTitle = document.getElementById('mobile-header-title');

    sidebarLinks.forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();
            const targetId = link.dataset.target;
            if (!targetId) return;

            sidebarLinks.forEach(l => l.classList.remove('active'));
            contentSections.forEach(s => s.classList.remove('active'));

            link.classList.add('active');
            document.getElementById(targetId)?.classList.add('active');

            if (mobileHeaderTitle && link.querySelector('span')) {
                mobileHeaderTitle.textContent = link.querySelector('span').textContent;
            }

            // Fecha a sidebar no mobile após o clique (breakpoint corrigido)
            if (window.innerWidth < 768) {
                body.classList.remove('sidebar-visible');
            }
        });
    });

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