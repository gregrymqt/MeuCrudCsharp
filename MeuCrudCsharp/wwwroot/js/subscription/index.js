document.addEventListener('DOMContentLoaded', () => {
    const plansContainer = document.getElementById('plans-grid-container');
    const loader = document.getElementById('plans-loader');

    /**
     * Cria o HTML para um único card de plano.
     * @param {object} plan - O objeto do plano vindo da API.
     * @returns {string} O HTML do card.
     */
    function createPlanCardHTML(plan) {
        const isRecommended = plan.isRecommended ? 'recommended' : '';
        const recommendationBadge = plan.isRecommended ? '<div class="recommendation-badge">MAIS POPULAR</div>' : '';
        const buttonText = plan.isRecommended ? 'Economize com o Anual' : 'Assinar Agora';

        // Divide o preço para estilização (ex: "R$ 49,90" -> ["R$ 49", "90"])
        const priceParts = plan.priceDisplay.split(',');
        const mainPrice = priceParts[0];
        const cents = priceParts.length > 1 ? `,${priceParts[1]}` : '';
        const priceSuffix = plan.slug === 'mensal' ? '/mês' : '/mês';

        // Gera a lista de features
        const featuresHTML = plan.features.map(feature => `<li><i class="fas fa-check-circle"></i> ${feature}</li>`).join('');

        return `
            <div class="plan-card ${isRecommended}">
                ${recommendationBadge}
                <h2 class="plan-title">${plan.name}</h2>
                <div class="plan-price">${mainPrice}<span>${cents}${priceSuffix}</span></div>
                <p class="plan-billing-info">${plan.billingInfo}</p>
                <ul class="plan-features">
                    ${featuresHTML}
                </ul>
                <a href="/Payment/CreditCard?plano=${plan.slug}" class="btn-choose-plan">${buttonText}</a>
            </div>
        `;
    }

    /**
     * Busca os planos da API e renderiza os cards na página.
     */
    async function loadPlans() {
        try {
            const response = await fetch('/api/plans');
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            const plans = await response.json();

            if (plans && plans.length > 0) {
                const plansHTML = plans.map(createPlanCardHTML).join('');
                plansContainer.innerHTML = plansHTML;
            } else {
                plansContainer.innerHTML = '<p class="error-message">Nenhum plano disponível no momento.</p>';
            }

        } catch (error) {
            console.error('Falha ao buscar os planos:', error);
            plansContainer.innerHTML = '<p class="error-message">Não foi possível carregar os planos. Tente novamente mais tarde.</p>';
        } finally {
            // Remove o loader, mesmo que tenha dado erro
            if (loader) {
                loader.remove();
            }
        }
    }

    // Inicia o processo ao carregar a página
    loadPlans();
});
