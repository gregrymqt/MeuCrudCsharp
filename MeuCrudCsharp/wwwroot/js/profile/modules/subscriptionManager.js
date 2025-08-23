// /js/modules/subscriptionManager.js
import { updateSubscriptionCard, reactivateSubscription } from './api/subscriptionAPI.js';
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
        // Opcional: recarregar a página para refletir as mudanças.
        // setTimeout(() => location.reload(), 2200);
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

// Inicializa o formulário de reativação de assinatura
export function initializeReactivationForm() {
    const form = document.getElementById('form-reactivate-subscription');
    if (!form) return;

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        const submitButton = e.target.querySelector('button[type="submit"]');

        const result = await Swal.fire({
            title: 'Reativar Assinatura?',
            text: 'A cobrança será retomada no próximo ciclo.',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'Sim, reativar!',
            cancelButtonText: 'Cancelar'
        });

        if (!result.isConfirmed) return;

        submitButton.disabled = true;
        submitButton.textContent = 'Reativando...';

        try {
            await reactivateSubscription();
            await Swal.fire({
                icon: 'success',
                title: 'Reativada!',
                text: 'Sua assinatura foi reativada com sucesso.'
            });
            location.reload();
        } catch (error) {
            Swal.fire({ icon: 'error', title: 'Erro!', text: error.message });
            submitButton.disabled = false;
            submitButton.textContent = 'Reativar Assinatura';
        }
    });
}