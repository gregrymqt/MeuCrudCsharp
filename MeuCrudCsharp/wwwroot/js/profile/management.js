// /js/main.js

import { initializeMercadoPago } from './modules/mercadopagoManager.js';
import { initializeTabNavigation } from './modules/ui/navigation.js';
import { initializeRefundForm } from './modules/ui/refundForm.js';
import { initializeCardAccordions, initializeReactivationForm } from './modules/subscriptionManager.js';

// Função principal que executa quando a página é carregada
function main() {
    // 1. Inicializa o SDK do Mercado Pago primeiro
    const mpReady = initializeMercadoPago();

    // 2. Inicializa os componentes de UI
    initializeTabNavigation();
    initializeRefundForm();

    // 3. Inicializa funcionalidades que dependem do SDK do MP
    if (mpReady) {
        initializeCardAccordions();
        initializeReactivationForm();
    }
}

// Garante que o script só rode após o carregamento completo do HTML
document.addEventListener('DOMContentLoaded', main);