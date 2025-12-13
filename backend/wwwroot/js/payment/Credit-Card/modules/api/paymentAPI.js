import apiService from '../../../../core/apiService.js'; // Ajuste o caminho conforme necessário

/**
 * Envia os dados do formulário de pagamento para o back-end.
 * @param {object} formData - Os dados do formulário coletados pelo Brick do Mercado Pago.
 * @param {string} processUrl - A URL do endpoint do backend para processar o pagamento.
 * @returns {Promise<object>} A resposta da API.
 */
export function processPayment(formData, processUrl) {
    // A função agora usa a URL que recebeu como parâmetro.
    // Ela não sabe e não precisa saber de onde essa URL veio.
    return apiService.fetch(processUrl, {
        method: "POST",
        body: JSON.stringify(formData)
    });
}