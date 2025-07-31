document.addEventListener('DOMContentLoaded', () => {
    // PASSO 1: VERIFICAÇÕES INICIAIS
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
        paymentContainer.style.display = 'block';
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
            },
        },
        callbacks: {
            onReady: () => {
                document.getElementById('loading-message').style.display = 'none';
                document.getElementById('paymentBrick_container').style.display = 'block';
            },
            onSubmit: async ({ formData }) => {
                document.getElementById('paymentBrick_container').style.display = 'none';
                document.getElementById('loading-message').style.display = 'block';
                document.getElementById('error-container').style.display = 'none';

                // --- ALTERAÇÃO PRINCIPAL ---
                // Adicionamos os dados do plano ao corpo da requisição
                const requestData = {
                    ...formData,
                    plano: window.paymentConfig.plano, // Envia o nome do plano
                    preapprovalPlanId: window.paymentConfig.preapprovalPlanId // Envia o ID do plano de assinatura
                };
                
                try {
                    const response = await fetch(window.paymentConfig.processPaymentUrl, {
                        method: "POST",
                        headers: {
                            "Content-Type": "application/json",
                            // Gerar uma chave de idempotência única para cada tentativa de pagamento
                            "X-Idempotency-Key": self.crypto.randomUUID()
                        },
                        body: JSON.stringify(requestData),
                    });

                    const responseData = await response.json();

                    if (!response.ok) {
                        throw new Error(responseData.message || `Erro HTTP: ${response.status}`);
                    }

                    // O ID pode ser de um pagamento ou de uma assinatura
                    const id = responseData.id || responseData.subscriptionId;

                    if (!id || !responseData.status) {
                        throw new Error(responseData.message || 'Resposta inválida do servidor.');
                    }
                    
                    document.getElementById('loading-message').style.display = 'none';
                    
                    // Se for uma assinatura, redirecionamos ou mostramos uma mensagem de sucesso.
                    // Se for um pagamento, mostramos o status brick.
                    if (window.paymentConfig.plano === 'anual') {
                         // Redireciona para uma página de sucesso da assinatura
                         window.location.href = '/Subscription/Success';
                    } else {
                        // Para pagamento único, renderiza o Status Brick
                        document.getElementById('statusScreenBrick_container').style.display = 'block';
                        await renderStatusScreenBrick(builder, id);
                    }

                } catch (error) {
                    showError(`Erro ao processar pagamento: ${error.message}`);
                    throw error;
                }
            },
            onError: (error) => {
                showError('Verifique os dados informados. ' + (error?.message || ''));
            },
        },
    };
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