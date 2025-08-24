document.addEventListener('DOMContentLoaded', () => {
    const navToggle = document.getElementById('nav-toggle');
    const navMenu = document.getElementById('nav-menu');

    // Verifica se os dois elementos essenciais existem
    if (navToggle && navMenu) {
        navToggle.addEventListener('click', () => {
            // 1. Mostra ou esconde o menu
            navMenu.classList.toggle('show');

            // 2. Troca o ícone
            const icon = navToggle.querySelector('i');
            if (navMenu.classList.contains('show')) {
                // Se o menu está sendo mostrado, mude para 'X'
                icon.classList.remove('fa-bars');
                icon.classList.add('fa-times');
            } else {
                // Se o menu está sendo escondido, volte para 'barras'
                icon.classList.remove('fa-times');
                icon.classList.add('fa-bars');
            }
        });
    }
});