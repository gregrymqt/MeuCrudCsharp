// --- Módulo de Cache Simples (usando sessionStorage) ---
const cacheService = {
    get: (key) => {
        const cachedData = sessionStorage.getItem(key);
        return cachedData ? JSON.parse(cachedData) : null;
    },
    set: (key, data) => {
        sessionStorage.setItem(key, JSON.stringify(data));
    }
};

// --- Funções "Privadas" do Módulo ---
async function fetchSubscriptionDetails() {
    const CACHE_KEY = 'userSubscriptionDetails';
    let data = cacheService.get(CACHE_KEY);
    if (data) return data; // Retorna do cache se existir

    // Chama o endpoint específico para os detalhes da assinatura
    const response = await fetch('/api/user-account/subscription-details');
    if (!response.ok) throw new Error('Falha ao buscar os detalhes da assinatura.');

    data = await response.json();
    cacheService.set(CACHE_KEY, data); // Salva no cache
    return data;
}

function renderSubscriptionPanel(subscription) {
    const container = document.getElementById('subscription-content');
    if (!container) return;

    // Se o usuário não tiver uma assinatura, exibe uma mensagem e encerra.
    if (!subscription) {
        container.innerHTML = `
            <h2>Gerenciar Assinatura</h2>
            <p>Você não possui uma assinatura ativa para gerenciar.</p>
        `;
        return;
    }

    const status = subscription.status?.toLowerCase() || 'desconhecido';
    const nextBillingDate = subscription.nextBillingDate ? new Date(subscription.nextBillingDate).toLocaleDateString('pt-BR') : 'N/A';

    // 1. Monta o HTML dos detalhes da assinatura
    const detailsHtml = `
        <div class="subscription-details">
            <div><strong>Plano:</strong> ${subscription.planName}</div>
            <div><strong>Valor:</strong> ${new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(subscription.amount)}</div>
            <div><strong>Status:</strong> <span class="status-badge status-${status}">${subscription.status}</span></div>
            <div><strong>Cartão:</strong> **** **** **** ${subscription.lastFourCardDigits}</div>
            <div><strong>Próxima Cobrança:</strong> ${nextBillingDate}</div>
        </div>
    `;

    // 2. Monta as opções do <select> e os itens do acordeão condicionalmente
    let actionOptions = '<option value="">-- Selecione uma ação --</option>';
    let actionForms = '';

    if (status === 'ativo' || status === 'autorizado') {
        actionOptions += `
            <option value="pause">Pausar Assinatura</option>
            <option value="cancel">Cancelar Assinatura</option>
        `;
    } else if (status === 'pausado') {
        actionOptions += `<option value="reactivate">Reativar Assinatura</option>`;
    }

    // 3. Monta o HTML final e injeta no container
    container.innerHTML = `
        <h2>Gerenciar Assinatura</h2>
        ${detailsHtml}
        <div class="accordion">
            <div class="accordion-item">
                <button class="accordion-header">Alterar cartão de pagamento</button>
                <div class="accordion-body">
                    <p>Para alterar o cartão principal, preencha os dados abaixo.</p>
                    <div id="primary-card-brick-container"></div>
                </div>
            </div>
            <div class="accordion-item">
                <button class="accordion-header">Ações da Assinatura</button>
                <div class="accordion-body">
                    <div class="form-group">
                        <label for="subscription-action-select">Selecione uma Ação</label>
                        <select id="subscription-action-select" class="form-select">${actionOptions}</select>
                    </div>
                    <!-- Formulários de ação serão adicionados aqui se necessário -->
                </div>
            </div>
             <div class="accordion-item">
                <button class="accordion-header">Solicitar Reembolso</button>
                <div class="accordion-body">
                    <p>Você pode solicitar o reembolso total em até 7 dias após a compra.</p>
                    <form id="form-request-refund" class="subscription-form">
                        <button type="submit" class="btn-danger">Entendo e Quero Solicitar o Reembolso</button>
                    </form>
                </div>
            </div>
        </div>
    `;
}

/**
 * Função principal exportada. Orquestra a busca e renderização do painel.
 */
export async function initializeSubscriptionPanel() {
    try {
        const data = await fetchSubscriptionDetails();
        renderSubscriptionPanel(data);
    } catch (error) {
        console.error('Falha ao inicializar o painel de assinatura:', error);
        const container = document.getElementById('subscription-content');
        if (container) {
            container.innerHTML = '<h2>Gerenciar Assinatura</h2><p style="color: var(--danger-color);">Não foi possível carregar os dados da sua assinatura.</p>';
        }
    }
}
