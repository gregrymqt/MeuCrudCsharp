// Recomendo renomear este arquivo para: /js/modules/api/userAccountAPI.js

import apiService from '../../../core/apiService.js';
import cacheService from '../../../core/cacheService.js';


export async function fetchPaymentHistory() {
    const CACHE_KEY = 'userPaymentHistory';
    let data = cacheService.get(CACHE_KEY);
    if (data) return data;

    // Rota continua correta.
    data = await apiService.fetch('/api/user-account/payment-history');
    cacheService.set(CACHE_KEY, data);
    return data;
}

export async function fetchProfileSummary() {
    const CACHE_KEY = 'profileSummary'; 

    let data = cacheService.get(CACHE_KEY);
    if (data) return data;

    data = await apiService.fetch('/api/user-account/profile-summary');
    cacheService.set(CACHE_KEY, data);
    return data;
}

export async function fetchSubscriptionDetails() {
    const CACHE_KEY = 'userSubscriptionDetails';
    let data = cacheService.get(CACHE_KEY);
    if (data) return data;

    data = await apiService.fetch('/api/user-account/subscription-details');
    cacheService.set(CACHE_KEY, data);
    return data;
}

export function updateSubscriptionCard(payload) {
    // ✅ ROTA ATUALIZADA: de '/api/user/card' para a nova rota unificada.
    return apiService.fetch('/api/user-account/subscription/card', {
        method: 'PUT',
        body: JSON.stringify(payload),
    });
}

export function requestRefund() {
    // ✅ ROTA ATUALIZADA: de '/api/refunds/request-refund' para a nova rota unificada.
    return apiService.fetch('/api/user-account/refunds/request', {
        method: 'POST'
    });
}

export function updateSubscriptionStatus(status) {
    // ✅ ROTA ATUALIZADA: de '/api/user/status' para a nova rota unificada.
    return apiService.fetch('/api/user-account/subscription/status', {
        method: 'PUT',
        body: JSON.stringify({ status: status })
    });
}