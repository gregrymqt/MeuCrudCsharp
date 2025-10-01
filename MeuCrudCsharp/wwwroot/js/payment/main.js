import { init as initPixModule } from './pix/pix.js';
import { init as initCreditCardModule } from './credit-card/credit-card.js';
import { init as initPaymentPage } from './Page/paymentPage.js';


document.addEventListener('DOMContentLoaded', () => {

    // Procura pelo módulo do PIX
    if (document.getElementById('pix-payment-module')) {
        console.log("Inicializando módulo PIX...");
        initPixModule();
    }
    // Se não achar o PIX, procura pelo módulo do Cartão de Crédito
    else if (document.getElementById('credit-card-module')) {
        console.log("Inicializando módulo Cartão de Crédito...");
        initCreditCardModule();
    }
    // Você pode adicionar 'else if' para outros métodos de pagamento aqui


    // Verificamos se estamos na página de pagamento antes de executar o código.
    // A presença do atributo 'data-plano-tipo' é um bom indicador.
    if (document.body.hasAttribute('data-plano-tipo')) {
        initPaymentPage();
    }

});