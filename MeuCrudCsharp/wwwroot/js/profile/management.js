// /js/main.js
import { initializeMercadoPago } from './modules/mercadopagoManager.js';
import { initializeTabNavigation, initializeAccordion } from './modules/ui/navigation.js';
import { initializeRefundForm } from './modules/ui/refundForm.js';

// Importa a função unificada que lida com todos os formulários de assinatura
import {
    initializeCardAccordions,
    initializeSubscriptionForms
} from './modules/subscriptionManager.js';

/**
 * Função principal que executa quando a página é carregada.
 */
function main() {
    // 1. Inicializa o SDK do Mercado Pago primeiro
    const mpReady = initializeMercadoPago();

    // 2. Inicializa os componentes de UI
    initializeTabNavigation();
    initializeAccordion();
    initializeRefundForm();

    // 3. Inicializa funcionalidades que dependem do SDK do MP
    if (mpReady) {
        initializeCardAccordions();
        initializeSubscriptionForms();
    }
}

// Garante que o script só rode após o carregamento completo do HTML
document.addEventListener('DOMContentLoaded', main);
