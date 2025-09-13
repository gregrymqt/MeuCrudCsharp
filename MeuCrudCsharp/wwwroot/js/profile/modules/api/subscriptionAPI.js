// /js/modules/api/subscriptionAPI.js

// 1. IMPORTA os serviços centrais que criamos.
//    Não há mais código duplicado de 'apiFetch' ou 'cacheService' aqui.
import apiService from '../../../core/apiService.js';
import cacheService from '../../../core/cacheService.js';

/**
 * Busca o histórico de pagamentos do usuário, utilizando o cacheService central.
 * @returns {Promise<any>}
 */
export async function fetchPaymentHistory() {
    const CACHE_KEY = 'userPaymentHistory';

    let data = cacheService.get(CACHE_KEY);
    if (data) return data;

    // USA o apiService.fetch importado.
    data = await apiService.fetch('/api/user-account/payment-history');

    // USA o cacheService importado (que agora tem tempo de expiração).
    cacheService.set(CACHE_KEY, data);
    return data;
}

/**
 * Busca as informações do cartão de perfil do usuário, utilizando o cacheService central.
 * @returns {Promise<any>}
 */
export async function fetchCardInfo() {
    const CACHE_KEY = 'profileCardInfo';

    let data = cacheService.get(CACHE_KEY);
    if (data) return data;

    data = await apiService.fetch('/api/user-account/card-info');

    cacheService.set(CACHE_KEY, data);
    return data;
}

/**
 * Busca os detalhes da assinatura do usuário, utilizando o cacheService central.
 * @returns {Promise<any>}
 */
export async function fetchSubscriptionDetails() {
    const CACHE_KEY = 'userSubscriptionDetails';

    let data = cacheService.get(CACHE_KEY);
    if (data) return data;

    data = await apiService.fetch('/api/user-account/subscription-details');

    cacheService.set(CACHE_KEY, data);
    return data;
}

/**
 * Envia um novo token de cartão para o back-end para atualizar a assinatura.
 * @param {object} payload - O corpo da requisição, geralmente contendo o newCardToken.
 * @returns {Promise<object>}
 */
export function updateSubscriptionCard(payload) {
    // Simplesmente chama o apiService.fetch com as opções corretas.
    return apiService.fetch('/api/user/card', {
        method: 'PUT',
        body: JSON.stringify(payload),
    });
}

/**
 * Envia uma requisição para solicitar o reembolso.
 * @returns {Promise<object|null>}
 */
export function requestRefund() {
    return apiService.fetch('/api/refunds/request-refund', {
        method: 'POST'
    });
}

/**
 * Função única para atualizar o status da assinatura.
 * @param {string} status - O novo status a ser enviado ('paused', 'authorized', 'cancelled').
 * @returns {Promise<object>}
 */
export function updateSubscriptionStatus(status) {
    return apiService.fetch('/api/user/status', {
        method: 'PUT',
        body: JSON.stringify({ status: status })
    });
}

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