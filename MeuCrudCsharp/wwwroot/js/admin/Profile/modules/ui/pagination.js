/**
 * O estado atual da paginação para diferentes seções (ex: 'students', 'plans', 'courses').
 * Guarda a página atual e o total de páginas para cada um.
 */
const paginationState = {};

/**
 * Atualiza a UI da paginação (botões e texto) com base nos dados da API.
 * Também controla a visibilidade do container de paginação.
 * @param {string} prefix - O prefixo para os IDs dos elementos (ex: 'students', 'plans').
 * @param {object} data - O objeto de resposta da API, contendo currentPage, totalPages, etc.
 */
export function updatePaginationState(prefix, data) {
    const { currentPage, totalPages } = data;

    // Guarda o estado atual
    paginationState[prefix] = { currentPage, totalPages };

    // Seleciona os elementos da UI usando o prefixo
    const prevButton = document.getElementById(`${prefix}-prev-page-btn`);
    const nextButton = document.getElementById(`${prefix}-next-page-btn`);
    const currentPageSpan = document.getElementById(`${prefix}-current-page`);
    const totalPagesSpan = document.getElementById(`${prefix}-total-pages`);
    const paginationContainer = document.getElementById(`${prefix}-pagination`); // Pega o container principal

    // Validação para garantir que os elementos existem na página antes de manipulá-los
    if (!paginationContainer || !prevButton || !nextButton || !currentPageSpan || !totalPagesSpan) {
        console.warn(`Elementos de paginação com prefixo "${prefix}" não foram encontrados.`);
        return;
    }

    // --- NOVA LÓGICA ---
    // Verifica se há mais de uma página. Se não houver, oculta todo o controle de paginação.
    if (totalPages > 1) {
        // Usamos 'flex' porque é comum para esse tipo de container, mas 'block' também funcionaria.
        paginationContainer.style.display = 'flex';
    } else {
        paginationContainer.style.display = 'none';
    }
    // --- FIM DA NOVA LÓGICA ---

    // Atualiza os números da página
    currentPageSpan.textContent = currentPage;
    totalPagesSpan.textContent = totalPages;

    // Habilita/desabilita o botão "Anterior"
    prevButton.disabled = currentPage <= 1;

    // Habilita/desabilita o botão "Próximo"
    nextButton.disabled = currentPage >= totalPages;
}


/**
 * Inicializa os event listeners para os botões de paginação.
 * @param {string} prefix - O prefixo para os IDs dos elementos (ex: 'students').
 * @param {function} onPageChange - A função a ser chamada quando uma nova página for solicitada.
 */
export function initializePagination(prefix, onPageChange) {
    const prevButton = document.getElementById(`${prefix}-prev-page-btn`);
    const nextButton = document.getElementById(`${prefix}-next-page-btn`);

    if (!prevButton || !nextButton) {
        return;
    }

    prevButton.addEventListener('click', () => {
        const state = paginationState[prefix];
        if (state && state.currentPage > 1) {
            onPageChange(state.currentPage - 1);
        }
    });

    nextButton.addEventListener('click', () => {
        const state = paginationState[prefix];
        if (state && state.currentPage < state.totalPages) {
            onPageChange(state.currentPage + 1);
        }
    });
}