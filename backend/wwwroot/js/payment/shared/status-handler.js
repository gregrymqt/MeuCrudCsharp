/**
 * Cria uma função de callback padronizada para o SignalR.
 * Centraliza a lógica comum (checar 'isComplete', parar a conexão) e
 * executa callbacks específicos para cada etapa do processo.
 *
 * @param {object} config - Objeto de configuração com os callbacks.
 * @param {object} config.hubConnection - A instância da conexão do SignalR.
 * @param {function(string):void} config.onUpdate - Chamado a cada mensagem de status recebida.
 * @param {function(object):void} config.onSuccess - Chamado quando o pagamento é concluído com sucesso.
 * @param {function(object):void} config.onFailure - Chamado quando o pagamento é concluído com falha.
 * @returns {function(object):void} A função de callback pronta para ser usada pelo SignalR.
 */
export function createStatusHandler({ hubConnection, onUpdate, onSuccess, onFailure }) {
    return (data) => {
        console.log("Status recebido via SignalR:", data);

        // 1. Executa a cada atualização para dar feedback contínuo ao usuário
        if (onUpdate && data.message) {
            onUpdate(data.message);
        }

        // 2. Verifica se o processo foi concluído
        if (data.isComplete) {
            hubConnection.stop(); // Lógica comum: parar a conexão

            // 3. Executa a ação de sucesso ou falha específica do módulo
            if (data.status === 'approved' || data.status === 'pending') {
                if (onSuccess) onSuccess(data);
            } else {
                if (onFailure) onFailure(data);
            }
        }
    };
}