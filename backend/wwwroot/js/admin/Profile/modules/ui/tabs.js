export function initializeTabs() {
    const tabContainers = document.querySelectorAll('.tabs-container');

    tabContainers.forEach(container => {
        const tabNav = container.querySelector('.tabs-nav');
        const tabLinks = container.querySelectorAll('.tab-link');
        const tabPanes = container.querySelectorAll('.tab-pane');

        tabNav.addEventListener('click', (e) => {
            const clickedTab = e.target.closest('.tab-link');
            if (!clickedTab) return;

            e.preventDefault();

            // Desativa todas as abas e painéis
            tabLinks.forEach(link => {
                link.classList.remove('active');
                link.setAttribute('aria-selected', 'false');
            });
            tabPanes.forEach(pane => pane.classList.remove('active'));

            // Ativa a aba e o painel clicados
            const tabId = clickedTab.dataset.tab;
            const activePane = container.querySelector(`#${tabId}`);

            clickedTab.classList.add('active');
            clickedTab.setAttribute('aria-selected', 'true');

            if (activePane) {
                activePane.classList.add('active');
            }
        });
    });
}