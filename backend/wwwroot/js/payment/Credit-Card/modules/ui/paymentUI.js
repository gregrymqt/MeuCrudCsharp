// Seletores de DOM para os elementos definidos no seu HTML
const DOMElements = {
    paymentContainer: document.getElementById('paymentBrick_container'),
    statusContainer: document.getElementById('statusScreenBrick_container'),
    errorContainer: document.getElementById('error-container'),
    loadingContainer: document.getElementById('loading-container'),
    mainModule: document.getElementById('credit-card-module'),
};

/** Exibe o formulário de pagamento e esconde o resto. */
export function showPaymentForm() {
    DOMElements.loadingContainer.style.display = 'none';
    DOMElements.paymentContainer.style.display = 'block';
    DOMElements.statusContainer.style.display = 'none';
    DOMElements.errorContainer.textContent = '';
    DOMElements.errorContainer.style.display = 'none';
}

/** Exibe a tela de status (pós-pagamento). */
export function showStatusScreen() {
    DOMElements.mainModule.innerHTML = ''; // Limpa tudo para dar espaço à tela de status
    DOMElements.mainModule.appendChild(DOMElements.statusContainer);
    DOMElements.statusContainer.style.display = 'block';
}


 // Controla a exibição do indicador de carregamento.
export function showLoading(show, message = 'Processando...') {
    if (show) {
        DOMElements.loadingContainer.querySelector('p').textContent = message;
        DOMElements.loadingContainer.style.display = 'flex'; // Usar flex para centralizar
        DOMElements.paymentContainer.style.display = 'none';
        DOMElements.statusContainer.style.display = 'none';
    } else {
        DOMElements.loadingContainer.style.display = 'none';
    }
}

/** Exibe uma mensagem de erro. */
export function showError(message) {
    DOMElements.errorContainer.textContent = message;
    DOMElements.errorContainer.style.display = 'block';
    // Esconde o loading e reexibe o formulário para o usuário tentar novamente
    showLoading(false);
    DOMElements.paymentContainer.style.display = 'block';
    console.error(message);
}