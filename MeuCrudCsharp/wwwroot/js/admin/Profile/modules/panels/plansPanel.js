// /js/admin/modules/panels/plansPanel.js

import * as api from '../api/adminAPI.js';
import {openModal, closeModal} from '../ui/modals.js';
import {initializePagination, updatePaginationState} from '../ui/pagination.js';


// --- Seletores de DOM ---
const plansTableBody = document.getElementById('plans-table-body');
const createPlanForm = document.getElementById('create-plan-form');
const editModal = document.getElementById('edit-plan-modal');
const editForm = document.getElementById('edit-plan-form');
const editPlanId = document.getElementById('edit-plan-id');
const editPlanReason = document.getElementById('edit-plan-reason');
const fetchButtonsContainer = document.getElementById('fetch-buttons');
const editPlanAmount = document.getElementById('edit-plan-amount');
const editPlanFrequencyType = document.getElementById('edit-plan-frequency');
const PLANS_PER_PAGE = 10;
let currentSource = 'db';


// ✨ FUNÇÃO CORRIGIDA ✨
function renderPlansTable(plans) {
    const plansTableBody = document.getElementById('plans-table-body');
    plansTableBody.innerHTML = '';

    if (!plans || plans.length === 0) {
        plansTableBody.innerHTML = '<tr><td colspan="5" class="text-center py-10 text-gray-500">Nenhum plano encontrado.</td></tr>';
        return;
    }

    plans.forEach(plan => {
        const name = plan.name ?? 'Nome Indisponível';
        const type = plan.slug ?? 'Tipo Indisponível';
        const price = plan.priceDisplay ?? 'R$ 0,00';
        const status = plan.isActive ? 'Active' : 'Inactive';
        const publicId = plan.publicId ?? 'ID_INDISPONIVEL';

        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${name}</td>
            <td>${type}</td>
            <td>${price}</td>
            <td>
                <span class="status-badge status-${status.toLowerCase()}">${status}</span>
            </td>
            <td class="text-right">
                <div class="dropdown">
                    <button class="btn btn-light btn-sm rounded-circle" type="button" data-bs-toggle="dropdown" aria-expanded="false">
                        &#8942;
                    </button>
                    <ul class="dropdown-menu dropdown-menu-end">
                        <li><a class="dropdown-item btn-edit" href="#" data-public-id="${publicId}">Editar</a></li>
                        <li><a class="dropdown-item btn-delete text-danger" href="#" data-public-id="${publicId}">Excluir</a></li>
                    </ul>
                </div>
            </td>
        `;
        plansTableBody.appendChild(row);
    });
}

async function handlePlanDelete(planId) {
    const result = await Swal.fire({
        title: 'Are you sure?',
        text: "This action cannot be undone.",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Yes, delete it!',
        cancelButtonText: 'Cancel'
    });
    if (result.isConfirmed) {
        try {
            await api.deletePlan(planId);
            api.invalidateCache('allPlans');
            await loadPlans();
            Swal.fire('Excluído!', 'O plano foi excluído.', 'success');
        } catch (error) {
            Swal.fire('Erro!', error.message, 'error');
        }
    }
}

async function openEditPlanModal(planId) {
    try {
        const plan = await api.getPlanById(planId);

        // Preenche os campos usando os dados brutos do DTO
        editPlanId.value = plan.publicId;
        editPlanReason.value = plan.name;
        editPlanAmount.value = plan.transactionAmount;
        editPlanFrequencyType.value = plan.frequency;

        openModal(editModal);

    } catch (error) {
        console.error("Falha ao buscar ou preencher detalhes do plano:", error);
    }
}

async function loadPlans() {
    await fetchAndDisplayPlans(1);
}

async function fetchAndDisplayPlans(source, page = 1) {
    // 1. Mostra o estado de carregamento
    plansTableBody.innerHTML = `
        <tr>
            <td colspan="5" class="text-center py-10">
                <div class="flex justify-center items-center flex-col">
                    <div class="loading-spinner mb-2"></div>
                    <span class="text-gray-600">Carregando planos...</span>
                </div>
            </td>
        </tr>`;

    // Desabilita os botões para prevenir múltiplos cliques
    const buttons = fetchButtonsContainer.querySelectorAll('button');
    buttons.forEach(button => button.disabled = true);

    try {
        // 1. A chamada de API agora passa a página e o tamanho
        const result = source === 'api'
            ? await api.getAdminPlans(page, PLANS_PER_PAGE)
            : await api.getPublicPlans(page, PLANS_PER_PAGE);

        // 2. Renderiza a tabela usando apenas a lista de 'items' da resposta
        renderPlansTable(result.items);

        // 3. Atualiza os controles de paginação com os metadados da resposta
        updatePaginationState('plans', result);

    } catch (error) {
        plansTableBody.innerHTML = `<tr><td colspan="5">${error.message}</td></tr>`;
        // Esconde a paginação em caso de erro
        document.getElementById('plans-pagination').style.display = 'none';
    } finally {
        buttons.forEach(button => button.disabled = false);
    }
}

export async function initializePlansPanel() {

    if (fetchButtonsContainer && !fetchButtonsContainer.hasAttribute('data-click-handler-attached')) {
        fetchButtonsContainer.addEventListener('click', (e) => {
            const button = e.target.closest('button');
            if (!button) return;

            currentSource = button.id === 'fetch-api-btn' ? 'api' : 'db';
            fetchAndDisplayPlans(currentSource, 1); // Sempre volta para a página 1 ao trocar a fonte
        });
        fetchButtonsContainer.setAttribute('data-click-handler-attached', 'true');
    }

    initializePagination('plans', (newPage) => {
        fetchAndDisplayPlans(currentSource, newPage);
    });

    // Carga inicial dos dados
    fetchAndDisplayPlans(currentSource, 1);

    if (plansTableBody && !plansTableBody.hasAttribute('data-click-listener-attached')) {

        plansTableBody.addEventListener('click', (e) => {
            const target = e.target.closest('a');

            if (!target) return;

            if (target.classList.contains('btn-edit')) {
                openEditPlanModal(target.dataset.publicId);
            }

            if (target.classList.contains('btn-delete')) {
                handlePlanDelete(target.dataset.publicId);
            }
        });

        // ADICIONA A FLAG PARA GARANTIR QUE O CÓDIGO ACIMA SÓ RODE UMA VEZ
        plansTableBody.setAttribute('data-click-listener-attached', 'true');
        console.log('Listener de delegação para a tabela de planos foi anexado.');
    }

    // SEU MÉTODO addEventListener CORRIGIDO
    if (createPlanForm && !createPlanForm.hasAttribute('data-listener-attached')) {

        // Adiciona o listener
        createPlanForm.addEventListener('submit', async function (e) {
            e.preventDefault();
            const saveButton = createPlanForm.querySelector('button[type="submit"]');
            saveButton.disabled = true;
            saveButton.textContent = 'Creating...';

            // Lê os valores dos novos campos do formulário
            const frequency = parseInt(document.getElementById('plan-interval').value, 10);
            const frequency_type = document.getElementById('plan-frequency-type').value;

            const planData = {
                reason: document.getElementById('plan-reason').value,
                auto_recurring: {
                    frequency: frequency,
                    frequency_type: frequency_type,
                    transaction_amount: parseFloat(document.getElementById('plan-amount').value),
                    currency_id: 'BRL'
                },
                description: document.getElementById('plan-description').value
            }

            try {
                const result = await api.createPlan(planData);
                await Swal.fire({
                    title: 'Success!', text: `Plan created successfully! ID: ${result.publicId}`, icon: 'success'
                });
                createPlanForm.reset();
                await loadPlans();
            } catch (error) {
                Swal.fire({
                    title: 'Error!', text: error.message, icon: 'error'
                });
            } finally {
                saveButton.disabled = false;
                saveButton.textContent = 'Create Plan';
            }
        });

        // MARCA O ELEMENTO PARA INDICAR QUE O LISTENER FOI ANEXADO
        createPlanForm.setAttribute('data-listener-attached', 'true');
        console.log('Listener do formulário de criação de plano foi anexado.');
    }

    if (editForm && !editForm.hasAttribute('data-listener-attached')) {

        editForm.addEventListener('submit', async (e) => {
            e.preventDefault();

            const planId = document.getElementById('edit-plan-id').value;
            const submitButton = editForm.querySelector('button[type="submit"]');
            submitButton.disabled = true;
            submitButton.textContent = 'Saving...';

            const updatedData = {
                reason: document.getElementById('edit-plan-reason').value,
                auto_recurring: {
                    transaction_amount: parseFloat(document.getElementById('edit-plan-amount').value),
                    frequency: parseInt(document.getElementById('edit-plan-frequency').value, 10),
                    frequency_type: 'months',
                    currency_id: 'BRL'
                }
            };

            try {
                const response = await api.updatePlan(planId, updatedData);

                Swal.fire({
                    title: 'Success!',
                    text: `Plan updated successfully! Name: ${response.Name}`,
                    icon: 'success',
                    timer: 2000,
                    showConfirmButton: false
                });

                closeModal();
                await loadPlans();

            } catch (error) {
                console.error('Error updating plan:', error);
                Swal.fire({
                    title: 'Error!', text: error.message, icon: 'error'
                });
            } finally {
                submitButton.disabled = false;
                submitButton.textContent = 'Save Changes';
            }
        });

        editForm.setAttribute('data-listener-attached', 'true');
        console.log('Listener do formulário de EDIÇÃO de plano foi anexado.');
    }

    document.getElementById('close-edit-plan-modal')?.addEventListener('click', () => closeModal(editModal));
}