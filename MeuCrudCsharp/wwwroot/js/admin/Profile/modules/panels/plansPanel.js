// /js/admin/modules/panels/plansPanel.js

import * as api from '../api/adminAPI.js';
import {openModal, closeModal} from '../ui/modals.js';

// --- Seletores de DOM ---
const plansTableBody = document.getElementById('plans-table-body');
const createPlanForm = document.getElementById('create-plan-form');
const editModal = document.getElementById('edit-plan-modal');
const editForm = document.getElementById('edit-plan-form');
const editPlanId = document.getElementById('edit-plan-id');
const editPlanReason = document.getElementById('edit-plan-reason');
const fetchButtonsContainer = document.getElementById('fetch-buttons');


function renderPlansTable(plans) {
    plansTableBody.innerHTML = ''; // Limpa a tabela antes de popular

    if (!plans || plans.length === 0) {
        plansTableBody.innerHTML = '<tr><td colspan="5" class="text-center py-10 text-gray-500">Nenhum plano encontrado.</td></tr>';
        return;
    }

    plans.forEach(plan => {
        // Verificação segura para cada propriedade do plano
        const name = plan.Name ?? 'Nome Indisponível'; // Propriedades em C# são PascalCase

        // CORREÇÃO: Usamos a propriedade 'Type' que vem diretamente do back-end.
        // O DTO que criamos já faz a lógica de "Mensal", "Trimestral", "Anual", etc.
        const type = plan.Type ?? 'Tipo Indisponível';

        const price = plan.PriceDisplay ?? 'R$ 0,00';
        const status = plan.IsActive ? 'Active' : 'Inactive'; // O DTO tem IsActive (booleano)
        const publicId = plan.PublicId ?? 'ID_INDISPONIVEL';

        const row = document.createElement('tr');
        row.innerHTML = `
        <td>${name}</td>
        <td>${type}</td>
        <td>${price}</td>
        <td>
            <span class="status-badge status-${status.toLowerCase()}">${status}</span>
        </td>
        <td class="text-right">
            <button class="btn btn-secondary btn-sm btn-edit" data-public-id="${publicId}">Editar</button>
            <button class="btn btn-danger btn-sm btn-delete" data-public-id="${publicId}">Excluir</button>
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

        editPlanId.value = plan.publicId;
        editPlanReason.value = plan.name;
        document.getElementById('edit-plan-amount').value = plan.transactionAmount;
        const frequencySelect = document.getElementById('edit-plan-frequency-type');
        if (plan.frequency === 12 && plan.frequencyType === 'months') {
            frequencySelect.value = 'years';
        } else {
            frequencySelect.value = 'months';
        }
        
        openModal(editModal);

    } catch (error) {
        console.error("Falha ao buscar ou preencher detalhes do plano:", error);
    }
}

export async function loadPlans(source) {
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
        // 2. Busca os dados da fonte correta
        const plans = source === 'api' ? await api.getAdminPlans() : await api.getPublicPlans();

        // 3. Renderiza a tabela com os dados
        renderPlansTable(plans);
        initializePlansPanel();
    } catch (error) {
        // 4. Mostra uma mensagem de erro em caso de falha
        plansTableBody.innerHTML = `<tr><td colspan="5" class="text-center py-10 text-red-600 font-semibold">${error.message}</td></tr>`;
    } finally {
        // 5. Reabilita os botões em qualquer cenário (sucesso ou erro)
        buttons.forEach(button => button.disabled = false);
    }
}

function initializePlansPanel() {
    // Event delegation para a tabela de planos

    fetchButtonsContainer.addEventListener('click', (e) => {
        const target = e.target.closest('button');
        if (!target) return;

        if (target.id === 'fetch-api-btn') {
            loadPlans('api');
        } else if (target.id === 'fetch-db-btn') {
            loadPlans('db');
        }
    });

    plansTableBody.addEventListener('click', (e) => {
        const target = e.target;
        if (target.classList.contains('btn-edit')) {
            openEditPlanModal(target.dataset.publicId);
        }
        if (target.classList.contains('btn-delete')) {
            handlePlanDelete(target.dataset.publicId);
        }
    });

    // SEU MÉTODO addEventListener CORRIGIDO
    createPlanForm?.addEventListener('submit', async function (e) {
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
                frequency: frequency,                 // Usando a variável do input 'plan-interval'
                frequency_type: frequency_type,       // Usando a variável do select 'plan-frequency-type'
                transaction_amount: parseFloat(document.getElementById('plan-amount').value),
                currency_id: 'BRL' // É bom sempre enviar a moeda
            },
            description: document.getElementById('plan-description').value,
            back_url: "https://b1027b9a8e2b.ngrok-free.app/" // Lembre-se de ajustar a URL conforme necessário
        }

        try {
            // NOVO: Passa o token encontrado para a função da API
            const result = await api.createPlan(planData);

            await Swal.fire({
                title: 'Success!', text: `Plan created successfully! ID: ${result.id}`, icon: 'success'
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

    editForm?.addEventListener('submit', async (e) => {
        e.preventDefault();
        const planId = editPlanId.value;
        const submitButton = editForm.querySelector('button[type="submit"]');
        submitButton.disabled = true;
        submitButton.textContent = 'Saving...';

        const selectedFrequency = document.getElementById('edit-plan-frequency-type').value;

        const updatedData = {
            reason: document.getElementById('edit-plan-reason').value,
            transaction_amount: parseFloat(document.getElementById('edit-plan-amount').value),
            frequency: selectedFrequency === 'years' ? 12 : 1, 
            frequency_type: 'months' 
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

    document.getElementById('close-edit-plan-modal')?.addEventListener('click', () => closeModal(editModal));
}