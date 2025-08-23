// /js/modules/ui/planCard.js

/**
 * Gera a string HTML para um único card de plano.
 * @param {object} plan - O objeto do plano contendo nome, preço, features, etc.
 * @returns {string} A string HTML do card.
 */
export function createPlanCardHTML(plan) {
    const isRecommended = plan.isRecommended ? 'recommended' : '';
    const recommendationBadge = plan.isRecommended ? '<div class="recommendation-badge">MAIS POPULAR</div>' : '';
    const buttonText = plan.isRecommended ? 'Economize com o Anual' : 'Assinar Agora';

    // Separa o preço para estilização (ex: "R$49,90" -> ["R$49", "90"])
    const priceParts = plan.priceDisplay.split(',');
    const mainPrice = priceParts[0];
    const cents = priceParts.length > 1 ? `,${priceParts[1]}` : '';
    const priceSuffix = '/mês'; // Pode ser ajustado conforme o plano no futuro

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