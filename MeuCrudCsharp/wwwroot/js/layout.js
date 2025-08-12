document.addEventListener('DOMContentLoaded', function () {
    // --- Lógica para o Menu Mobile ---
    const navToggle = document.getElementById('nav-toggle');
    const navMenu = document.getElementById('nav-menu');

    if (navToggle && navMenu) {
        navToggle.addEventListener('click', () => {
            navMenu.classList.toggle('is-active');
            // Altera o ícone do botão
            const icon = navToggle.querySelector('i');
            if (navMenu.classList.contains('is-active')) {
                icon.classList.remove('fa-bars');
                icon.classList.add('fa-times');
            } else {
                icon.classList.remove('fa-times');
                icon.classList.add('fa-bars');
            }
        });
    }

    // --- Lógica para o Dropdown do Usuário ---
    const userDropdownButton = document.getElementById('user-dropdown-button');
    const userDropdown = userDropdownButton ? userDropdownButton.closest('.user-dropdown') : null;

    if (userDropdownButton && userDropdown) {
        userDropdownButton.addEventListener('click', (event) => {
            // Impede que o clique no botão feche o menu imediatamente (ver próximo listener)
            event.stopPropagation(); 
            userDropdown.classList.toggle('is-active');
        });

        // Opcional, mas recomendado: Fecha o dropdown se clicar fora dele
        document.addEventListener('click', (event) => {
            if (userDropdown.classList.contains('is-active') && !userDropdown.contains(event.target)) {
                userDropdown.classList.remove('is-active');
            }
        });
    }
});