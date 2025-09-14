// /js/modules/mercadopagoManager.js
let bricksBuilder;

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
        initialization: {amount: 1.00}, // Valor simbólico para validação do cartão
        customization: {visual: {style: {theme: 'bootstrap'}}},
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