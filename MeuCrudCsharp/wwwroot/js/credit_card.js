/**
 * Exibe uma mensagem de erro na interface do usuário e no console.
 * @param {string} message - A mensagem de erro a ser exibida.
 */
function showError(message) {
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
}

/**
 * Ponto de entrada para inicializar o processo de pagamento do Mercado Pago.
 * Verifica a disponibilidade do SDK e das configurações antes de renderizar o Brick de Pagamento.
 */
function initializePayment() {
    if (typeof MercadoPago === 'undefined') {
        showError('O SDK do Mercado Pago não foi carregado.');
        return;
    }
    if (!window.paymentConfig || !window.paymentConfig.publicKey || !window.paymentConfig.preferenceId) {
        showError('Erro de configuração: Chave pública ou ID da preferência não encontrados.');
        return;
    }
    var mp = new MercadoPago(window.paymentConfig.publicKey, {
        locale: 'pt-BR'
    });
    var bricksBuilder = mp.bricks();
    renderPaymentBrick(bricksBuilder);
}

/**
 * Renderiza o Brick de Pagamento do Mercado Pago.
 * Configura os callbacks para lidar com o envio do formulário, erros e o estado de "pronto".
 * @param {object} builder - A instância do construtor de Bricks do Mercado Pago.
 */
function renderPaymentBrick(builder) {
    var settings = {
        initialization: {
            amount: window.paymentConfig.amount,
            preferenceId: window.paymentConfig.preferenceId
        },
        customization: {
            paymentMethods: {
                creditCard: "all",
                ticket: "all",
                pix: "all"
            }
        },
        callbacks: {
            onReady: function() {
                console.log("Payment Brick está pronto.");
            },
            onSubmit: function(params) {
                document.getElementById('paymentBrick_container').style.display = 'none';
                document.getElementById('loading-message').style.display = 'block';
                document.getElementById('error-container').style.display = 'none';
                fetch(window.paymentConfig.processPaymentUrl, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify(params.formData)
                })
                .then(function(response) {
                    if (!response.ok) {
                        return response.json().then(function(err) {
                            throw new Error(err.message || "Erro HTTP: " + response.status);
                        });
                    }
                    return response.json();
                })
                .then(function(responseData) {
                    if (!responseData.id || !responseData.status) {
                        throw new Error(responseData.message || 'Resposta inválida do servidor.');
                    }
                    document.getElementById('loading-message').style.display = 'none';
                    document.getElementById('statusScreenBrick_container').style.display = 'block';
                    renderStatusScreenBrick(builder, responseData.id);
                })
                .catch(function(error) {
                    showError('Erro ao processar pagamento: ' + error.message);
                });
            },
            onError: function(error) {
                showError('Verifique os dados informados. ' + (error && error.message ? error.message : ''));
            }
        }
    };
    builder.create("payment", "paymentBrick_container", settings).then(function(controller) {
        window.paymentBrickController = controller;
    });
}

/**
 * Renderiza o Brick de Status da Tela do Mercado Pago após a conclusão de um pagamento.
 * @param {object} builder - A instância do construtor de Bricks do Mercado Pago.
 * @param {string} paymentId - O ID do pagamento cujo status será exibido.
 */
function renderStatusScreenBrick(builder, paymentId) {
    var settings = {
        initialization: { paymentId: paymentId },
        callbacks: {
            onReady: function() { console.log('Status Screen Brick pronto.'); },
            onError: function(error) { showError('Ocorreu um erro ao exibir o status do pagamento: ' + error.message); }
        }
    };
    builder.create('statusScreen', 'statusScreenBrick_container', settings).then(function(controller) {
        window.statusScreenBrickController = controller;
    });
}

// Inicializa o processo de pagamento assim que o conteúdo do DOM for totalmente carregado.
document.addEventListener('DOMContentLoaded', initializePayment);