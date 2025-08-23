// /js/admin/modules/ui/navigation.js

export function initializeSidebar(panelLoaders) {
    const sidebarLinks = document.querySelectorAll('.sidebar-link');
    const contentPanels = document.querySelectorAll('.content-panel');

    sidebarLinks.forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();

            sidebarLinks.forEach(l => l.classList.remove('active'));
            contentPanels.forEach(c => c.classList.remove('active'));

            link.classList.add('active');
            const contentId = link.id.replace('nav-', 'content-');
            const activePanel = document.getElementById(contentId);

            if (activePanel) {
                activePanel.classList.add('active');
                // Se o painel ainda não foi carregado, chama sua função de carregamento
                if (!activePanel.dataset.loaded) {
                    const loaderFunction = panelLoaders[link.id];
                    if (loaderFunction) {
                        loaderFunction();
                    }
                    activePanel.dataset.loaded = 'true';
                }
            }
        });
    });
}