import { fetchPlans } from './modules/api/plansAPI.js';
import { createPlanCardHTML } from './modules/ui/planCard.js';

// --- ESTADO DA PÁGINA ---
let currentPage = 1;
const PLANS_PER_PAGE = window.innerWidth < 768 ? 1 : 2; // Conforme sua solicitação: 2 planos por página.

// --- ELEMENTOS DA UI ---
const plansContainer = document.getElementById('plans-grid-container');
const loader = document.getElementById('plans-loader');
const paginationControls = document.getElementById('plans-pagination-controls');
const prevButton = document.getElementById('prev-page-btn');
const nextButton = document.getElementById('next-page-btn');
const currentPageSpan = document.getElementById('current-page');
const totalPagesSpan = document.getElementById('total-pages');

/**
 * Atualiza a UI dos controles de paginação (botões e texto).
 * @param {object} pagedResult - O objeto de paginação retornado pela API.
 */
function updatePaginationControls({ currentPage, totalPages, hasPreviousPage, hasNextPage }) {
    if (totalPages <= 1) {
        paginationControls.style.display = 'none'; // Esconde se só tem 1 página
        return;
    }

    paginationControls.style.display = 'flex'; // Mostra os controles
    currentPageSpan.textContent = currentPage;
    totalPagesSpan.textContent = totalPages;

    prevButton.disabled = !hasPreviousPage;
    nextButton.disabled = !hasNextPage;
}

/**
 * Busca os dados de uma página específica e renderiza na tela.
 * @param {number} page - O número da página a ser carregada.
 */
async function loadAndRenderPlans(page) {
    plansContainer.innerHTML = ''; // Limpa os cards antigos
    loader.style.display = 'block'; // Mostra o loader
    prevButton.disabled = true; // Desabilita botões durante o carregamento
    nextButton.disabled = true;

    try {
        // 1. Busca os dados paginados da API
        const pagedResult = await fetchPlans(page, PLANS_PER_PAGE);

        // 2. Renderiza os cards usando pagedResult.items, que é a lista da página atual
        if (pagedResult.items && pagedResult.items.length > 0) {
            plansContainer.innerHTML = pagedResult.items.map(createPlanCardHTML).join('');
        } else {
            plansContainer.innerHTML = '<p class="error-message">Nenhum plano disponível no momento.</p>';
        }

        // 3. Atualiza os controles de paginação com os metadados
        updatePaginationControls(pagedResult);

    } catch (error) {
        console.error(`Falha ao carregar os planos da página ${page}:`, error);
        plansContainer.innerHTML = '<p class="error-message">Não foi possível carregar os planos. Tente novamente mais tarde.</p>';
        paginationControls.style.display = 'none'; // Esconde em caso de erro
    } finally {
        loader.style.display = 'none'; // Esconde o loader
    }
}

/**
 * Orquestra a inicialização da página e dos eventos.
 */
function initializePlansPage() {
    // Adiciona os listeners aos botões de paginação
    prevButton.addEventListener('click', () => {
        if (currentPage > 1) {
            currentPage--;
            loadAndRenderPlans(currentPage);
        }
    });

    nextButton.addEventListener('click', () => {
        // A lógica de "tem próxima página" é controlada pelo estado 'disabled' do botão,
        // então não precisamos de uma verificação extra aqui.
        currentPage++;
        loadAndRenderPlans(currentPage);
    });

    // Carga inicial dos planos da primeira página
    loadAndRenderPlans(currentPage);
}

document.addEventListener('DOMContentLoaded', initializePlansPage);