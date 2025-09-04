// /js/modules/ui/paymentUI.js

// Seletores de DOM centralizados
const DOMElements = {
    paymentContainer: document.getElementById('paymentBrick_container'),
    statusContainer: document.getElementById('statusScreenBrick_container'),
    errorContainer: document.getElementById('error-container'),
    loadingMessage: document.getElementById('loading-message'),
};

/** Exibe o formulário de pagamento e esconde o resto. */
export function showPaymentForm() {
    DOMElements.paymentContainer.style.display = 'block';
    DOMElements.statusContainer.style.display = 'none';
    DOMElements.errorContainer.style.display = 'none';
    DOMElements.loadingMessage.style.display = 'none';
}

/** Exibe a tela de status e esconde o resto. */
export function showStatusScreen() {
    DOMElements.paymentContainer.style.display = 'none';
    DOMElements.statusContainer.style.display = 'block';
    DOMElements.errorContainer.style.display = 'none';
    DOMElements.loadingMessage.style.display = 'none';
}

/** Exibe uma mensagem de carregamento/processamento. */
export function showLoading(message) {
    DOMElements.loadingMessage.querySelector('p').textContent = message;
    DOMElements.loadingMessage.style.display = 'block';
    DOMElements.paymentContainer.style.display = 'none';
    DOMElements.statusContainer.style.display = 'none';
    DOMElements.errorContainer.style.display = 'none';
}

/** Exibe uma mensagem de erro e mostra o formulário de pagamento novamente. */
export function showError(message) {
    DOMElements.errorContainer.textContent = message;
    DOMElements.errorContainer.style.display = 'block';
    DOMElements.loadingMessage.style.display = 'none';
    DOMElements.paymentContainer.style.display = 'block'; // Permite tentar de novo
    DOMElements.statusContainer.style.display = 'none';
    console.error(message);
}