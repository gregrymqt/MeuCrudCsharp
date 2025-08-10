/**
 * @file Fetches and displays subscription plans on the page.
 *
 * This script runs when the DOM is loaded. It fetches a list of subscription plans
 * from a backend API, dynamically generates HTML cards for each plan, and
 * injects them into the page. It also handles loading states and potential errors.
 */
document.addEventListener('DOMContentLoaded', () => {
    const plansContainer = document.getElementById('plans-grid-container');
    const loader = document.getElementById('plans-loader');

    /**
     * Creates the HTML string for a single plan card.
     * @param {object} plan - The plan object from the API.
     * @param {string} plan.name - The name of the plan (e.g., "Monthly").
     * @param {string} plan.priceDisplay - The formatted price string (e.g., "$49.90").
     * @param {string} plan.billingInfo - A short description of the billing cycle (e.g., "Billed monthly").
     * @param {string[]} plan.features - An array of features included in the plan.
     * @param {string} plan.slug - A unique identifier for the plan used in the URL (e.g., "monthly").
     * @param {boolean} [plan.isRecommended=false] - Flag to highlight the plan as recommended.
     * @returns {string} The HTML string for the plan card.
     */
    function createPlanCardHTML(plan) {
        // Add a specific class and a badge if the plan is marked as recommended.
        const isRecommended = plan.isRecommended ? 'recommended' : '';
        const recommendationBadge = plan.isRecommended ? '<div class="recommendation-badge">MOST POPULAR</div>' : '';
        const buttonText = plan.isRecommended ? 'Save with Annual' : 'Subscribe Now';

        // Split the price string to style the main part and the cents differently.
        // Example: "$49.90" -> ["$49", "90"]
        const priceParts = plan.priceDisplay.split(',');
        const mainPrice = priceParts[0];
        const cents = priceParts.length > 1 ? `,${priceParts[1]}` : '';
        // The suffix is currently the same, but this allows for future flexibility.
        const priceSuffix = plan.slug === 'mensal' ? '/month' : '/month';

        // Generate the list of features with checkmark icons.
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
     * Fetches plans from the API and renders the cards on the page.
     * Handles loading and error states.
     */
    async function loadPlans() {
        try {
            const response = await fetch('/api/plans');
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            const plans = await response.json();

            // Render plans if available, otherwise show a message.
            if (plans && plans.length > 0) {
                const plansHTML = plans.map(createPlanCardHTML).join('');
                plansContainer.innerHTML = plansHTML;
            } else {
                plansContainer.innerHTML = '<p class="error-message">No plans available at the moment.</p>';
            }

        } catch (error) {
            console.error('Failed to fetch plans:', error);
            plansContainer.innerHTML = '<p class="error-message">Could not load plans. Please try again later.</p>';
        } finally {
            // Always remove the loader, even if an error occurred.
            if (loader) {
                loader.remove();
            }
        }
    }

    // Start the process when the page loads.
    loadPlans();
});
