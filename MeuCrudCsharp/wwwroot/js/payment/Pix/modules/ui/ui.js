/**
 * Renderiza o formulário inicial de dados do pagador.
 * @param {HTMLElement} container - O elemento onde o formulário será renderizado.
 */
export function renderForm(container) {
    container.innerHTML = `
        <form id="form-checkout" novalidate>
            <div class="form-group half-width">
                <label for="identificationType">Tipo de documento</label>
                <select id="pix-identification-type" name="identificationType" required></select>
            </div>
            </form>
    `;
}

/**
 * Popula um elemento <select> com opções.
 * @param {string} selectId - O ID do elemento select.
 * @param {Array<object>} options - Array de opções vindo do Mercado Pago.
 */
export function populateIdentificationTypes(selectId, options) {
    const selectElement = document.getElementById(selectId);
    if (!selectElement) return;

    let html = '';
    options.forEach(option => {
        html += `<option value="${option.id}">${option.name}</option>`;
    });
    selectElement.innerHTML = html;
}

/**
 * Renderiza a visualização de pagamento PIX (QR Code e código).
 * @param {HTMLElement} container - O elemento onde a visualização será renderizada.
 * @param {object} pixData - Dados retornados pelo backend ({ qrCodeBase64, qrCode }).
 */
export function renderPaymentView(container, pixData) {
    container.innerHTML = `
        <div id="pix-payment-details">
            <h2 class="pix-result-title">Pague para finalizar</h2>
            <p class="pix-result-subtitle">Escaneie o QR Code ou use o código abaixo.</p>
            <img id="pix-qr-code-image" src="data:image/jpeg;base64,${pixData.qrCodeBase64}" alt="PIX QR Code" class="qr-code">
            <div class="pix-copy-paste">
                <input type="text" id="pix-qr-code-text" value="${pixData.qrCode}" readonly>
                <button id="copy-pix-code-button" title="Copiar código PIX">
                    <i class="fas fa-copy"></i> <span>Copiar Código</span>
                </button>
            </div>
            <p class="pix-expiration-info">
                <i class="fas fa-info-circle"></i> Este código expira em 30 minutos.
            </p>
        </div>
    `;
    setupCopyButton();
}

/**
 * Renderiza a tela de status final do pagamento.
 * @param {HTMLElement} container - O elemento onde o status será renderizado.
 * @param {'approved' | 'pending' | 'rejected'} status - O status do pagamento.
 */
export function renderStatusView(container, status) {
    const statusMap = {
        approved: {
            icon: 'fa-check-circle',
            title: 'Pagamento Aprovado!',
            message: 'Obrigado! Sua assinatura está ativa. Você será redirecionado em breve.',
            buttonText: 'Acessar minha conta',
            buttonUrl: '/dashboard'
        },
        // Adicione outros status se necessário
        rejected: {
            icon: 'fa-times-circle',
            title: 'Pagamento Rejeitado',
            message: 'Houve um problema ao processar seu pagamento. Por favor, tente novamente.',
            buttonText: 'Tentar Novamente',
            buttonUrl: '/pagamento?plano=mensal' // ou a lógica para reiniciar
        }
    };

    const currentStatus = statusMap[status] || statusMap.rejected;

    container.innerHTML = `
        <div id="payment-status-screen" class="status-${status}">
            <div class="status-icon">
                <i class="fas ${currentStatus.icon}"></i>
            </div>
            <h2 class="status-title">${currentStatus.title}</h2>
            <p class="status-message">${currentStatus.message}</p>
            <a href="${currentStatus.buttonUrl}" class="status-button">${currentStatus.buttonText}</a>
        </div>
    `;
}

/** Adiciona um listener ao botão de copiar código PIX. */
function setupCopyButton() {
    const copyButton = document.getElementById('copy-pix-code-button');
    if (!copyButton) return;

    copyButton.addEventListener('click', () => {
        const qrCodeInput = document.getElementById('pix-qr-code-text');
        navigator.clipboard.writeText(qrCodeInput.value).then(() => {
            const buttonText = copyButton.querySelector('span');
            buttonText.textContent = 'Copiado!';
            copyButton.querySelector('i').classList.replace('fa-copy', 'fa-check');

            setTimeout(() => {
                buttonText.textContent = 'Copiar Código';
                copyButton.querySelector('i').classList.replace('fa-check', 'fa-copy');
            }, 2000);
        });
    });
}