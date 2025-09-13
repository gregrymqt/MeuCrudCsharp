// /js/modules/api/paymentAPI.js

// 1. IMPORTA o serviço central de API.
//    Não há mais nenhuma lógica de fetch duplicada neste arquivo.
import apiService from '../../../../core/apiService.js'; // Ajuste o caminho se sua estrutura de pastas for diferente

/**
 * Envia os dados do formulário de pagamento para o back-end.
 * Utiliza o apiService central para garantir robustez e consistência na autenticação.
 * @param {object} formData - Os dados do formulário coletados pelo Brick do Mercado Pago.
 * @returns {Promise<object>} A resposta da API.
 */
export function processPayment(formData) {
    // 2. USA o apiService.fetch importado.
    // A chamada agora é uma linha, pois toda a complexidade (tokens, headers, erros)
    // é gerenciada pelo serviço central.
    return apiService.fetch(window.paymentConfig.processPaymentUrl, {
        method: "POST",
        body: JSON.stringify(formData)
    });
}