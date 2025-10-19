import * as api from '../api/adminAPI.js';
import { initializePagination, updatePaginationState } from '../ui/pagination.js';

// Objeto para manter o estado atual dos filtros e da página
const chargebacksState = {
    searchTerm: '',
    statusFilter: '',
    currentPage: 1,
};

/**
 * Renderiza a tabela de chargebacks com os dados fornecidos.
 * @param {Array} chargebacks - A lista de chargebacks a ser exibida.
 */
function renderChargebacksTable(chargebacks) {
    const tableBody = document.getElementById('chargebacks-table-body');
    if (!tableBody) {
        console.error('Elemento com ID "chargebacks-table-body" não encontrado.');
        return;
    }

    tableBody.innerHTML = ''; // Limpa a tabela antes de renderizar

    if (!chargebacks || chargebacks.length === 0) {
        tableBody.innerHTML = `<tr><td colspan="6" class="text-center">Nenhum chargeback encontrado.</td></tr>`;
        return;
    }

    chargebacks.forEach(cb => {
        const formattedDate = new Date(cb.date).toLocaleDateString('pt-BR', {
            day: '2-digit', month: '2-digit', year: 'numeric'
        });
        const formattedAmount = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(cb.amount);

        // Mapeamento de status para classes de badge
        const statusBadges = {
            'won': 'bg-success',
            'lost': 'bg-danger',
            'under_review': 'bg-warning text-dark',
            'default': 'bg-secondary'
        };
        const statusClass = statusBadges[cb.status] || statusBadges['default'];

        const row = `
            <tr>
                <td>${cb.id}</td>
                <td>${cb.customer}</td>
                <td class="text-danger fw-bold">${formattedAmount}</td>
                <td>${formattedDate}</td>
                <td><span class="badge ${statusClass}">${cb.status.replace('_', ' ')}</span></td>
                <td>
                    <a href="${cb.mercadoPagoUrl}" class="btn btn-sm btn-secondary" target="_blank" title="Ver no Mercado Pago">
                        <i class="bi bi-box-arrow-up-right"></i> Ver no MP
                    </a>
                </td>
            </tr>
        `;
        tableBody.insertAdjacentHTML('beforeend', row);
    });
}

/**
 * Carrega os dados de chargebacks da API com base no estado atual e atualiza a UI.
 * @param {number} page - O número da página a ser carregada.
 */
async function loadChargebacks(page = 1) {
    chargebacksState.currentPage = page;
    const spinner = document.getElementById('chargebacks-loading-spinner');
    const tableContainer = document.getElementById('chargebacks-table-container');

    if(spinner) spinner.style.display = 'block';
    if(tableContainer) tableContainer.style.display = 'none';

    try {
        const response = await api.getChargebacks(chargebacksState.searchTerm, chargebacksState.statusFilter, chargebacksState.currentPage);
        renderChargebacksTable(response.chargebacks);
        updatePaginationState('chargebacks', response);
    } catch (error) {
        console.error('Falha ao carregar os chargebacks:', error);
        const tableBody = document.getElementById('chargebacks-table-body');
        if (tableBody) {
            tableBody.innerHTML = `<tr><td colspan="6" class="text-center text-danger">Erro ao carregar dados.</td></tr>`;
        }
    } finally {
        if(spinner) spinner.style.display = 'none';
        if(tableContainer) tableContainer.style.display = 'block';
    }
}

/**
 * Inicializa o painel de chargebacks, configurando os event listeners para os filtros.
 */
export async function initializeChargebacksPanel() {
    const searchInput = document.getElementById('chargebacks-search-term');
    const statusFilter = document.getElementById('chargebacks-status-filter');

    if (!searchInput || !statusFilter) {
        console.warn('Elementos de filtro para chargebacks não encontrados. O painel não será totalmente inicializado.');
        return;
    }

    let debounceTimer;
    searchInput.addEventListener('keyup', (e) => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
            chargebacksState.searchTerm = e.target.value;
            loadChargebacks(1); // Volta para a primeira página ao filtrar
        }, 500);
    });

    statusFilter.addEventListener('change', (e) => {
        chargebacksState.statusFilter = e.target.value;
        loadChargebacks(1); // Volta para a primeira página ao filtrar
    });

    // Inicializa a paginação e carrega os dados iniciais
    initializePagination('chargebacks', loadChargebacks);
    loadChargebacks(1);
}