import * as ui from './modules/ui/ui.js';
import * as api from './modules/api/api.js';
import * as helpers from '../shared/ui-helpers.js';
import { createStatusHandler } from '../shared/status-handler.js';
import { createPaymentHubConnection } from '../shared/signalr.js';
import {initializeMercadoPago} from "../../core/mercadoPagoService";

// --- Estado do Módulo ---
let mpInstance;
let pixData;

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
        // 1. LER OS DADOS DO HTML (A ÚNICA FONTE DA VERDADE)
        const dataElement = document.getElementById('pix-data');
        if (!dataElement?.textContent) throw new Error("Dados de configuração do PIX não encontrados.");
        pixData = JSON.parse(dataElement.textContent);

        mpInstance = initializeMercadoPago(pixData.publicKey);
        const container = document.getElementById('pix-content-container');

        // 2. INICIAR O SIGNALR (usando o ID do usuário vindo do backend)
        const hubConnection = createPaymentHubConnection({
            onUpdate: (message) => console.log(`PIX Status: ${message}`),
            onSuccess: () => {
                ui.renderStatusView(container, 'approved');
                helpers.updateTabState(3);
                hubConnection.stop();
            },
            onFailure: () => {
                ui.renderStatusView(container, 'rejected');
                helpers.updateTabState(3);
                hubConnection.stop();
            }
        });
        await hubConnection.start();
        
        // 3. RENDERIZAR O FORMULÁRIO INICIAL (com dados pré-preenchidos)
        ui.renderForm(container, pixData.payer);
        helpers.updateTabState(1);
        await loadIdentificationTypes();

        const form = container.querySelector('#form-checkout');
        form.addEventListener('submit', handleFormSubmit);

    } catch (error) {
        helpers.displayError(`Ocorreu um erro fatal ao carregar o pagamento: ${error.message}`);
    }
}