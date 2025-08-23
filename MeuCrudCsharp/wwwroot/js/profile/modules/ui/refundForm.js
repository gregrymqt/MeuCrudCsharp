import { requestRefund } from '../api/subscriptionAPI.js';
import { initializeSignalR } from '../services/signalRService.js'; // Importe a função

export function initializeRefundForm() {
    const refundForm = document.getElementById('form-request-refund');
    if (!refundForm) return;

    // Inicia a conexão com o Hub do SignalR assim que a página carrega
    initializeSignalR();

    const refundStep1 = document.getElementById('refund-step-1');
    const refundStep2 = document.getElementById('refund-step-2');
    const submitButton = refundForm.querySelector('button[type="submit"]');

    refundForm.addEventListener('submit', async function (event) {
        event.preventDefault();

        const result = await Swal.fire({
            title: 'Você tem certeza?',
            text: "Esta ação não pode ser desfeita. Seu acesso ao conteúdo será revogado.",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            confirmButtonText: 'Sim, solicitar reembolso!',
            cancelButtonText: 'Cancelar'
        });

        if (!result.isConfirmed) return;

        submitButton.disabled = true;
        submitButton.textContent = 'Processando...';

        try {
            await requestRefund();
            refundStep1.style.display = 'none';
            refundStep2.style.display = 'block';
        } catch (error) {
            Swal.fire({
                icon: 'error',
                title: 'Não foi possível processar',
                text: error.message
            });
        } 
    });
}