// /js/modules/subscriptionManager.js
import { updateSubscriptionCard, reactivateSubscription, cancelSubscription } from './api/subscriptionAPI.js';
import { createAndRenderCardBrick } from './mercadopagoManager.js';

let primaryBrickRendered = false;
let secondaryBrickRendered = false;

// Função para lidar com a atualização do cartão no back-end
async function handleCardUpdate(formData) {
    try {
        await updateSubscriptionCard({ newCardToken: formData.token });
        Swal.fire({
            icon: 'success',
            title: 'Sucesso!',
            text: 'Seu cartão foi atualizado com sucesso.',
            timer: 2000,
            showConfirmButton: false
        });
        setTimeout(() => location.reload(), 2200);
    } catch (error) {
        Swal.fire({
            icon: 'error',
            title: 'Oops...',
            text: error.message
        });
        // Rejeita a promise para que o Brick saiba que houve um erro.
        return Promise.reject(error);
    }
}

// Inicializa a lógica dos accordions para troca de cartão
export function initializeCardAccordions() {
    const accordionHeaders = document.querySelectorAll('.accordion-header');

    accordionHeaders.forEach(header => {
        header.addEventListener('click', () => {
            const body = header.nextElementSibling;
            const wasActive = header.classList.contains('active');

            accordionHeaders.forEach(h => {
                h.classList.remove('active');
                h.nextElementSibling.classList.remove('active');
            });

            if (!wasActive) {
                header.classList.add('active');
                body.classList.add('active');

                const brickTargetId = header.getAttribute('data-brick-target');
                if (brickTargetId === 'primary-card-brick-container' && !primaryBrickRendered) {
                    createAndRenderCardBrick(brickTargetId, handleCardUpdate);
                    primaryBrickRendered = true;
                } else if (brickTargetId === 'secondary-card-brick-container' && !secondaryBrickRendered) {
                    createAndRenderCardBrick(brickTargetId, handleCardUpdate);
                    secondaryBrickRendered = true;
                }
            }
        });
    });
}

async function initializeSubscriptionActionForm(options) {
    const form = document.getElementById(options.formId);
    if (!form) return;

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        const submitButton = e.target.querySelector('button[type="submit"]');

        const result = await Swal.fire({
            title: options.confirmTitle,
            text: options.confirmText,
            icon: options.icon,
            showCancelButton: true,
            confirmButtonText: options.confirmButtonText,
            cancelButtonText: 'Cancelar',
            confirmButtonColor: options.confirmButtonColor || '#3085d6',
            cancelButtonColor: options.cancelButtonColor || '#aaa'
        });

        if (!result.isConfirmed) return;

        const originalButtonText = submitButton.textContent;
        submitButton.disabled = true;
        submitButton.textContent = `${options.verb}ndo...`;

        try {
            // Chama a função de API genérica com o status correto
            await updateSubscriptionStatus(options.status);

            await Swal.fire({
                icon: 'success',
                title: options.successTitle,
                text: options.successText
            });
            location.reload();
        } catch (error) {
            Swal.fire({ icon: 'error', title: 'Erro!', text: error.message });
            submitButton.disabled = false;
            submitButton.textContent = originalButtonText;
        }
    });
}

/**
 * ✅ 3. FUNÇÃO PÚBLICA E UNIFICADA
 * Procura e inicializa todos os formulários de gerenciamento de assinatura na página.
 */
export function initializeSubscriptionForms() {
    // Configura o formulário de reativação
    initializeSubscriptionActionForm({
        formId: 'form-reactivate-subscription',
        status: 'authorized', // Status enviado para a API
        verb: 'Reativa',
        confirmTitle: 'Reativar Assinatura?',
        confirmText: 'A cobrança será retomada no próximo ciclo.',
        icon: 'question',
        confirmButtonText: 'Sim, reativar!',
        successTitle: 'Reativada!',
        successText: 'Sua assinatura foi reativada com sucesso.'
    });

    // Configura o formulário de pausa
    initializeSubscriptionActionForm({
        formId: 'form-pause-subscription',
        status: 'paused',
        verb: 'Pausa',
        confirmTitle: 'Pausar Assinatura?',
        confirmText: 'As cobranças serão interrompidas até que você reative.',
        icon: 'warning',
        confirmButtonText: 'Sim, pausar!',
        successTitle: 'Pausada!',
        successText: 'Sua assinatura foi pausada com sucesso.'
    });

    // Configura o formulário de cancelamento (adicione um se precisar)
    initializeSubscriptionActionForm({
        formId: 'form-cancel-subscription',
        status: 'cancelled',
        verb: 'Cancela',
        confirmTitle: 'Cancelar Assinatura?',
        confirmText: 'Esta ação é definitiva e não pode ser desfeita.',
        icon: 'error',
        confirmButtonColor: '#d33',
        confirmButtonText: 'Sim, cancelar!',
        successTitle: 'Cancelada!',
        successText: 'Sua assinatura foi cancelada com sucesso.'
    });
}
