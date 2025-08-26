// /js/admin/modules/panels/plansPanel.js

import * as api from '../api/adminAPI.js';
import { openModal, closeModal } from '../ui/modals.js';

// --- Seletores de DOM ---
const plansTableBody = document.getElementById('plans-table-body');
const createPlanForm = document.getElementById('create-plan-form');
const createPlanStatus = document.getElementById('create-plan-status');
const editModal = document.getElementById('edit-plan-modal');
const editForm = document.getElementById('edit-plan-form');
const editPlanId = document.getElementById('edit-plan-id');
const editPlanReason = document.getElementById('edit-plan-reason');

function renderPlansTable(plans) {
    plansTableBody.innerHTML = '';
    if (!plans || plans.length === 0) {
        plansTableBody.innerHTML = '<tr><td colspan="5" class="text-center">Nenhum plano encontrado.</td></tr>';
        return;
    }
    plans.forEach(plan => {
        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${plan.reason || 'N/A'}</td>
            <td>${plan.autoRecurring?.frequencyType === 'years' ? 'Anual' : 'Mensal'}</td>
            <td>R$${plan.autoRecurring?.transactionAmount.toFixed(2)}</td>
            <td><span class="status-badge status-${plan.status?.toLowerCase()}">${plan.status || 'N/A'}</span></td>
            <td class="actions">
                <button class="btn btn-secondary btn-sm btn-edit" data-plan-id="${plan.id}">Editar</button>
                <button class="btn btn-danger btn-sm btn-delete" data-plan-id="${plan.id}">Excluir</button>
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
            await api.deletePlan(planId, token);
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
        editPlanId.value = plan.id;
        document.getElementById('edit-plan-id').value = plan.id;
        document.getElementById('edit-plan-reason').value = plan.reason;
        document.getElementById('edit-plan-amount').value = plan.autoRecurring.transactionAmount;
        document.getElementById('edit-plan-frequency-type').value = plan.autoRecurring.frequencyType;
        document.getElementById('edit-plan-back-url').value = plan.back_url || '';
        openModal(editModal);
    } catch (error) {
        console.error("Falha ao buscar detalhes do plano:", error);
    }
}

export async function loadPlans() {
    try {
        plansTableBody.innerHTML = `<tr><td colspan="5" class="text-center">Carregando...</td></tr>`;
        const plans = await api.getPlans();
        renderPlansTable(plans);
    } catch (error) {
        plansTableBody.innerHTML = `<tr><td colspan="5" class="text-center text-danger">${error.message}</td></tr>`;
    }
}

export function initializePlansPanel() {
    // Event delegation para a tabela de planos
    plansTableBody.addEventListener('click', (e) => {
        const target = e.target;
        if (target.classList.contains('btn-edit')) {
            openEditPlanModal(target.dataset.planId);
        }
        if (target.classList.contains('btn-delete')) {
            handlePlanDelete(target.dataset.planId);
        }
    });

    // SEU MÉTODO addEventListener CORRIGIDO
createPlanForm?.addEventListener('submit', async function (e) {
    e.preventDefault();
    const saveButton = createPlanForm.querySelector('button[type="submit"]');
    saveButton.disabled = true;
    saveButton.textContent = 'Creating...';
    const planData = {
        reason: document.getElementById('plan-reason').value,
        autoRecurring: {
            frequency: 1,
            frequencyType: document.getElementById('plan-type').value,
            transactionAmount: parseFloat(document.getElementById('plan-amount').value),
        },
        backUrl: "https://b1027b9a8e2b.ngrok-free.app/"
    };

    try {
        // NOVO: Passa o token encontrado para a função da API
        const result = await api.createPlan(planData);

        await Swal.fire({
            title: 'Success!',
            text: `Plan created successfully! ID: ${result.id}`,
            icon: 'success'
        });

        createPlanForm.reset();
        await loadPlans();

    } catch (error) {
        Swal.fire({
            title: 'Error!',
            text: error.message,
            icon: 'error'
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
        
        const updatedData = {
            reason: editPlanReason.value,
            back_url: editPlanBackUrl.value,
            auto_recurring: {
                transaction_amount: parseFloat(editPlanAmount.value)
            }
        };

        try {
            const response = await api.updatePlan(planId, updatedData, token);

            Swal.fire({
                title: 'Success!',
                text: `Plan updated successfully! Name: ${response.Name}`,
                icon: 'success',
                timer: 2000,
                showConfirmButton: false
            });

            closeEditModal();
            await loadPlans();

        } catch (error) {
            console.error('Error updating plan:', error);
            Swal.fire({
                title: 'Error!',
                text: error.message,
                icon: 'error'
            });
        } finally {
            submitButton.disabled = false;
            submitButton.textContent = 'Save Changes';
        }
    });

    document.getElementById('close-edit-plan-modal')?.addEventListener('click', () => closeModal(editModal));
}