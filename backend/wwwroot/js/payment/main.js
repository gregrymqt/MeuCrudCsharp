// Importa os 'inicializadores' de cada módulo
import { init as initPixModule } from './pix/pix.js';
import { init as initCreditCardModule } from './Credit-Card/credit-card.js';
import { init as initPaymentPage } from './Page/paymentPage.js';

document.addEventListener('DOMContentLoaded', () => {
    // A presença do atributo 'data-plano-tipo' é um bom indicador
    // de que estamos na página de pagamento.
    if (document.body.hasAttribute('data-plano-tipo')) {

        // Inicializa o módulo do PIX se ele existir
        if (document.getElementById('pix-payment-module')) {
             initPixModule(); // Você implementará a lógica do PIX aqui
        }

        // Inicializa o setup do Cartão de Crédito se ele existir
        if (document.querySelector('[id*="credit-card"]')) {
            initCreditCardModule();
        }

        // **Inicializa o gerenciador da página, que controla a sidebar**
        initPaymentPage();
    }
});