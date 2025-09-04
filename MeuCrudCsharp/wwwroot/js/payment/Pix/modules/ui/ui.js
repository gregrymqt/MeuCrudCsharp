// js/modules/ui.js

/**
 * Popula um elemento <select> com opções.
 * @param {HTMLElement} selectElement - O elemento select.
 * @param {Array<object>} options - O array de opções vindo da API do MP.
 */
export function createSelectOptions(selectElement, options) {
    if (!selectElement) return;
    selectElement.options.length = 0; // Limpa opções existentes

    const fragment = document.createDocumentFragment();
    options.forEach(option => {
        const opt = document.createElement('option');
        opt.value = option.id;
        opt.textContent = option.name;
        fragment.appendChild(opt);
    });
    selectElement.appendChild(fragment);
}

/**
 * Exibe a seção de pagamento PIX com o QR Code e o código.
 * @param {object} pixData - Dados retornados pelo backend ({ qrCodeBase64, qrCode }).
 */
export function displayPixPayment(pixData) {
    document.getElementById('form-checkout').style.display = 'none'; // Esconde o formulário
    document.getElementById('pix-payment-container').style.display = 'block'; // Mostra a área do PIX

    const qrCodeImage = document.getElementById('pix-qr-code-image');
    qrCodeImage.src = `data:image/jpeg;base64,${pixData.qrCodeBase64}`;

    const qrCodeInput = document.getElementById('pix-qr-code-text');
    qrCodeInput.value = pixData.qrCode;
}

/**
 * Controla a visibilidade do loader e o estado do botão.
 * @param {boolean} isLoading - Se deve mostrar o loader ou não.
 */
export function setLoading(isLoading) {
    const loader = document.getElementById('loading-spinner');
    const submitButton = document.querySelector('#form-checkout button[type="submit"]');

    if (isLoading) {
        loader.style.display = 'block';
        submitButton.disabled = true;
        submitButton.textContent = 'Processando...';
    } else {
        loader.style.display = 'none';
        submitButton.disabled = false;
        submitButton.textContent = 'Criar Pagamento PIX';
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
}

/**
 * Adiciona um listener ao botão de copiar código PIX.
 */
export function setupCopyButton() {
    const copyButton = document.getElementById('copy-pix-code-button');
    copyButton.addEventListener('click', () => {
        const qrCodeInput = document.getElementById('pix-qr-code-text');
        navigator.clipboard.writeText(qrCodeInput.value).then(() => {
            copyButton.innerHTML = '<i class="fas fa-check"></i> Copiado!';
            setTimeout(() => {
                copyButton.innerHTML = '<i class="fas fa-copy"></i> Copiar Código';
            }, 2000);
        });
    });
}

/**
 * Exibe a tela de sucesso final, escondendo as outras seções.
 * @param {string} paymentId - O ID do pagamento para referência (opcional).
 */
export function showSuccessScreen(paymentId) {
    document.getElementById('form-checkout').style.display = 'none';
    document.getElementById('pix-payment-container').style.display = 'none';

    const successScreen = document.getElementById('success-screen');
    successScreen.style.display = 'flex'; // Usamos flex para centralizar o conteúdo

    console.log(`Pagamento ${paymentId} aprovado! Exibindo tela de sucesso.`);
}