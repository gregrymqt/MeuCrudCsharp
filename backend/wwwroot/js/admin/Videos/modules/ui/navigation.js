export function initializeNavigation() {
    // --- 1. Seletores dos Elementos de UI ---
    const menuToggleBtn = document.getElementById('menu-toggle-btn');
    const sidebarLinks = document.querySelectorAll('.sidebar-link');
    const contentPanels = document.querySelectorAll('.content-panel');
    const mobileHeaderTitle = document.getElementById('mobile-header-title');

    // Validação para garantir que os elementos essenciais existem
    if (!menuToggleBtn || sidebarLinks.length === 0 || contentPanels.length === 0) {
        console.error('Não foi possível inicializar a navegação: elementos essenciais não encontrados.');
        return;
    }

    // --- 2. Lógica de Eventos ---

    // Evento para abrir/fechar a sidebar no mobile
    menuToggleBtn.addEventListener('click', () => {
        document.body.classList.toggle('sidebar-visible');
    });

    // Evento para navegar entre os painéis ao clicar nos links
    sidebarLinks.forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();

            const targetId = link.dataset.target;
            const targetPanel = document.getElementById(targetId);

            // Atualiza os links da sidebar para marcar o ativo
            sidebarLinks.forEach(l => l.classList.remove('active'));
            link.classList.add('active');

            // Mostra o painel de conteúdo correspondente
            contentPanels.forEach(panel => panel.classList.remove('active'));
            if (targetPanel) {
                targetPanel.classList.add('active');
            }

            // Atualiza o título no cabeçalho mobile
            if (mobileHeaderTitle && link.querySelector('span')) {
                mobileHeaderTitle.textContent = link.querySelector('span').textContent;
            }

            // (UX) Fecha a sidebar após clicar em um link no mobile
            if (window.innerWidth < 768) {
                document.body.classList.remove('sidebar-visible');
            }
        });
    });

    // --- 3. Estado Inicial ---
    // Ativa o primeiro painel como padrão ao carregar a página
    if (sidebarLinks.length > 0) {
        sidebarLinks[0].click();
    }
}
