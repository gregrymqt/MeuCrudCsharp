// /js/modules/api/paymentAPI.js

/**
 * Envia os dados do formulário de pagamento para o back-end para iniciar o processamento.
 * @param {object} formData - Os dados do formulário coletados pelo Brick.
 * @returns {Promise<object>} A resposta da API.
 */
export async function processPayment(formData) {
    const response = await fetch(window.paymentConfig.processPaymentUrl, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(formData)
    });

    if (!response.ok) {
        const err = await response.json();
        throw new Error(err.message || "Falha ao iniciar o processo de pagamento.");
    }

    return response.json();
}