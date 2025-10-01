import * as ui from './modules/ui/ui.js';
import * as api from './modules/api/api.js';
import * as helpers from '../shared/ui-helpers.js';
// 1. ADICIONADO: Import do novo criador de handlers
import { createStatusHandler } from '../shared/status-handler.js';
// Presumindo que seu SignalR Service unificado está na pasta shared
import { createPaymentHubConnection } from '../shared/signalr.js';
import {initializeMercadoPago} from "../../core/mercadoPagoService";

// --- Estado do Módulo ---
let mercadoPagoInstance = null;

// --- Manipuladores de Eventos e Fluxo ---

/**
 * Carrega e popula os tipos de documento do Mercado Pago.
 * (Esta função permanece inalterada)
 */
async function loadIdentificationTypes() {
    try {
        const identificationTypes = await mercadoPagoInstance.getIdentificationTypes();
        ui.populateIdentificationTypes('pix-identification-type', identificationTypes);
    } catch (e) {
        console.error('Erro ao buscar tipos de documento: ', e);
        helpers.displayError('Não foi possível carregar os tipos de documento.');
    }
}

/**
 * Manipula o envio do formulário de dados.
 * (Esta função permanece inalterada, pois o SignalR já está escutando desde o início)
 */
async function handleFormSubmit(event) {
    event.preventDefault();
    const buttonId = '#form-submit-button';
    helpers.setLoading(true, buttonId);

    try {
        const formData = new FormData(event.target);
        const paymentData = {
            transactionAmount: window.paymentConfig.transactionAmount,
            description: window.paymentConfig.description,
            payer: {
                firstName: formData.get('payerFirstName'),
                lastName: formData.get('payerLastName'),
                email: formData.get('email'),
                identification: {
                    type: formData.get('identificationType'),
                    number: formData.get('identificationNumber'),
                },
            },
        };

        const pixResult = await api.createPixPayment(paymentData);

        const container = document.getElementById('pix-content-container');
        ui.renderPaymentView(container, pixResult);
        helpers.updateTabState(2);

    } catch (error) {
        helpers.displayError(error.message || "Não foi possível gerar o PIX.");
    } finally {
        helpers.setLoading(false, buttonId);
    }
}

/**
 * Função principal que inicializa o módulo PIX.
 */
export async function init() {
    try {
        const { userId, publicKey } = window.paymentConfig;
        if (!userId || !publicKey) {
            throw new Error("Configurações essenciais (userId, publicKey) não encontradas.");
        }

        mercadoPagoInstance = initializeMercadoPago();
        const container = document.getElementById('pix-content-container');

        // 3. ALTERADO: A inicialização do SignalR agora usa o novo padrão
        const hubConnection = createPaymentHubConnection();
        await hubConnection.start();

        // Cria o handler de status dinamicamente usando a fábrica
        const pixStatusHandler = createStatusHandler({
            hubConnection: hubConnection,
            // (Opcional) Adiciona um feedback para o usuário enquanto aguarda
            onUpdate: (message) => console.log(`PIX Status Update: ${message}`),
            onSuccess: (data) => {
                ui.renderStatusView(container, 'approved');
                helpers.updateTabState(3);
            },
            onFailure: (data) => {
                ui.renderStatusView(container, 'rejected');
                helpers.updateTabState(3);
            }
        });

        // Supondo que o seu `createPaymentHubConnection` unificado agora aceite
        if (hubConnection.onUpdate) {
            hubConnection.onUpdate(pixStatusHandler);
        }

        await hubConnection.subscribe(userId);

        // O resto da inicialização permanece igual
        ui.renderForm(container);
        helpers.updateTabState(1);
        await loadIdentificationTypes();
        const form = container.querySelector('#form-checkout');
        form.addEventListener('submit', handleFormSubmit);

        console.log("Módulo PIX inicializado e pronto.");

    } catch (error) {
        console.error("Erro fatal na inicialização do PIX:", error);
        helpers.displayError("Ocorreu um erro ao carregar o pagamento. Tente recarregar a página.");
    }
}