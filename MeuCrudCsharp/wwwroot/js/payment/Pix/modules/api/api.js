// /js/modules/api/pixAPI.js (ou o caminho/nome que preferir)

// 1. IMPORTA o serviço central de API.
import apiService from '../../../../core/apiService.js'; // Ajuste o caminho se necessário

/**
 * Busca a Public Key do Mercado Pago no back-end.
 * @returns {Promise<string>} A Public Key.
 */
export async function fetchPublicKey() {
    try {
        // 2. USA o apiService para a chamada GET.
        const data = await apiService.fetch('/api/payment/getpublickey');
        return data.publicKey;
    } catch (error) {
        console.error("Erro ao buscar a Public Key:", error.message);
        // Propaga o erro para que a lógica que chamou esta função possa tratá-lo.
        throw error;
    }
}

/**
 * Envia os dados do formulário para o back-end para criar um pagamento PIX.
 * @param {object} paymentData - Dados do pagamento (nome, email, valor, etc.).
 * @returns {Promise<object>} Os dados do PIX gerado (QR Code, etc.).
 */
export async function postCreatePayment(paymentData) {
    try {
        // 3. USA o apiService para a chamada POST, que lida com tudo automaticamente.
        return await apiService.fetch('/api/payment/createpix', {
            method: 'POST',
            body: JSON.stringify(paymentData)
        });
    } catch (error) {
        console.error("Erro ao criar o pagamento PIX:", error.message);
        // Propaga o erro para a UI poder mostrar uma mensagem ao usuário.
        throw error;
    }
}