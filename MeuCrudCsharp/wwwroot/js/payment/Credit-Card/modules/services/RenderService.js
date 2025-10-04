import { showError } from '../ui/paymentUI.js';

let bricksBuilder;
let mpInstance;

/**
 * Inicializa o serviço de renderização com a instância do Mercado Pago.
 * @param {MercadoPago} mp - A instância do SDK do Mercado Pago.
 */
export function initializeRenderService(mp) {
    mpInstance = mp;
    bricksBuilder = mp.bricks();
}

/**
 * Renderiza o Brick de Pagamento com base nas configurações fornecidas.
 * @param {object} settings - O objeto de configuração completo para o Brick.
 */
export function renderPaymentBrick(settings) {
    if (!bricksBuilder) {
        showError("O serviço de renderização não foi inicializado.");
        return;
    }
    // A função agora é "burra": ela apenas pega as configurações prontas
    // e manda o SDK criar o Brick.
    bricksBuilder.create("payment", "paymentBrick_container", settings);
}

/**
 * Renderiza o Brick de Status da Tela.
 * @param {string} paymentId - O ID do pagamento gerado.
 */
export function renderStatusScreenBrick(paymentId) {
    if (!bricksBuilder) {
        showError("O serviço de renderização não foi inicializado.");
        return;
    }
    const settings = {
        initialization: { paymentId: paymentId },
        callbacks: {
            onReady: () => console.log('Status Screen Brick está pronto.'),
            onError: (error) => showError('Erro ao exibir status: ' + error.message)
        }
    };
    // Limpa o container para garantir que não haja duplicatas
    const container = document.getElementById('statusScreenBrick_container');
    container.innerHTML = '';
    bricksBuilder.create('statusScreen', 'statusScreenBrick_container', settings);
}