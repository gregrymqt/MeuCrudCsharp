// /js/modules/mercadopagoManager.js

let bricksBuilder;

/**
 * Inicializa o SDK do Mercado Pago e o construtor de Bricks.
 * @returns {boolean} - Retorna true se a inicialização foi bem-sucedida.
 */
export function initializeMercadoPago() {
    const publicKey = 'APP_USR-9237cffa-5ad4-4056-956b-20d62d1d0dab'; // Sua Public Key
    if (!publicKey || publicKey.startsWith('APP_USR-')) {
        console.error('A Public Key do Mercado Pago não está configurada!');
        return false;
    }
    const mp = new MercadoPago(publicKey);
    bricksBuilder = mp.bricks();
    return true;
}

/**
 * Cria e renderiza um Card Payment Brick do Mercado Pago.
 * @param {string} containerId - O ID do elemento HTML onde o Brick será renderizado.
 * @param {Function} onSubmitCallback - A função a ser executada no 'onSubmit' do Brick.
 */
export async function createAndRenderCardBrick(containerId, onSubmitCallback) {
    if (!bricksBuilder) {
        console.error("Mercado Pago Bricks não foi inicializado.");
        return;
    }

    const settings = {
        initialization: { amount: 1.00 }, // Valor simbólico para validação do cartão
        customization: { visual: { style: { theme: 'bootstrap' } } },
        callbacks: {
            onReady: () => console.log(`Brick em #${containerId} está pronto.`),
            onError: (error) => console.error(`Erro no Brick #${containerId}:`, error),
            onSubmit: (formData) => {
                // O callback 'onSubmit' do Brick espera uma Promise.
                return new Promise((resolve, reject) => {
                    onSubmitCallback(formData)
                        .then(resolve)
                        .catch(reject);
                });
            },
        },
    };

    // Limpa o container antes de renderizar para evitar duplicatas
    const container = document.getElementById(containerId);
    if (container) {
        container.innerHTML = '';
        await bricksBuilder.create('cardPayment', containerId, settings);
    }
}