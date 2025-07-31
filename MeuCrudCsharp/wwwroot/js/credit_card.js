document.addEventListener('DOMContentLoaded', () => {
    // PASSO 1: VERIFICAÇÕES INICIAIS

    // Garante que o SDK do Mercado Pago e as configurações globais foram carregadas.
    if (typeof MercadoPago === 'undefined') {
        showError('O SDK do Mercado Pago não foi carregado.');
        return;
    }

    if (!window.paymentConfig?.publicKey || !window.paymentConfig?.preferenceId) {
        showError('Erro de configuração: Chave pública ou ID da preferência não encontrados.');
        return;
    }

    // PASSO 2: INICIALIZAÇÃO DO SDK E DOS BRICKS
    const mp = new MercadoPago(window.paymentConfig.publicKey, {
        locale: 'pt-BR'
    });

    const bricksBuilder = mp.bricks();

    // Inicia o processo de renderização do formulário de pagamento.
    renderPaymentBrick(bricksBuilder);
});


// FUNÇÕES AUXILIARES

/**
 * Exibe uma mensagem de erro na tela.
 * @param {string} message - A mensagem de erro a ser exibida.
 */
const showError = (message) => {
    const errorContainer = document.getElementById('error-container');
    const loadingMessage = document.getElementById('loading-message');
    const paymentContainer = document.getElementById('paymentBrick_container');

    if (errorContainer) {
        errorContainer.textContent = message;
        errorContainer.style.display = 'block';
    }
    if (loadingMessage) {
        loadingMessage.style.display = 'none';
    }
    if (paymentContainer) {
        paymentContainer.style.display = 'block'; // Garante que o formulário reapareça em caso de erro
    }
    console.error(message);
};


/**
 * Renderiza o Brick de Pagamento na tela.
 * @param {object} builder - A instância do construtor de bricks do Mercado Pago.
 */
async function renderPaymentBrick(builder) {
    const settings = {
        initialization: {
            amount: window.paymentConfig.amount,
            preferenceId: window.paymentConfig.preferenceId,
        },
        customization: {
            paymentMethods: {
                creditCard: "all",
                ticket: "all",
                pix: "all",
            },
        },
        callbacks: {
            onReady: () => {
                console.log("Payment Brick está pronto.");
                // Esconde a mensagem de "carregando" e mostra o formulário
                document.getElementById('loading-message').style.display = 'none';
                document.getElementById('paymentBrick_container').style.display = 'block';
            },
            onSubmit: async (params) => {
                // Esconde o formulário e mostra a mensagem de "processando"
                document.getElementById('paymentBrick_container').style.display = 'none';
                document.getElementById('loading-message').style.display = 'block';
                document.getElementById('error-container').style.display = 'none';

                try {
                    // Envia os dados do pagamento para o seu backend
                    const response = await fetch(window.paymentConfig.processPaymentUrl, {
                        method: "POST",
                        headers: { "Content-Type": "application/json" },
                        body: JSON.stringify(params.formData),
                    });

                    const responseData = await response.json();

                    if (!response.ok) {
                        throw new Error(responseData.message || `Erro HTTP: ${response.status}`);
                    }

                    if (!responseData.id || !responseData.status) {
                        throw new Error(responseData.message || 'Resposta inválida do servidor.');
                    }

                    // Se o pagamento foi processado, esconde a mensagem de "carregando"
                    // e mostra a tela de status.
                    document.getElementById('loading-message').style.display = 'none';
                    document.getElementById('statusScreenBrick_container').style.display = 'block';

                    // Renderiza o Brick de Status do Pagamento
                    await renderStatusScreenBrick(builder, responseData.id);

                } catch (error) {
                    showError(`Erro ao processar pagamento: ${error.message}`);
                    // Rejeita a promessa para o Brick saber que houve um erro
                    throw error;
                }
            },
            onError: (error) => {
                showError('Verifique os dados informados. ' + (error?.message || ''));
            },
        },
    };
    // Cria e guarda a instância do controller do brick para referência futura, se necessário
    window.paymentBrickController = await builder.create("payment", "paymentBrick_container", settings);
}

/**
 * Renderiza o Brick de Status do Pagamento na tela.
 * @param {object} builder - A instância do construtor de bricks do Mercado Pago.
 * @param {string} paymentId - O ID do pagamento retornado pelo seu backend.
 */
async function renderStatusScreenBrick(builder, paymentId) {
    const settings = {
        initialization: {
            paymentId: paymentId
        },
        callbacks: {
            onReady: () => console.log('Status Screen Brick pronto.'),
            onError: (error) => showError('Ocorreu um erro ao exibir o status do pagamento: ' + error.message),
        },
    };
    window.statusScreenBrickController = await builder.create('statusScreen', 'statusScreenBrick_container', settings);
}
