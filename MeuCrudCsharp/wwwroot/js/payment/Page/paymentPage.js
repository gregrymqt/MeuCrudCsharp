// Importamos a função que comanda a renderização do brick de cartão de crédito
import { renderCreditCardBrick } from '../Credit-Card/credit-card.js';

/**
 * Controla a visibilidade da view de pagamento ativa e o estado do link na sidebar.
 * @param {string} targetId - O ID da view a ser mostrada (ex: '#pix-payment').
 * @param {NodeListOf<Element>} allLinks - Todos os links da sidebar.
 * @param {NodeListOf<Element>} allViews - Todas as views de pagamento.
 */
function activateView(targetId, allLinks, allViews) {
    // Primeiro, desativa tudo para garantir um estado limpo
    allViews.forEach(view => view.classList.remove('active'));
    allLinks.forEach(link => link.classList.remove('active'));

    // Ativa o link e a view corretos
    const targetView = document.querySelector(targetId);
    const targetLink = document.querySelector(`.sidebar-link[data-target="${targetId}"]`);

    if (targetView) targetView.classList.add('active');
    if (targetLink) targetLink.classList.add('active');
}

/**
 * Configura toda a lógica de navegação da sidebar.
 */
function initializeSidebarNavigation() {
    // Seleciona os elementos da página
    const menuToggle = document.getElementById('menu-toggle');
    const sidebar = document.getElementById('sidebar');
    const sidebarLinks = document.querySelectorAll('.sidebar-link');
    const paymentViews = document.querySelectorAll('.payment-method-view');

    // Se não houver sidebar na página, não faz nada.
    if (!sidebar || sidebarLinks.length === 0) {
        return;
    }

    // Lógica para abrir/fechar o menu em telas pequenas
    menuToggle.addEventListener('click', () => {
        sidebar.classList.toggle('open');
    });

    // Adiciona o listener de clique para cada link da sidebar
    sidebarLinks.forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();
            const targetId = link.dataset.target;

            // Mostra a view correta (PIX, Cartão à vista, etc.)
            activateView(targetId, sidebarLinks, paymentViews);

            // COMANDO PARA O MÓDULO DE CARTÃO DE CRÉDITO
            // Se o alvo for um dos containers de cartão de crédito...
            if (targetId.includes('credit-card')) {
                // ...determinamos o tipo e comandamos a renderização do brick.
                const paymentType = targetId.includes('installments') ? 'parcelado' : 'avista';
                renderCreditCardBrick(paymentType);
            }

            // Fecha a sidebar no mobile após a seleção
            if (window.innerWidth < 768) {
                sidebar.classList.remove('open');
            }
        });
    });

    // BÔNUS: No carregamento da página, verifica se um link já está ativo
    // e comanda a renderização do brick de cartão, se for o caso.
    const initiallyActiveLink = document.querySelector('.sidebar-link.active');
    if (initiallyActiveLink && initiallyActiveLink.dataset.target.includes('credit-card')) {
        const initialPaymentType = initiallyActiveLink.dataset.target.includes('installments') ? 'parcelado' : 'avista';
        renderCreditCardBrick(initialPaymentType);
    }
}

/**
 * Função de inicialização do módulo, exportada para o main.js.
 */
export function init() {
    initializeSidebarNavigation();
    console.log("Módulo da Página de Pagamento inicializado.");
}