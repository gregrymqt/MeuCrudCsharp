/**
 * ====================================================================================
 * ARQUIVO JAVASCRIPT COMBINADO: MERCADO PAGO BRICKS + SIGNALR
 * Responsabilidade: Orquestrar o fluxo de pagamento do Mercado Pago com
 * notifica��es em tempo real via SignalR.
 * ====================================================================================
 */

document.addEventListener('DOMContentLoaded', initializePayment);

// --- FUN��ES DO MERCADO PAGO ---

function renderStatusScreenBrick(builder, paymentId) {
    // Esconde a mensagem de carregamento e mostra o container do status
    document.getElementById('loading-message').style.display = 'none';
    const statusContainer = document.getElementById('statusScreenBrick_container');
    statusContainer.style.display = 'block';

    const settings = {
        initialization: { paymentId: paymentId },
        callbacks: {
            onReady: () => console.log('Status Screen Brick est� pronto.'),
            onError: (error) => showError('Ocorreu um erro ao exibir o status do pagamento: ' + error.message)
        }
    };
    // Limpa o container antes de criar um novo brick para evitar duplicatas
    statusContainer.innerHTML = '';
    builder.create('statusScreen', 'statusScreenBrick_container', settings);
}

function renderPaymentBrick(builder) {
    const settings = {
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
            onReady: () => {
                console.log("Payment Brick est� pronto.");
                const loadingMessage = document.getElementById('loading-message');
                if (loadingMessage) loadingMessage.style.display = 'none';
            },
            // A M�GICA ACONTECE AQUI! O onSubmit agora � uma fun��o ass�ncrona.
            onSubmit: async ({ formData }) => {
                // Esconde o formul�rio e mostra uma mensagem de "processando".
                document.getElementById('paymentBrick_container').style.display = 'none';
                document.getElementById('loading-message').style.display = 'block';
                document.getElementById('error-container').style.display = 'none';

                // --- IN�CIO DA INTEGRA��O COM SIGNALR ---

                // 1. Constr�i a conex�o com o Hub de Pagamento.
                const connection = new signalR.HubConnectionBuilder()
                    .withUrl("/paymentProcessingHub")
                    .build();

                // 2. Define o que fazer quando receber uma mensagem "PaymentStatusUpdate".
                connection.on("PaymentStatusUpdate", (data) => {
                    console.log("Status recebido via SignalR:", data); // { message, status, isComplete }

                    // Atualiza a mensagem de carregamento para o usu�rio.
                    document.getElementById('loading-message').innerText = data.message;

                    // 3. Se o processo terminou (com sucesso ou falha).
                    if (data.isComplete) {
                        connection.stop(); // Desconecta do Hub.

                        if (data.status === 'approved' || data.status === 'pending') {
                            // Sucesso ou pendente, renderiza a tela de status do MP.
                            // O paymentId deve vir do back-end junto com a notifica��o final.
                            renderStatusScreenBrick(builder, data.paymentId);
                        } else {
                            // Se falhou ('failed', 'error'), mostra a mensagem de erro.
                            showError(data.message);
                        }
                    }
                });

                try {
                    // 4. Inicia a conex�o e se inscreve no grupo do usu�rio.
                    await connection.start();
                    // O userId deve estar dispon�vel globalmente ou ser pego de algum lugar.
                    await connection.invoke("SubscribeToPaymentStatus", window.paymentConfig.userId);

                    console.log("Conectado ao Hub! Enviando pagamento para o back-end...");

                    // 5. AGORA SIM: chama a API para iniciar o processo de pagamento.
                    // O back-end vai processar e enviar as atualiza��es via SignalR.
                    const response = await fetch(window.paymentConfig.processPaymentUrl, {
                        method: "POST",
                        headers: { "Content-Type": "application/json" },
                        body: JSON.stringify(formData)
                    });

                    // Se a chamada inicial � API falhar, trata o erro imediatamente.
                    if (!response.ok) {
                        const err = await response.json();
                        throw new Error(err.message || "Falha ao iniciar o processo de pagamento.");
                    }

                    // A partir daqui, o front-end apenas espera passivamente pelas notifica��es do SignalR.

                } catch (error) {
                    console.error("Erro no processo:", error);
                    showError('Erro: ' + error.message);
                    if (connection.state === 'Connected') {
                        connection.stop();
                    }
                }
            },
            onError: (error) => {
                showError('Por favor, verifique os dados inseridos. ' + (error?.message || ''));
            }
        }
    };

    builder.create("payment", "paymentBrick_container", settings);
}

function initializePayment() {
    if (typeof MercadoPago === 'undefined') {
        showError('SDK do Mercado Pago falhou ao carregar.');
        return;
    }
    if (!window.paymentConfig || !window.paymentConfig.publicKey || !window.paymentConfig.preferenceId) {
        showError('Erro de configura��o: Chave p�blica ou ID de prefer�ncia n�o encontrados.');
        return;
    }

    const mp = new MercadoPago(window.paymentConfig.publicKey, { locale: 'pt-BR' });
    const bricksBuilder = mp.bricks();
    renderPaymentBrick(bricksBuilder);
}

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
    // Mostra o formul�rio de pagamento novamente para o usu�rio poder tentar de novo.
    if (paymentContainer) {
        paymentContainer.style.display = 'block';
    }
    console.error(message);
}
