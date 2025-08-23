// /js/modules/services/signalRService.js

/**
 * Cria e gerencia uma conexão com o Hub de Pagamento do SignalR.
 * @param {Function} onStatusUpdate - Callback para ser executado quando um status é recebido.
 * @returns {object} Um objeto com métodos para controlar a conexão (start, stop, subscribe).
 */
export function createPaymentHubConnection(onStatusUpdate) {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/paymentProcessingHub")
        .build();

    // Define o que fazer ao receber uma atualização
    connection.on("PaymentStatusUpdate", (data) => {
        onStatusUpdate(data);
    });

    // Encerra a conexão em caso de fechamento inesperado para limpeza
    connection.onclose(() => {
        console.log("Conexão com o Hub foi fechada.");
    });

    // Retorna uma "interface" para controlar a conexão de fora
    return {
        start: () => connection.start(),
        stop: () => connection.state === 'Connected' ? connection.stop() : Promise.resolve(),
        subscribe: (userId) => connection.invoke("SubscribeToPaymentStatus", userId),
        _connection: connection // Exposto para checagem de estado se necessário
    };
}