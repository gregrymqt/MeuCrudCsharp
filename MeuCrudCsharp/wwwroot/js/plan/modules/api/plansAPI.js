// /js/modules/api/plansAPI.js

/**
 * Busca a lista de planos de assinatura da API.
 * @returns {Promise<Array>} Uma promessa que resolve com a lista de planos.
 * @throws {Error} Lança um erro se a requisição de rede falhar.
 */
export async function fetchPlans() {
    const response = await fetch('/api/plans');

    if (!response.ok) {
        throw new Error(`Erro na API! Status: ${response.status}`);
    }

    return await response.json();
}