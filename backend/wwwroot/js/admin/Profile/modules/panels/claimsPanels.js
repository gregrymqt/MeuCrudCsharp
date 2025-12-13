import * as api from '../api/adminAPI.js';
import { initializePagination, updatePaginationState } from '../ui/pagination.js'; // Assumindo que updatePaginationState recebe (prefix, data)
import { openModal, closeModal } from '../ui/modals.js';
// Objeto para manter o estado atual dos filtros e da página
const claimsState = {
    searchTerm: '',
    statusFilter: '',
    currentPage: 1,
};

/**
 * Renderiza a tabela de reclamações usando o template do HTML.
 * @param {Array} claims - A lista de reclamações a ser exibida.
 */
function renderClaimsTable(claims) {
    const tableBody = document.getElementById('claims-table-body');
    const template = document.getElementById('claim-row-template');

    if (!tableBody || !template) {
        console.error('Elementos da tabela de reclamações (tbody ou template) não encontrados.');
        return;
    }

    tableBody.innerHTML = ''; // Limpa a tabela antes de renderizar

    if (!claims || claims.length === 0) {
        tableBody.innerHTML = `<tr><td colspan="6" class="text-center">Nenhuma reclamação encontrada.</td></tr>`;
        return;
    }

    claims.forEach(claim => {
        const clone = template.content.cloneNode(true);
        const cells = clone.querySelectorAll('td');

        const formattedDate = new Date(claim.dateCreated).toLocaleDateString('pt-BR', {
            day: '2-digit',
            month: '2-digit',
            year: 'numeric',
        });

        // Mapeamento de status para classes de badge do Bootstrap
        const statusBadges = {
            'Nova': 'bg-primary',
            'Em Análise': 'bg-warning text-dark',
            'Resolvida': 'bg-success',
        };

        cells[0].textContent = claim.orderId;
        cells[1].textContent = claim.customerName;
        cells[2].textContent = claim.reason;
        cells[3].textContent = formattedDate;

        const statusBadge = cells[4].querySelector('.badge');
        statusBadge.textContent = claim.status;
        statusBadge.className = `badge ${statusBadges[claim.status] || 'bg-secondary'}`;

        // Ações (exemplo, links precisam ser preenchidos)
        clone.querySelector('.view-on-mp').href = `https://www.mercadopago.com.br/gz/claims/${claim.orderId}`;

        const changeStatusBtn = clone.querySelector('.btn-change-status');
        changeStatusBtn.dataset.claimId = claim.id; // Usando o ID interno do banco
        changeStatusBtn.dataset.currentStatus = claim.status;

        tableBody.appendChild(clone);
    });
}

/**
 * Carrega os dados das reclamações da API com base no estado atual e atualiza a UI.
 * @param {number} page - O número da página a ser carregada.
 */
async function loadClaims(page = 1) {
    claimsState.currentPage = page;
    const spinner = document.getElementById('claims-loading-spinner');
    const tableContainer = document.getElementById('claims-table-container');

    spinner.style.display = 'block';
    tableContainer.style.display = 'none';

    try {
        const response = await api.getClaims(claimsState.searchTerm, claimsState.statusFilter, claimsState.currentPage);
        renderClaimsTable(response.claims);
        // Corrigido: Passando o objeto de resposta inteiro para a função de paginação
        updatePaginationState('claims', response);
    } catch (error) {
        console.error('Falha ao carregar as reclamações:', error);
        const tableBody = document.getElementById('claims-table-body');
        if (tableBody) {
            tableBody.innerHTML = `<tr><td colspan="6" class="text-center text-danger">Erro ao carregar dados.</td></tr>`;
        }
    } finally {
        spinner.style.display = 'none';
        tableContainer.style.display = 'block';
    }
}

/**
 * Abre o modal para alterar o status de uma claim, preenchendo os campos.
 * @param {string} claimId - O ID da claim a ser alterada.
 * @param {string} currentStatus - O status atual da claim.
 */
function openChangeStatusModal(claimId, currentStatus) {
    const statusModal = document.getElementById('changeClaimStatusModal');
    if (!statusModal) return;

    document.getElementById('modal-claim-id').value = claimId;
    const statusSelect = document.getElementById('modal-claim-status');
    statusSelect.value = currentStatus;

    openModal(statusModal);
}

/**
 * Cria uma versão "debounced" de uma função, que atrasa sua execução
 * até que um determinado tempo tenha passado sem que ela tenha sido chamada.
 * @param {Function} func A função a ser "debounced".
 * @param {number} delay O tempo de espera em milissegundos.
 * @returns {Function} A nova função "debounced".
 */
function debounce(func, delay) {
    let timeoutId;

    return function(...args) {
        // Cancela o timer anterior toda vez que a função é chamada
        clearTimeout(timeoutId);

        // Configura um novo timer
        timeoutId = setTimeout(() => {
            // Executa a função original com os argumentos corretos
            func.apply(this, args);
        }, delay);
    };
}

/**
 * Inicializa o painel de reclamações, configurando os event listeners para os filtros.
 */

export function initializeClaimsPanel() {
    const searchInput = document.getElementById('claims-search-term');
    const statusFilter = document.getElementById('claims-status-filter');
    const statusModal = document.getElementById('changeClaimStatusModal');
    const changeStatusForm = document.getElementById('change-status-form');
    const tableBody = document.getElementById('claims-table-body');

    // 1. Criamos a função que faz a busca
    const handleSearch = (event) => {
        claimsState.searchTerm = event.target.value;
        loadClaims(1); // Volta para a primeira página ao filtrar
    };

    // 2. Criamos uma versão "debounced" da nossa função de busca
    //    com um delay de 500ms (meio segundo).
    const debouncedSearch = debounce(handleSearch, 500);

    // 3. Adicionamos o listener para 'keyup', mas agora ele chama a versão debounced!
    searchInput.addEventListener('keyup', debouncedSearch);


    // O filtro de status não precisa de debounce, pois 'change' só dispara uma vez.
    statusFilter.addEventListener('change', (e) => {
        claimsState.statusFilter = e.target.value;
        loadClaims(1); // Volta para a primeira página ao filtrar
    });

    // Listener para abrir o modal de status
    tableBody.addEventListener('click', (e) => {
        const statusBtn = e.target.closest('.btn-change-status');
        if (statusBtn) {
            const { claimId, currentStatus } = statusBtn.dataset;
            openChangeStatusModal(claimId, currentStatus);
        }
    });

    // Listener para o formulário de mudança de status
    changeStatusForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const submitButton = e.target.querySelector('button[type="submit"]');
        const claimId = document.getElementById('modal-claim-id').value;
        const newStatus = document.getElementById('modal-claim-status').value;

        submitButton.disabled = true;
        submitButton.textContent = 'Salvando...';

        try {
            await api.updateClaimStatus(claimId, newStatus);
            closeModal(statusModal);
            await loadClaims(claimsState.currentPage); // Recarrega a página atual
            // Você pode adicionar um toast de sucesso aqui se quiser
        } catch (error) {
            console.error('Erro ao atualizar status da claim:', error);
            alert('Falha ao atualizar o status. Tente novamente.');
        } finally {
            submitButton.disabled = false;
            submitButton.textContent = 'Salvar Alterações';
        }
    });

    // Listeners para fechar o modal
    document.getElementById('close-status-modal-btn').addEventListener('click', () => {
        closeModal(statusModal);
    });
    document.getElementById('cancel-status-modal-btn').addEventListener('click', () => {
        closeModal(statusModal);
    });

    // O resto continua igual
    initializePagination('claims', loadClaims);
    loadClaims(1);
}
