// /js/modules/api/plansAPI.js

// 1. IMPORTA o serviço central de API.
//    Toda a lógica complexa de comunicação com o back-end virá daqui.
import apiService from '../../../core/apiService.js'; // Ajuste o caminho se sua estrutura de pastas for diferente

/**
 * Busca a lista de planos de assinatura públicos da API.
 * @returns {Promise<Array>} Uma promessa que resolve com a lista de planos.
 * @throws {Error} Lança um erro se a requisição de rede falhar.
 */
export function fetchPlans() {
    // 2. USA o apiService.fetch para fazer a chamada. Simples e direto.
    return apiService.fetch('/api/public/plans');
}

/**
 * Busca os detalhes de um plano específico.
 * @param {string} planId - O ID do plano.
 * @returns {Promise<object>} O objeto do plano.
 */
export function getPlanDetails(planId) {
    // Também usa o apiService, mantendo a consistência.
    return apiService.fetch(`/api/plans/${planId}`);
}