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
async function fetchCardInfo() {
    const CACHE_KEY = 'profileCardInfo';
    let data = cacheService.get(CACHE_KEY);
    if (data) return data; // Retorna do cache se existir

    const response = await fetch('/api/user-account/card-info');
    if (!response.ok) throw new Error('Falha ao buscar os dados do card.');

    data = await response.json();
    cacheService.set(CACHE_KEY, data); // Salva no cache
    return data;
}

function renderProfileCard(data) {
    const cardContainer = document.querySelector('#profile-content .profile-card');
    if (!cardContainer) return;

    // Popula os dados básicos
    cardContainer.querySelector('.profile-picture').src = data.userProfile.avatarUrl || 'https://placehold.co/120x120/6a11cb/ffffff?text=User';
    cardContainer.querySelector('.profile-name').textContent = data.userProfile.name;
    cardContainer.querySelector('.profile-email').textContent = data.userProfile.email;

    // Limpa o status/botão anterior antes de adicionar o novo
    cardContainer.querySelector('.subscription-status-badge')?.remove();
    cardContainer.querySelector('.profile-cta-button')?.remove();

    // Lógica para exibir o status e o botão corretos
    if (data.isAdmin) {
        cardContainer.insertAdjacentHTML('beforeend', `
            <div class="subscription-status-badge status-admin"><i class="fas fa-crown"></i> Administrador</div>
            <a href="/Profile/Admin/Index" class="profile-cta-button">Painel Admin</a>
        `);
    } else if (data.subscription) {
        const statusClass = `status-${data.subscription.status?.toLowerCase() || 'none'}`;
        cardContainer.insertAdjacentHTML('beforeend', `
            <div class="subscription-status-badge ${statusClass}">Plano: ${data.subscription.planName} (${data.subscription.status})</div>
            <a href="/Pages/Courses/Index" class="profile-cta-button">✨ Acessar Cursos</a>
        `);
    } else {
        cardContainer.insertAdjacentHTML('beforeend', `
            <div class="subscription-status-badge status-none">Você não tem uma assinatura ativa.</div>
            <a href="/Pages/Payments/Subscription/Index" class="profile-cta-button">✨ Virar Premium</a>
        `);
    }
}

/**
 * Função principal exportada. Orquestra a busca e renderização do card.
 */
export async function initializeProfileCard() {
    try {
        const data = await fetchCardInfo();
        renderProfileCard(data);
    } catch (error) {
        console.error('Falha ao inicializar o card de perfil:', error);
        // UX: Mostra uma mensagem de erro no card se a API falhar
        const cardContainer = document.querySelector('#profile-content .profile-card');
        if (cardContainer) {
            cardContainer.innerHTML = '<p style="text-align:center; color: var(--danger-color);">Não foi possível carregar seus dados.</p>';
        }
    }
}