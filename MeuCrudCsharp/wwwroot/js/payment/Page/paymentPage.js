/**
 * Anexa os listeners de evento para a navegação da sidebar (plano mensal).
 */
function setupMonthlyPlanSidebar() {
    const menuToggle = document.getElementById('menu-toggle');
    const sidebar = document.getElementById('sidebar');
    const sidebarLinks = document.querySelectorAll('.sidebar-link');
    const paymentMethodViews = document.querySelectorAll('.payment-method-view');

    // Abrir/Fechar sidebar no mobile
    menuToggle.addEventListener('click', () => {
        sidebar.classList.toggle('open');
    });

    // Trocar entre PIX e Cartão
    sidebarLinks.forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();

            sidebarLinks.forEach(l => l.classList.remove('active'));
            paymentMethodViews.forEach(v => v.classList.remove('active'));

            link.classList.add('active');
            const targetView = document.querySelector(link.getAttribute('data-target'));
            if (targetView) {
                targetView.classList.add('active');
            }

            if (window.innerWidth < 768) {
                sidebar.classList.remove('open');
            }
        });
    });
}

/**
 * Anexa os listeners de submissão aos formulários de pagamento.
 * Esta função é genérica e funciona para qualquer formulário com a classe .payment-form.
 */
function setupPaymentForms() {
    const paymentForms = document.querySelectorAll('.payment-form');

    paymentForms.forEach(form => {
        form.addEventListener('submit', function (e) {
            e.preventDefault();

            const container = form.closest('.payment-box');
            const formTab = container.querySelector('.tab-pane.active');
            const resultTabPane = container.querySelector('.result-pane');
            const formTabLink = container.querySelector('.tab-link[data-tab="form-tab"]');
            const resultTabLink = container.querySelector('.tab-link[data-tab="result-tab"]');
            const submitButton = form.querySelector('.btn-submit');

            submitButton.disabled = true;
            submitButton.textContent = 'Processando...';

            // Simula uma chamada de API
            setTimeout(() => {
                // Troca de abas
                formTab.classList.remove('active');
                resultTabPane.classList.add('active');
                formTabLink.classList.remove('active');
                resultTabLink.classList.add('active');
                resultTabLink.style.display = 'inline-block';

                // Simula resultado
                const statuses = ['approved', 'pending', 'rejected'];
                const randomStatus = statuses[Math.floor(Math.random() * statuses.length)];

                resultTabPane.querySelectorAll('.payment-status').forEach(s => s.style.display = 'none');

                const statusToShow = resultTabPane.querySelector(`.status-${randomStatus}`);
                if (statusToShow) {
                    statusToShow.style.display = 'block';
                }

                // Restaura o botão
                submitButton.disabled = false;
                // O texto original pode variar (PIX vs Cartão), então buscamos do próprio botão
                const originalButtonText = form.id === 'pix-form' ? 'Já Realizei o Pagamento' : 'Realizar Pagamento';
                submitButton.textContent = originalButtonText;

            }, 2000);
        });
    });
}

/**
 * Função de inicialização do módulo da página de pagamento.
 * Exportada para ser chamada pelo main.js.
 */
export function init() {
    const planType = document.body.getAttribute('data-plano-tipo');

    // Configura a sidebar apenas se for o plano mensal
    if (planType === 'mensal') {
        setupMonthlyPlanSidebar();
    }

    // Configura os formulários de pagamento em qualquer caso
    setupPaymentForms();

    console.log("Módulo da Página de Pagamento inicializado.");
}