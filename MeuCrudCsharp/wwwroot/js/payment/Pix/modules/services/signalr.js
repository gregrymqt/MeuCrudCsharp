// js/modules/signalr.js

/**
 * Cria e gerencia uma conexão com o Hub de processamento de pagamentos.
 * @param {function} onStatusUpdate - Callback a ser executado ao receber uma atualização de status.
 * @returns {object} Uma interface para controlar a conexão.
 */
export function createPaymentHubConnection(onStatusUpdate) {
    // Garante que a biblioteca SignalR esteja disponível
    if (!window.signalR) {
        throw new Error("A biblioteca do SignalR não foi carregada. Verifique a inclusão do script.");
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/paymentProcessingHub") // O mesmo Hub do Cartão de Crédito
        .withAutomaticReconnect() // Adiciona resiliência à conexão
        .build();

    // Define o que fazer ao receber uma atualização
    connection.on("PaymentStatusUpdate", (data) => {
        console.log("Status recebido via SignalR:", data);
        onStatusUpdate(data);
    });

    // Encerra a conexão em caso de fechamento inesperado para limpeza
    connection.onclose((error) => {
        console.log("Conexão com o Hub foi fechada.", error);
    });

    // Retorna uma "interface" para controlar a conexão de fora
    return {
        start: () => connection.start(),
        stop: () => connection.state === 'Connected' ? connection.stop() : Promise.resolve(),
        subscribe: (userId) => connection.invoke("SubscribeToPaymentStatus", userId),
    };
}