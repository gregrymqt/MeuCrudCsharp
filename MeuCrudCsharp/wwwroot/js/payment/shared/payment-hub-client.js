/**
 * Cria e gerencia uma conexão robusta com o Hub de Pagamento do SignalR.
 * Combina uma interface de controle limpa com lógica de reconexão automática e callbacks detalhados.
 *
 * @param {object} callbacks - Objeto com funções para lidar com os eventos do hub.
 * @returns {object|null} Um objeto com métodos para controlar a conexão (start, stop, invoke) ou null se o SignalR não estiver carregado.
 */
export function createPaymentHubConnection(callbacks) {
    if (typeof signalR === 'undefined') {
        callbacks.onFailure?.({ message: "Erro de comunicação. Recarregue a página." });
        return null;
    }

    const hubConnection = new signalR.HubConnectionBuilder()
        // IMPORTANTE: Use a URL do endpoint definida no seu Program.cs
        .withUrl("/PaymentProcessingHub")
        .configureLogging(signalR.LogLevel.Warning)
        .build();

    // --- Lógica de reconexão automática (do arquivo 1) ---
    async function startInternal() {
        callbacks.onConnecting?.();
        try {
            await hubConnection.start();
            console.log("SignalR: Conectado com sucesso.");
            callbacks.onConnected?.();
        } catch (err) {
            console.error("SignalR: Falha ao conectar. Tentando novamente em 5 segundos.", err);
            setTimeout(startInternal, 5000);
        }
    }

    // --- Sistema de callbacks rico (do arquivo 2) ---
    hubConnection.on("UpdatePaymentStatus", (statusUpdate) => {
        console.log("Hub: Status recebido:", statusUpdate);
        switch (statusUpdate.status.toLowerCase()) {
            case 'success':
            case 'approved':
                callbacks.onSuccess?.(statusUpdate);
                break;
            case 'failure':
            case 'rejected':
                callbacks.onFailure?.(statusUpdate);
                break;
            default: // 'processing', 'pending', etc.
                callbacks.onUpdate?.(statusUpdate.message);
                break;
        }
    });

    hubConnection.onclose(() => {
        console.warn("SignalR: Conexão perdida. Tentando reconectar...");
        callbacks.onConnectionClosed?.();
        startInternal(); // Tenta reconectar automaticamente
    });

    // --- Retorna uma interface de controle limpa (do arquivo 1) ---
    // Em vez de retornar o 'hubConnection' inteiro, retornamos um objeto mais simples de usar.
    return {
        /** Inicia a conexão com o Hub. */
        start: startInternal,

        /** Encerra a conexão de forma segura. */
        stop: () => {
            if (hubConnection.state === 'Connected') {
                return hubConnection.stop();
            }
            return Promise.resolve();
        },

        /**
         * Invoca um método no Hub do servidor.
         * @param {string} methodName - O nome do método no Hub C#.
         * @param {...any} args - Os argumentos para o método.
         */
        invoke: (methodName, ...args) => {
            if (hubConnection.state !== 'Connected') {
                console.error(`SignalR: Não foi possível invocar '${methodName}'. A conexão não está ativa.`);
                return Promise.reject(new Error("A conexão não está ativa."));
            }
            return hubConnection.invoke(methodName, ...args);
        }
    };
}