// /js/modules/services/mercadopagoService.js

import { showError } from '../ui/paymentUI.js';

let mp;
let bricksBuilder;

/** Inicializa o SDK do Mercado Pago. */
export function initializeMercadoPago(publicKey) {
    mp = new MercadoPago(publicKey, { locale: 'pt-BR' });
    bricksBuilder = mp.bricks();
}

/** Renderiza o Brick de Pagamento. */
export function renderPaymentBrick(callbacks) {
    const settings = {
        initialization: {
            amount: window.paymentConfig.amount,
            preferenceId: window.paymentConfig.preferenceId
        },
        customization: {
            paymentMethods: { creditCard: "all", ticket: "all", pix: "all" }
        },
        callbacks: callbacks // Passa os callbacks diretamente (onReady, onSubmit, onError)
    };
    bricksBuilder.create("payment", "paymentBrick_container", settings);
}

/** Renderiza o Brick de Status da Tela. */
export function renderStatusScreenBrick(paymentId) {
    const settings = {
        initialization: { paymentId: paymentId },
        callbacks: {
            onReady: () => console.log('Status Screen Brick está pronto.'),
            onError: (error) => showError('Erro ao exibir status: ' + error.message)
        }
    };
    const container = document.getElementById('statusScreenBrick_container');
    container.innerHTML = ''; // Limpa antes de renderizar
    bricksBuilder.create('statusScreen', 'statusScreenBrick_container', settings);
}