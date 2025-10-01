// /js/modules/api/pixAPI.js (ou o caminho/nome que preferir)

// 1. IMPORTA o serviço central de API.
import apiService from '../../../../core/apiService.js'; // Ajuste o caminho se necessário

/**
 * Envia os dados do formulário para o back-end para criar um pagamento PIX.
 * @param {object} paymentData - Dados do pagamento (nome, email, valor, etc.).
 * @returns {Promise<object>} Os dados do PIX gerado (QR Code, etc.).
 */
export async function createPixPayment(paymentData) {
    try {
        // 3. USA o apiService para a chamada POST, que lida com tudo automaticamente.
        return await apiService.fetch('/api/pix/createpix', {
            method: 'POST',
            body: JSON.stringify(paymentData)
        });
    } catch (error) {
        console.error("Erro ao criar o pagamento PIX:", error.message);
        // Propaga o erro para a UI poder mostrar uma mensagem ao usuário.
        throw error;
    }
}