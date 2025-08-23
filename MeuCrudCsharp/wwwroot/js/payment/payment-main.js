// /js/payment-main.js

import * as UI from './modules/ui/paymentUI.js';
import * as MP from './modules/services/mercadopagoService.js';
import { createPaymentHubConnection } from './modules/services/signalRService.js';
import { processPayment } from './modules/api/paymentAPI.js';

/**
 * Fun��o principal que orquestra o processo de pagamento.
 * @param {object} formData - Dados do formul�rio do Brick.
 */
async function handlePaymentSubmit({ formData }) {
    UI.showLoading("Conectando ao servi�o de pagamento...");

    let hubConnection;

    const handleStatusUpdate = (data) => {
        console.log("Status recebido via SignalR:", data);
        UI.showLoading(data.message); // Atualiza a mensagem na tela

        if (data.isComplete) {
            hubConnection.stop();
            if (data.status === 'approved' || data.status === 'pending') {
                UI.showStatusScreen();
                MP.renderStatusScreenBrick(data.paymentId);
            } else {
                UI.showError(data.message);
            }
        }
    };

    hubConnection = createPaymentHubConnection(handleStatusUpdate);

    try {
        await hubConnection.start();
        await hubConnection.subscribe(window.paymentConfig.userId);

        console.log("Conectado ao Hub! Enviando pagamento para o back-end...");
        UI.showLoading("Processando seu pagamento...");

        // A chamada � API agora s� inicia o processo, o resto � via SignalR
        await processPayment(formData);

    } catch (error) {
        console.error("Erro no processo de pagamento:", error);
        UI.showError('Erro: ' + error.message);
        hubConnection?.stop();
    }
}

/**
 * Inicializa a p�gina de pagamento.
 */
function initializePage() {
    if (typeof MercadoPago === 'undefined' || typeof signalR === 'undefined') {
        UI.showError('Falha ao carregar depend�ncias essenciais (Mercado Pago ou SignalR).');
        return;
    }
    if (!window.paymentConfig?.publicKey || !window.paymentConfig?.preferenceId) {
        UI.showError('Erro de configura��o: Chave p�blica ou ID de prefer�ncia n�o encontrados.');
        return;
    }

    MP.initializeMercadoPago(window.paymentConfig.publicKey);

    const brickCallbacks = {
        onReady: () => {
            console.log("Payment Brick est� pronto.");
            UI.showPaymentForm();
        },
        onSubmit: handlePaymentSubmit,
        onError: (error) => {
            UI.showError('Por favor, verifique os dados inseridos. ' + (error?.message || ''));
        }
    };

    MP.renderPaymentBrick(brickCallbacks);
}

document.addEventListener('DOMContentLoaded', initializePage);