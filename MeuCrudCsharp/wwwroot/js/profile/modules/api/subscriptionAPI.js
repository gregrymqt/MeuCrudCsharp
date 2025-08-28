// /js/modules/api/subscriptionAPI.js

/**
 * NOVO: Função central e automática para todas as chamadas de API deste módulo.
 * Pega o token automaticamente e lida com toda a lógica de requisição e resposta.
 * @param {string} url - O endpoint da API.
 * @param {object} options - Opções do fetch (method, body, etc.).
 * @returns {Promise<any>} - A resposta da API em formato JSON.
 */
async function apiFetch(url, options = {}) {

    // 2. Configura os cabeçalhos.
    const headers = { ...options.headers };

    if (options.body && (options.method === 'POST' || options.method === 'PUT')) {
        headers['Content-Type'] = 'application/json';
    }

    // 4. Realiza a chamada fetch
    const response = await fetch(url, {
        ...options,
        headers,
        credentials: 'include',
    });

    // 5. Lida com a resposta
    // Lida com sucesso sem corpo de resposta (ex: 204 No Content)
    if (response.status === 204) {
        return null;
    }

    const contentType = response.headers.get("content-type");
    if (!contentType || !contentType.includes("application/json")) {
        const text = await response.text();
        console.error("Resposta inesperada do servidor:", text);
        throw new Error(`O servidor respondeu com um formato inesperado (não-JSON). Status: ${response.status}`);
    }

    const data = await response.json();
    
    if (!response.ok) {
        const errorMessage = data.message || `Erro na API. Status: ${response.status}`;
        throw new Error(errorMessage);
    }

    return data;
}

/**
 * REATORADO: Envia um novo token de cartão para o back-end para atualizar a assinatura.
 * @param {object} payload - O corpo da requisição, geralmente contendo o newCardToken.
 * @returns {Promise<object>} - A resposta JSON do servidor.
 */
export function updateSubscriptionCard(payload) {
    return apiFetch('/api/user/card', {
        method: 'PUT',
        body: JSON.stringify(payload),
    });
}

/**
 * REATORADO: Envia uma requisição para solicitar o reembolso.
 * @returns {Promise<object|null>} - A resposta do servidor.
 */
export function requestRefund() {
    return apiFetch('/api/refunds/request-refund', {
        method: 'POST'
    });
}


/**
 *  Função única para atualizar o status da assinatura.
 * @param {string} status - O novo status a ser enviado ('paused', 'authorized', 'cancelled').
 * @returns {Promise<object>} - A resposta JSON do servidor.
 */
export function updateSubscriptionStatus(status) {
    return apiFetch('/api/user/status', {
        method: 'PUT',
        body: JSON.stringify({ status: status })
    });
}