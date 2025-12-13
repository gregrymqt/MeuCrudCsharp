export function initializeSidebar(panelLoaders) {
    const sidebarLinks = document.querySelectorAll('.sidebar-link');
    const contentPanels = document.querySelectorAll('.content-panel');
    const mobileHeaderTitle = document.getElementById('mobile-header-title');
    const body = document.body;

    sidebarLinks.forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();

            // 1. Gerencia as classes 'active'
            sidebarLinks.forEach(l => l.classList.remove('active'));
            contentPanels.forEach(p => p.classList.remove('active'));
            link.classList.add('active');

            // 2. Mostra o painel correto
            const panelId = link.id.replace('nav-', 'content-');
            const activePanel = document.getElementById(panelId);

            if (activePanel) {
                activePanel.classList.add('active');

                // 3. Atualiza o título no header mobile (Melhoria de UX)
                if (mobileHeaderTitle) {
                    mobileHeaderTitle.textContent = link.textContent.trim();
                }

                // 4. Carrega os dados do painel se for a primeira vez (Lazy-loading)
                if (!activePanel.dataset.loaded) {
                    const loaderFunction = panelLoaders[link.id];
                    if (loaderFunction) {
                        loaderFunction(); // Ex: Chama loadPlans()
                    }
                    activePanel.dataset.loaded = 'true';
                }
            }

            // 5. Fecha a sidebar no mobile após o clique (Melhoria de UX)
            if (window.innerWidth < 768) {
                body.classList.remove('sidebar-visible');
            }
        });
    });
}
export function initializeMenuToggle() {
    // 1. Seleciona o botão e o corpo da página
    const menuToggleBtn = document.getElementById('menu-toggle');
    const body = document.body;

    // 2. Garante que o botão existe na página antes de continuar
    if (menuToggleBtn) {
        // 3. Adiciona um "ouvinte" de evento de clique
        menuToggleBtn.addEventListener('click', () => {
            // 4. Alterna (adiciona/remove) a classe no body. É isso que faz a mágica! ✨
            body.classList.toggle('sidebar-visible');
        });
    }
}