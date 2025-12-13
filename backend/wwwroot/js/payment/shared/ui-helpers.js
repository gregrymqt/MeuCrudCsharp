/**
 * Controla a visibilidade do loader e o estado do botão.
 * @param {boolean} isLoading - Se deve mostrar o loader ou não.
 * @param {string} [buttonSelector] - O seletor do botão a ser desabilitado/reabilitado.
 */
export function setLoading(isLoading, buttonSelector) {
    const loader = document.getElementById('loading-spinner');
    const submitButton = buttonSelector ? document.querySelector(buttonSelector) : null;

    if (isLoading) {
        loader.style.display = 'block';
        if (submitButton) {
            submitButton.disabled = true;
            submitButton.innerHTML = '<span class="button-loading-text">Processando...</span>';
        }
    } else {
        loader.style.display = 'none';
        if (submitButton) {
            submitButton.disabled = false;
            // O texto original deve ser restaurado pelo módulo que o chamou
        }
    }
}

/**
 * Exibe uma mensagem de erro no formulário.
 * @param {string} message - A mensagem de erro.
 */
export function displayError(message) {
    const errorElement = document.getElementById('form-error');
    errorElement.textContent = message;
    errorElement.style.display = 'block';
    setTimeout(() => {
        errorElement.style.display = 'none';
    }, 5000); // Esconde o erro após 5 segundos
}

/**
 * Atualiza o estado visual das abas de navegação.
 * @param {number} currentStep - O número da aba que deve ficar ativa.
 */
export function updateTabState(currentStep) {
    const tabs = document.querySelectorAll('.payment-tabs-nav .tab-link');
    tabs.forEach(tab => {
        const tabStep = parseInt(tab.dataset.step, 10);
        tab.classList.remove('active');

        if (tabStep < currentStep) {
            tab.classList.remove('disabled');
        } else if (tabStep === currentStep) {
            tab.classList.add('active');
            tab.classList.remove('disabled');
        } else {
            tab.classList.add('disabled');
        }
    });
}