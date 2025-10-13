// js/admin/modules/ui/pagination.js

/**
 * Estado para armazenar a página atual de cada painel.
 * Usamos um objeto para que cada painel (plans, courses, etc.)
 * possa ter sua própria contagem de página independentemente.
 */
const paginationState = {};

/**
 * Inicializa os controles de paginação para um painel específico.
 * Adiciona os event listeners aos botões "Anterior" e "Próximo".
 * Garante que os listeners sejam adicionados apenas uma vez.
 *
 * @param {string} prefix - O prefixo único para os IDs dos elementos de paginação (ex: 'plans', 'courses').
 * @param {function} onPageChange - A função a ser chamada quando o usuário clica para mudar de página. Ela receberá o novo número da página como argumento.
 */
export function initializePagination(prefix, onPageChange) {
    const paginationContainer = document.getElementById(`${prefix}-pagination`);
    if (!paginationContainer || paginationContainer.hasAttribute('data-pagination-initialized')) {
        return; // Sai se não encontrar o container ou se já foi inicializado
    }

    const prevButton = document.getElementById(`${prefix}-prev-page-btn`);
    const nextButton = document.getElementById(`${prefix}-next-page-btn`);

    // Inicializa o estado da página para este prefixo se ainda não existir
    if (!paginationState[prefix]) {
        paginationState[prefix] = 1; // Começa na página 1
    }

    prevButton.addEventListener('click', () => {
        if (paginationState[prefix] > 1) {
            paginationState[prefix]--;
            onPageChange(paginationState[prefix]);
        }
    });

    nextButton.addEventListener('click', () => {
        // A lógica para saber se existe uma próxima página será controlada pelo estado 'disabled' do botão
        paginationState[prefix]++;
        onPageChange(paginationState[prefix]);
    });

    // Adiciona a "flag" para garantir que esta inicialização não ocorra novamente
    paginationContainer.setAttribute('data-pagination-initialized', 'true');
    console.log(`Paginação para '${prefix}' foi inicializada.`);
}

/**
 * Atualiza a UI dos controles de paginação (números e estado dos botões).
 *
 * @param {string} prefix - O prefixo único para os IDs dos elementos (ex: 'plans', 'courses').
 * @param {object} data - Um objeto contendo as informações da paginação vindas da API.
 * @param {number} data.currentPage - O número da página atual.
 * @param {number} data.totalPages - O número total de páginas.
 * @param {boolean} data.hasNextPage - Se existe uma próxima página.
 * @param {boolean} data.hasPreviousPage - Se existe uma página anterior.
 */
export function updatePaginationState(prefix, { currentPage, totalPages, hasNextPage, hasPreviousPage }) {
    const currentPageSpan = document.getElementById(`${prefix}-current-page`);
    const totalPagesSpan = document.getElementById(`${prefix}-total-pages`);
    const prevButton = document.getElementById(`${prefix}-prev-page-btn`);
    const nextButton = document.getElementById(`${prefix}-next-page-btn`);
    const paginationContainer = document.getElementById(`${prefix}-pagination`);

    // Se não encontrar os elementos, não faz nada.
    if (!currentPageSpan || !totalPagesSpan || !prevButton || !nextButton || !paginationContainer) {
        return;
    }

    // Mostra ou esconde o container de paginação se houver mais de uma página
    if (totalPages > 1) {
        paginationContainer.style.display = 'flex';
    } else {
        paginationContainer.style.display = 'none';
    }

    // Atualiza os textos
    currentPageSpan.textContent = currentPage;
    totalPagesSpan.textContent = totalPages;

    // Atualiza o estado dos botões
    prevButton.disabled = !hasPreviousPage;
    nextButton.disabled = !hasNextPage;

    // Atualiza o estado global para garantir consistência
    paginationState[prefix] = currentPage;
}