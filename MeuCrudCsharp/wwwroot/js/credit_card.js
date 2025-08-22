/**
 * ====================================================================================
 * ARQUIVO JAVASCRIPT COMBINADO: MERCADO PAGO BRICKS + SIGNALR
 * Responsabilidade: Orquestrar o fluxo de pagamento do Mercado Pago com
 * notificações em tempo real via SignalR.
 * ====================================================================================
 */

document.addEventListener('DOMContentLoaded', initializePayment);

// --- FUNÇÕES DO MERCADO PAGO ---

function renderStatusScreenBrick(builder, paymentId) {
    // Esconde a mensagem de carregamento e mostra o container do status
    document.getElementById('loading-message').style.display = 'none';
    const statusContainer = document.getElementById('statusScreenBrick_container');
    statusContainer.style.display = 'block';

    const settings = {
        initialization: { paymentId: paymentId },
        callbacks: {
            onReady: () => console.log('Status Screen Brick está pronto.'),
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
                console.log("Payment Brick está pronto.");
                const loadingMessage = document.getElementById('loading-message');
                if (loadingMessage) loadingMessage.style.display = 'none';
            },
            // A MÁGICA ACONTECE AQUI! O onSubmit agora é uma função assíncrona.
            onSubmit: async ({ formData }) => {
                // Esconde o formulário e mostra uma mensagem de "processando".
                document.getElementById('paymentBrick_container').style.display = 'none';
                document.getElementById('loading-message').style.display = 'block';
                document.getElementById('error-container').style.display = 'none';

                // --- INÍCIO DA INTEGRAÇÃO COM SIGNALR ---

                // 1. Constrói a conexão com o Hub de Pagamento.
                const connection = new signalR.HubConnectionBuilder()
                    .withUrl("/paymentProcessingHub")
                    .build();

                // 2. Define o que fazer quando receber uma mensagem "PaymentStatusUpdate".
                connection.on("PaymentStatusUpdate", (data) => {
                    console.log("Status recebido via SignalR:", data); // { message, status, isComplete }

                    // Atualiza a mensagem de carregamento para o usuário.
                    document.getElementById('loading-message').innerText = data.message;

                    // 3. Se o processo terminou (com sucesso ou falha).
                    if (data.isComplete) {
                        connection.stop(); // Desconecta do Hub.

                        if (data.status === 'approved' || data.status === 'pending') {
                            // Sucesso ou pendente, renderiza a tela de status do MP.
                            // O paymentId deve vir do back-end junto com a notificação final.
                            renderStatusScreenBrick(builder, data.paymentId);
                        } else {
                            // Se falhou ('failed', 'error'), mostra a mensagem de erro.
                            showError(data.message);
                        }
                    }
                });

                try {
                    // 4. Inicia a conexão e se inscreve no grupo do usuário.
                    await connection.start();
                    // O userId deve estar disponível globalmente ou ser pego de algum lugar.
                    await connection.invoke("SubscribeToPaymentStatus", window.paymentConfig.userId);

                    console.log("Conectado ao Hub! Enviando pagamento para o back-end...");

                    // 5. AGORA SIM: chama a API para iniciar o processo de pagamento.
                    // O back-end vai processar e enviar as atualizações via SignalR.
                    const response = await fetch(window.paymentConfig.processPaymentUrl, {
                        method: "POST",
                        headers: { "Content-Type": "application/json" },
                        body: JSON.stringify(formData)
                    });

                    // Se a chamada inicial à API falhar, trata o erro imediatamente.
                    if (!response.ok) {
                        const err = await response.json();
                        throw new Error(err.message || "Falha ao iniciar o processo de pagamento.");
                    }

                    // A partir daqui, o front-end apenas espera passivamente pelas notificações do SignalR.

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
        showError('Erro de configuração: Chave pública ou ID de preferência não encontrados.');
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
    // Mostra o formulário de pagamento novamente para o usuário poder tentar de novo.
    if (paymentContainer) {
        paymentContainer.style.display = 'block';
    }
    console.error(message);
}
