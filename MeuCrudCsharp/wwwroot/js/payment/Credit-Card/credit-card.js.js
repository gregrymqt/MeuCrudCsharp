// /js/payment-main.js

import * as UI from './modules/ui/paymentUI.js';
import * as MP from './modules/services/RenderService.js';
import { processPayment } from './modules/api/paymentAPI.js';
import { initializeMercadoPago } from '../../core/mercadoPagoService.js';
import {createStatusHandler} from "../shared/status-handler.js";
import * as Bricks from "./modules/services/RenderService.js";
import {createPaymentHubConnection} from "../shared/signalr";

/**
 * Função principal que orquestra o processo de pagamento.
 * @param {object} formData - Dados do formulário do Brick.
 */
async function handlePaymentSubmit({ formData }) {
    UI.showLoading("Conectando ao serviço de pagamento...");

    const hubConnection = createPaymentHubConnection(); // O callback é criado depois

    try {
        await hubConnection.start();

        // Cria o handler de status específico para Cartão de Crédito
        const handleStatusUpdate = createStatusHandler({
            hubConnection: hubConnection,
            onUpdate: (message) => UI.showLoading(message),
            onSuccess: (data) => {
                UI.showStatusScreen();
                Bricks.renderStatusScreenBrick(mpInstance, data.paymentId);
            },
            onFailure: (data) => UI.showError(data.message)
        });

        // Registra o handler recém-criado no SignalR
        hubConnection.onUpdate(handleStatusUpdate);

        await hubConnection.subscribe(window.paymentConfig.userId);

        UI.showLoading("Processando seu pagamento...");
        await processPayment(formData);

    } catch (error) {
        UI.showError('Erro: ' + (error.message || 'Não foi possível processar o pagamento.'));
        hubConnection?.stop();
    }
}

/**
 * Inicializa a página de pagamento.
 */
// Renomeamos 'initializePage' para 'init' para padronização
export async function init() {
    if (typeof MercadoPago === 'undefined' || typeof signalR === 'undefined') {
        UI.showError('Falha ao carregar dependências essenciais (Mercado Pago ou SignalR).');
        return;
    }
    const { preferenceId } = window.paymentConfig;

    if (!preferenceId) {
        UI.showError('Erro de configuração. Por favor, recarregue a página.');
        return; // Adicionado 'return' para parar a execução
    }

    await initializeMercadoPago();

    const brickCallbacks = {
        onReady: () => {
            console.log("Payment Brick está pronto.");
            UI.showPaymentForm();
        },
        onSubmit: handlePaymentSubmit,
        onError: (error) => {
            UI.showError('Por favor, verifique os dados inseridos. ' + (error?.message || ''));
        }
    };

    MP.renderPaymentBrick(brickCallbacks);
}
