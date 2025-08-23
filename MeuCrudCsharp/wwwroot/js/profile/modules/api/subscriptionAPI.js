// /js/modules/api/subscriptionAPI.js

/**
 * Envia um novo token de cartão para o back-end para atualizar a assinatura.
 * @param {object} payload - O corpo da requisição, geralmente contendo o newCardToken.
 * @returns {Promise<object>} - A resposta JSON do servidor.
 */
export async function updateSubscriptionCard(payload) {
    const response = await fetch('/api/user/subscription/card', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
    });

    if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.message || 'Falha ao atualizar a assinatura.');
    }
    return response.json();
}

/**
 * Envia uma requisição para reativar a assinatura do usuário.
 * @returns {Promise<object>} - A resposta JSON do servidor.
 */
export async function reactivateSubscription() {
    const response = await fetch('/api/user/subscription/reactivate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
    });

    if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.message || 'Não foi possível reativar a assinatura.');
    }
    return response.json();
}

/**
 * Envia uma requisição para solicitar o reembolso.
 * @returns {Promise<object>} - A resposta JSON do servidor.
 */
export async function requestRefund() {
    const response = await fetch('/api/profile/request-refund', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
    });

    if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.message || 'Ocorreu um erro ao solicitar o reembolso.');
    }
    // Retorna um objeto de sucesso, já que o corpo da resposta pode estar vazio em um 200 OK.
    return { success: true };
}