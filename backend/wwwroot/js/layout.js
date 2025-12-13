document.addEventListener('DOMContentLoaded', function () {

    // --- 1. CONTROLE DA SIDEBAR DE NAVEGAÇÃO (MOBILE) ---
    const navToggle = document.getElementById('nav-toggle');
    const navMenu = document.getElementById('nav-menu');

    if (navToggle && navMenu) {
        navToggle.addEventListener('click', () => {
            // Mostra ou esconde a sidebar
            navMenu.classList.toggle('show');

            // Troca o ícone do botão entre 'hambúrguer' e 'X'
            const icon = navToggle.querySelector('i');
            if (navMenu.classList.contains('show')) {
                icon.classList.remove('fa-bars');
                icon.classList.add('fa-times');
            } else {
                icon.classList.remove('fa-times');
                icon.classList.add('fa-bars');
            }
        });
    }


    // --- 2. CONTROLE DO DROPDOWN DO USUÁRIO (_LoginPartial) ---
    const dropdownButton = document.getElementById('user-dropdown-button');
    const dropdownMenu = document.getElementById('user-dropdown-menu');

    // Executa apenas se o botão do dropdown (usuário logado) existir na página
    if (dropdownButton && dropdownMenu) {

        // Função para abrir/fechar o menu dropdown
        function toggleDropdown() {
            const isExpanded = dropdownButton.getAttribute('aria-expanded') === 'true';
            dropdownButton.setAttribute('aria-expanded', !isExpanded);
            dropdownMenu.classList.toggle('show');
        }

        // Adiciona o evento de clique ao botão do usuário
        dropdownButton.addEventListener('click', function (event) {
            event.stopPropagation(); // Impede que o clique se propague e feche o menu imediatamente
            toggleDropdown();
        });

        // Fecha o menu se o usuário clicar em qualquer outro lugar da tela
        window.addEventListener('click', function (event) {
            if (dropdownMenu.classList.contains('show') && !dropdownButton.contains(event.target)) {
                toggleDropdown();
            }
        });

        // Fecha o menu se o usuário pressionar a tecla 'Escape'
        window.addEventListener('keydown', function (event) {
            if (event.key === 'Escape' && dropdownMenu.classList.contains('show')) {
                toggleDropdown();
            }
        });
    }

    try {
        // Pega o caminho da URL atual (ex: "/Courses/Index")
        const currentPagePath = window.location.pathname;

        // Seleciona todos os links de navegação dentro da lista
        const navLinks = document.querySelectorAll('.nav-links .nav-link');

        navLinks.forEach(link => {
            // Pega o caminho do atributo href do link
            const linkPath = link.getAttribute('href');

            // Condição especial para a Home Page, que pode ser "/" ou "/Index"
            const isHomePage = (currentPagePath === '/' && (linkPath.toLowerCase() === '/index' || linkPath === '/'));

            // Compara o caminho do link com o da página atual (ignorando maiúsculas/minúsculas)
            // A comparação direta funciona para as outras páginas
            if (isHomePage || (linkPath.toLowerCase() === currentPagePath.toLowerCase())) {

                // Adiciona a classe 'active' para estilização no CSS
                link.classList.add('active');

                // Adiciona um atributo ARIA para acessibilidade, indicando que é a página atual
                link.setAttribute('aria-current', 'page');
            }
        });

    } catch (error) {
        console.error('Erro ao tentar marcar o link ativo:', error);
    }
});