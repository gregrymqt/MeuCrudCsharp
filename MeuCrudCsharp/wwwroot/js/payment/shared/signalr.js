/**
 * Cria e gerencia uma conexão robusta com o Hub de Pagamento do SignalR.
 * Combina uma interface de controle limpa com lógica de reconexão automática.
 *
 * @param {Function} onStatusUpdate - Callback a ser executado quando um status é recebido do Hub.
 * @returns {object} Um objeto com métodos para controlar a conexão (start, stop, subscribe).
 */
export function createPaymentHubConnection(onStatusUpdate) {
    // 1. Construção da conexão (comum a ambos)
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/paymentProcessingHub") // URL única para o Hub
        .configureLogging(signalR.LogLevel.Warning) // Loga apenas avisos e erros
        .build();

    // 2. Lógica de reconexão robusta (do seu módulo PIX)
    async function startConnection() {
        try {
            await connection.start();
            console.log("SignalR: Conectado ao Hub de Pagamentos.");
        } catch (err) {
            console.error("SignalR: Falha ao conectar, tentando novamente em 5 segundos.", err);
            setTimeout(startConnection, 5000);
        }
    }

    // 3. Handlers de eventos (comum a ambos)
    connection.on("PaymentStatusUpdate", onStatusUpdate);

    connection.onclose(async () => {
        console.warn("SignalR: Conexão perdida. Tentando reconectar...");
        await startConnection();
    });

    // 4. Interface de controle limpa (do seu módulo de Cartão de Crédito)
    //    Retorna um objeto que dá ao chamador controle total sobre o ciclo de vida.
    return {
        /** Inicia a conexão com o Hub. */
        start: startConnection,

        /** Encerra a conexão de forma segura. */
        stop: () => {
            if (connection.state === 'Connected') {
                return connection.stop();
            }
            return Promise.resolve();
        },

        /** Registra o usuário para receber atualizações de status de pagamento. */
        subscribe: (userId) => {
            if (connection.state === 'Connected') {
                return connection.invoke("SubscribeToPaymentStatus", userId);
            }
            return Promise.reject("Não foi possível se inscrever: a conexão não está ativa.");
        }
    };
}