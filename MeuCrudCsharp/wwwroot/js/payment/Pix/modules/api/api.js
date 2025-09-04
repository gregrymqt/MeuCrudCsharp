// js/modules/api.js

/**
 * Busca a Public Key do Mercado Pago no seu backend.
 * @returns {Promise<string>} A Public Key.
 */
export async function fetchPublicKey() {
    try {
        const response = await fetch('/api/payment/getpublickey', {
            credentials: "include",
        }); // Endpoint de exemplo no seu backend
        
        if (!response.ok) {
            throw new Error('Falha ao buscar a chave pública.');
        }
        const data = await response.json();
        return data.publicKey;
    } catch (error) {
        console.error("Erro na API (fetchPublicKey):", error);
        throw error; // Propaga o erro para ser tratado no fluxo principal
    }
}

/**
 * Envia os dados do formulário para o seu backend para criar o pagamento PIX.
 * @param {object} paymentData - Dados do pagamento (nome, email, valor, etc.).
 * @returns {Promise<object>} Os dados do PIX gerado (QR Code, etc.).
 */
export async function postCreatePayment(paymentData) {
    try {
        const response = await fetch('/api/payment/createpix', { // Endpoint de exemplo
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(paymentData),
            credentials: 'include',
        });

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.message || 'Não foi possível processar o pagamento.');
        }

        return await response.json();
    } catch (error) {
        console.error("Erro na API (postCreatePayment):", error);
        throw error;
    }
}