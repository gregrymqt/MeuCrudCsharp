document.addEventListener('DOMContentLoaded', function () {
    const form = document.getElementById('create-plan-form');
    const saveButton = document.getElementById('save-plan-button');
    const formStatus = document.getElementById('form-status');
    const plansTableBody = document.getElementById('plans-table-body');

    // --- Lógica para Criar um Novo Plano ---
    form.addEventListener('submit', async function (event) {
        event.preventDefault();
        saveButton.disabled = true;
        saveButton.textContent = 'Criando...';
        formStatus.innerHTML = '';

        // Coleta os dados do formulário
        const planData = {
            reason: document.getElementById('plan-reason').value,
            auto_recurring: {
                frequency: parseInt(document.getElementById('plan-frequency').value),
                frequency_type: document.getElementById('plan-frequency-type').value,
                transaction_amount: parseFloat(document.getElementById('plan-amount').value),
                currency_id: document.getElementById('plan-currency').value
            },
            back_url: document.getElementById('plan-back-url').value
        };

        try {
            // O backend precisa ter um endpoint para receber esta chamada
            const response = await fetch('/api/admin/plans', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(planData)
            });

            const result = await response.json();

            if (!response.ok) {
                throw new Error(result.message || 'Ocorreu um erro ao criar o plano.');
            }

            formStatus.innerHTML = `<p style="color: var(--success-color);"><strong>Plano criado com sucesso!</strong> ID: ${result.id}</p>`;
            form.reset();
            loadPlans(); // Recarrega a lista de planos

        } catch (error) {
            console.error('Erro ao criar plano:', error);
            formStatus.innerHTML = `<p style="color: var(--danger-color);"><strong>Erro:</strong> ${error.message}</p>`;
        } finally {
            saveButton.disabled = false;
            saveButton.textContent = 'Criar Plano';
        }
    });

    // --- Lógica para Listar Planos Existentes ---
    async function loadPlans() {
        try {
            // O backend precisa ter um endpoint para listar os planos
            const response = await fetch('/api/admin/plans');
            if (!response.ok) throw new Error('Falha ao buscar planos.');

            const plans = await response.json();
            plansTableBody.innerHTML = '';

            if (plans.length === 0) {
                plansTableBody.innerHTML = '<tr><td colspan="3" style="text-align: center;">Nenhum plano criado ainda.</td></tr>';
                return;
            }

            plans.forEach(plan => {
                const row = `
                        <tr>
                            <td>${plan.id}</td>
                            <td>${plan.reason}</td>
                            <td>${plan.auto_recurring.transaction_amount.toFixed(2)} ${plan.auto_recurring.currency_id}</td>
                        </tr>
                    `;
                plansTableBody.innerHTML += row;
            });

        } catch (error) {
            console.error('Erro ao carregar planos:', error);
            plansTableBody.innerHTML = `<tr><td colspan="3" style="text-align: center; color: var(--danger-color);">${error.message}</td></tr>`;
        }
    }

    // Carrega os planos ao iniciar a página
    loadPlans();
});