// /js/plans-main.js

import { fetchPlans } from './modules/api/plansAPI.js';
import { createPlanCardHTML } from './modules/ui/planCard.js';

/**
 * Orquestra o carregamento e a renderização dos planos na página.
 */
async function initializePlansPage() {
    const plansContainer = document.getElementById('plans-grid-container');
    const loader = document.getElementById('plans-loader');

    try {
        // 1. Busca os dados da API
        const plans = await fetchPlans();

        // 2. Renderiza os planos ou uma mensagem de "nenhum plano encontrado"
        if (plans && plans.length > 0) {
            plansContainer.innerHTML = plans.map(createPlanCardHTML).join('');
        } else {
            plansContainer.innerHTML = '<p class="error-message">Nenhum plano disponível no momento.</p>';
        }

    } catch (error) {
        console.error('Falha ao carregar os planos:', error);
        plansContainer.innerHTML = '<p class="error-message">Não foi possível carregar os planos. Tente novamente mais tarde.</p>';
    } finally {
        // 3. Remove o loader, independentemente do resultado
        loader?.remove(); // O '?' evita erro se o loader já tiver sido removido
    }
}

// Garante que o script só rode após o carregamento completo do HTML
document.addEventListener('DOMContentLoaded', initializePlansPage);