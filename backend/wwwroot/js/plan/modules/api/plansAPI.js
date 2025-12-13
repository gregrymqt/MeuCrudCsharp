import apiService from '../../../core/apiService.js';

/**
 * Busca a lista de planos de assinatura públicos da API de forma paginada.
 * @param {number} page - O número da página a ser buscada.
 * @param {number} pageSize - A quantidade de planos por página.
 * @returns {Promise<object>} Uma promessa que resolve com o objeto de resultado paginado.
 * @throws {Error} Lança um erro se a requisição de rede falhar.
 */
export function fetchPlans(page, pageSize) {
    // Adiciona os parâmetros de paginação como query string na URL
    return apiService.fetch(`/api/public/plans?page=${page}&pageSize=${pageSize}`);
}