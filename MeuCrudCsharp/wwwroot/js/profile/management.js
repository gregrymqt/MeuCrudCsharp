import { initializeMercadoPago } from '../core/mercadoPagoService';
import { initializeSidebarNavigation, initializeAccordions } from './modules/ui/navigation.js';

// Importa os inicializadores dos nossos painéis de dados dinâmicos
import { initializeProfileCard } from './modules/ui/profileCard.js';
import { initializePaymentHistory } from './modules/ui/paymentHistory.js';
import { initializeSubscriptionPanel } from "./modules/ui/subscriptionPanel.js";

// ✅ CORRETO: Importa o Manager que contém TODA a lógica de interatividade da assinatura
import { initializeSubscriptionManager } from './modules/subscriptionManager.js';

/**
 * ✅ A função principal agora é 'async' para poder usar 'await'.
 * Isso garante que a renderização aconteça antes de anexar a lógica.
 */
async function main() {
    // 1. Inicializa componentes que não dependem de dados da API
    await initializeMercadoPago();
    initializeSidebarNavigation();
    initializeAccordions(); // Mantido para outros acordeões que não sejam da assinatura

    // 2. Inicia o carregamento e a renderização dos painéis que dependem da API
    await initializeProfileCard();
    await initializePaymentHistory();

    // Primeiro, renderiza o painel de assinatura e aguarda ele retornar os dados.
    const subscriptionData = await initializeSubscriptionPanel();

    // Segundo, se a renderização foi bem-sucedida (retornou dados),
    // passa esses dados para o Manager, que irá anexar toda a lógica de interatividade.
    initializeSubscriptionManager(subscriptionData);
}
// Garante que o script só rode após o carregamento completo do HTML
document.addEventListener('DOMContentLoaded', main);

