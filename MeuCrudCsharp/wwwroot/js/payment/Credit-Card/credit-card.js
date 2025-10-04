import * as UI from './modules/ui/paymentUI.js';
import * as Bricks from './modules/services/RenderService.js';
import { processPayment } from './modules/api/paymentAPI.js';
import { initializeMercadoPago } from '../../core/mercadoPagoService.js';
import { createPaymentHubConnection } from '/../shared/payment-hub-client.js';


let paymentData; // Guardaremos os dados lidos do HTML aqui
let mpInstance; // Guardaremos a instância do MP aqui

/**
 * Função de setup inicial. Apenas lê os dados e inicializa os serviços.
 * Não renderiza o brick ainda.
 */
export async function init() {
    UI.showLoading(true, 'Carregando informações...');

    const paymentDataElement = document.getElementById('payment-data');
    if (!paymentDataElement?.textContent) {
        UI.showError('Erro crítico: Dados de configuração não encontrados.');
        return;
    }
    // Salva os dados na variável do módulo para uso posterior
    paymentData = JSON.parse(paymentDataElement.textContent);

    // Inicializa os serviços e salva a instância do MP
    mpInstance = await initializeMercadoPago(paymentData.publicKey);
    Bricks.initializeRenderService(mpInstance);

    // O brick não é renderizado aqui! Apenas preparamos o terreno.
    // UI.showPaymentForm(); // Mostra o formulário vazio, esconde o loading inicial
}

/**
 * ESTA É A NOVA FUNÇÃO EXPORTADA
 * Ela será chamada pelo paymentPage.js para efetivamente renderizar o brick.
 * @param {string} paymentType - Pode ser 'avista' ou 'parcelado'.
 */
export function renderCreditCardBrick(paymentType) {
    if (!paymentData || !mpInstance) {
        UI.showError("O módulo de cartão de crédito não foi inicializado corretamente.");
        return;
    }

    // A lógica de submit permanece a mesma
    const handlePaymentSubmit = async ({ formData }) => {
        let hubConnection; // Variável para manter a referência da conexão

        // Estes são os callbacks que dizem ao Hub o que fazer com a UI
        const hubCallbacks = {
            onConnecting: () => UI.showLoading(true, "Conectando ao serviço de pagamento..."),
            onUpdate: (message) => UI.showLoading(true, message),
            onSuccess: (data) => {
                Bricks.renderStatusScreenBrick(data.paymentId);
                UI.showStatusScreen();
            },
            onFailure: (data) => {
                UI.showError(data.message || "Ocorreu uma falha no pagamento.");
            },
            onConnectionClosed: () => UI.showError("Comunicação perdida. Verifique sua conexão e tente novamente.")
        };

        try {
            // Cria a conexão usando nosso novo serviço
            hubConnection = createPaymentHubConnection(hubCallbacks);
            if (!hubConnection) return; // Se a biblioteca do SignalR não carregou, para aqui.

            // Inicia a conexão
            await hubConnection.start();
            await hubConnection.invoke("SubscribeToPaymentStatus", paymentData.id);


            // Dispara a chamada à API para iniciar o processo de pagamento no backend
            // O backend, ao receber isso, começará a enviar atualizações via SignalR
            await processPayment(formData, paymentData.processUrl);

        } catch (error) {
            UI.showError(error.message || 'Não foi possível iniciar o processo de pagamento.');
            // Garante que a conexão seja fechada em caso de erro
            await hubConnection?.stop();
        }
        // NOTA: A conexão pode permanecer aberta esperando a resposta final do backend,
        // ou você pode adicionar um 'finally' para fechá-la após um timeout, por exemplo.
        // Os callbacks onSuccess/onFailure podem ser responsáveis por fechar a conexão.
    };


    let brickSettings;

    // A lógica de montar as configurações com base no tipo de pagamento
    if (paymentType === 'parcelado') {
        if (!paymentData.preapprovalPlanId) {
            UI.showError(`O plano "${paymentData.planName}" não suporta assinatura.`);
            return;
        }
        brickSettings = {
            initialization: { preapprovalPlanId: paymentData.preapprovalPlanId },
            customization: { visual: { style: { theme: 'bootstrap' } } },
            callbacks: {
                onReady: () => UI.showPaymentForm(),
                onSubmit: handlePaymentSubmit,
                onError: (error) => UI.showError(error?.message || 'Erro no formulário.'),
            },
        };
    } else { // 'avista'
        if (!paymentData.preferenceId) {
            UI.showError('Erro de configuração para pagamento à vista.');
            return;
        }
        brickSettings = {
            initialization: { amount: paymentData.amount, preferenceId: paymentData.preferenceId },
            customization: { visual: { style: { theme: 'bootstrap' } } },
            callbacks: {
                onReady: () => UI.showPaymentForm(),
                onSubmit: handlePaymentSubmit,
                onError: (error) => UI.showError(error?.message || 'Erro no formulário.'),
            },
        };
    }

    // Comanda a renderização
    Bricks.renderPaymentBrick(brickSettings);
}