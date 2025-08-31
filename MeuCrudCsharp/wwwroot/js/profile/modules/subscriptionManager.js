// /js/modules/subscriptionManager.js
import { updateSubscriptionCard, updateSubscriptionStatus } from './api/subscriptionAPI.js';
import { createAndRenderCardBrick } from './mercadopagoManager.js';

// Flag para garantir que o Brick do Mercado Pago seja renderizado apenas uma vez.
let cardBrickRendered = false;

// --- Funções "Privadas" de Ação ---

/**
 * Callback para o Brick do Mercado Pago. Lida com a atualização do cartão no back-end.
 */
async function handleCardUpdate(formData) {
    try {
        await updateSubscriptionCard({ newCardToken: formData.token });
        await Swal.fire('Sucesso!', 'Seu cartão foi atualizado com sucesso.', 'success');
        sessionStorage.clear(); // Limpa o cache para forçar a busca de novos dados na próxima página
        location.reload();
    } catch (error) {
        Swal.fire('Oops...', error.message, 'error');
        return Promise.reject(error); // Informa ao Brick que a submissão falhou
    }
}

/**
 * Anexa todos os event listeners necessários ao painel já renderizado.
 */
function attachAllListeners() {
    const container = document.getElementById('subscription-content');
    if (!container) return;

    // 1. Lógica do Acordeão e do Card Brick do Mercado Pago
    const accordionHeaders = container.querySelectorAll('.accordion-header');
    accordionHeaders.forEach(header => {
        header.addEventListener('click', () => {
            const wasActive = header.classList.contains('active');
            accordionHeaders.forEach(h => h.classList.remove('active'));
            if (!wasActive) {
                header.classList.add('active');
                if (header.id === 'card-accordion-header' && !cardBrickRendered) {
                    createAndRenderCardBrick('primary-card-brick-container', handleCardUpdate);
                    cardBrickRendered = true;
                }
            }
        });
    });

    // 2. Lógica para o <select> de ações (Pausar, Cancelar, Reativar)
    const actionSelect = container.querySelector('#subscription-action-select');
    actionSelect?.addEventListener('change', async (e) => {
        const action = e.target.value;
        if (!action) return;

        const actionsConfig = {
            pause: { status: 'paused', verb: 'Pausa', title: 'Pausar Assinatura?', text: 'As cobranças serão interrompidas.', icon: 'warning', confirmText: 'Sim, pausar!' },
            cancel: { status: 'cancelled', verb: 'Cancela', title: 'Cancelar Assinatura?', text: 'Esta ação é definitiva.', icon: 'error', confirmText: 'Sim, cancelar!' },
            reactivate: { status: 'authorized', verb: 'Reativa', title: 'Reativar Assinatura?', text: 'A cobrança será retomada.', icon: 'question', confirmText: 'Sim, reativar!' }
        };
        const config = actionsConfig[action];

        const result = await Swal.fire({ title: config.title, text: config.text, icon: config.icon, showCancelButton: true, confirmButtonText: config.confirmText, cancelButtonText: 'Voltar' });
        if (result.isConfirmed) {
            Swal.fire({ title: `${config.verb}ndo...`, text: 'Aguarde um momento.', didOpen: () => Swal.showLoading() });
            try {
                await updateSubscriptionStatus(config.status);
                await Swal.fire('Sucesso!', `Sua assinatura foi ${config.verb.toLowerCase()}da.`, 'success');
                sessionStorage.clear();
                location.reload();
            } catch (error) {
                Swal.fire('Erro!', error.message, 'error');
            }
        }
        e.target.value = ""; // Reseta o select para permitir a mesma ação novamente
    });

    // 3. Lógica do formulário de Reembolso
    const refundForm = container.querySelector('#form-request-refund');
    if (refundForm) {
        initializeSignalR(); // Inicia o SignalR quando o formulário de reembolso está presente
        refundForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const result = await Swal.fire({ title: 'Você tem certeza?', text: "Seu acesso ao conteúdo será revogado.", icon: 'warning', showCancelButton: true, confirmButtonColor: '#d33', confirmButtonText: 'Sim, solicitar!', cancelButtonText: 'Cancelar' });
            if (result.isConfirmed) {
                try {
                    await requestRefund();
                    // Troca a UI para o estado de "processando"
                    container.querySelector('#refund-step-1').style.display = 'none';
                    container.querySelector('#refund-step-2').style.display = 'block';
                } catch (error) {
                    Swal.fire('Erro!', error.message, 'error');
                }
            }
        });
    }
}

/**
 * Função principal exportada. Recebe os dados da assinatura e anexa toda a lógica de interatividade.
 * @param {object} subscriptionData - Os dados da assinatura, vindos do painel de renderização.
 */
export function initializeSubscriptionManager(subscriptionData) {
    // Só anexa os listeners se houver dados de assinatura para gerenciar.
    if (subscriptionData) {
        attachAllListeners();
    }
}

