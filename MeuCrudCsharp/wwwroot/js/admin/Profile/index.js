document.addEventListener('DOMContentLoaded', function() {
    // =====================================================================
    // Lógica de Navegação da Sidebar
    // =====================================================================
    const sidebarLinks = document.querySelectorAll('.sidebar-link');
    const contentPanels = document.querySelectorAll('.content-panel');

    sidebarLinks.forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault(); // Impede a navegação padrão do link

            // Remove a classe 'active' de todos os links e painéis
            sidebarLinks.forEach(l => l.classList.remove('active'));
            contentPanels.forEach(c => c.classList.remove('active'));

            // Adiciona a classe 'active' ao link clicado e ao painel correspondente
            link.classList.add('active');
            const contentId = link.id.replace('nav-', 'content-');
            const activePanel = document.getElementById(contentId);
            if (activePanel) {
                activePanel.classList.add('active');
            }

            // Carrega os dados da aba clicada, se necessário
            if (link.id === 'nav-students' && !activePanel.dataset.loaded) {
                loadStudents();
                activePanel.dataset.loaded = 'true'; // Marca como carregado
            }
        });
    });

    // =====================================================================
    // Lógica do Painel "Criar Plano"
    // =====================================================================
    const createPlanForm = document.getElementById('create-plan-form');
    const createPlanStatus = document.getElementById('create-plan-status');

    if (createPlanForm) {
        createPlanForm.addEventListener('submit', async function (e) {
            e.preventDefault();
            const saveButton = createPlanForm.querySelector('button[type="submit"]');
            saveButton.disabled = true;
            saveButton.textContent = 'Criando...';
            createPlanStatus.innerHTML = '<p>Enviando dados para o Mercado Pago...</p>';

            // Coleta os dados do formulário
            const planData = {
                reason: document.getElementById('plan-reason').value,
                autoRecurring: {
                    frequency: 1, // Fixo em 1 para simplicidade
                    frequencyType: document.getElementById('plan-type').value,
                    transactionAmount: parseFloat(document.getElementById('plan-amount').value),
                },
                backUrl: "https://www.seusite.com/confirmacao" // URL de retorno
            };

            try {
                // Faz a chamada real para a sua API de backend
                const response = await fetch('/api/admin/plans', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(planData)
                });

                const result = await response.json();

                if (!response.ok) {
                    // Se a API retornar um erro, exibe a mensagem
                    throw new Error(result.message || 'Ocorreu um erro ao criar o plano.');
                }

                // Exibe a mensagem de sucesso com o ID retornado pela API
                createPlanStatus.innerHTML = `<p style="color: var(--success-color);"><strong>Plano criado com sucesso!</strong> ID: ${result.id}</p>`;
                createPlanForm.reset();

            } catch (error) {
                createPlanStatus.innerHTML = `<p style="color: var(--danger-color);">Erro: ${error.message}</p>`;
            } finally {
                saveButton.disabled = false;
                saveButton.textContent = 'Criar Plano';
            }
        });
    }

    // Lógica do Painel "Alunos"
    async function loadStudents() {
        const studentsTableBody = document.getElementById('students-table-body');
        studentsTableBody.innerHTML = '<tr><td colspan="4" style="text-align: center;">Carregando alunos...</td></tr>';

        try {
            // --- CHAMADA REAL À API ---
            const response = await fetch('/api/admin/students');
            if (!response.ok) {
                throw new Error('Falha ao carregar os dados dos alunos do servidor.');
            }
            const students = await response.json();
            // -------------------------

            studentsTableBody.innerHTML = ''; // Limpa a tabela

            if (students.length === 0) {
                studentsTableBody.innerHTML = '<tr><td colspan="4" style="text-align: center;">Nenhum aluno encontrado.</td></tr>';
                return;
            }

            students.forEach(student => {
                // Lógica para definir a cor do status
                let statusColor = 'grey'; // Padrão
                if (student.subscriptionStatus.toLowerCase() === 'approved' || student.subscriptionStatus.toLowerCase() === 'ativa') {
                    statusColor = 'green';
                } else if (student.subscriptionStatus.toLowerCase() === 'cancelled' || student.subscriptionStatus.toLowerCase() === 'cancelada') {
                    statusColor = 'red';
                } else if (student.subscriptionStatus.toLowerCase() === 'paused' || student.subscriptionStatus.toLowerCase() === 'pausada') {
                    statusColor = 'orange';
                }

                const row = `
                <tr>
                    <td>${student.name}</td>
                    <td>${student.email}</td>
                    <td><span style="color: ${statusColor}; font-weight: 600;">${student.subscriptionStatus}</span></td>
                    <td>${new Date(student.registrationDate).toLocaleDateString()}</td>
                </tr>
            `;
                studentsTableBody.innerHTML += row;
            });

        } catch (error) {
            console.error('Erro ao carregar alunos:', error);
            studentsTableBody.innerHTML = `<tr><td colspan="4" style="text-align: center; color: red;">${error.message}</td></tr>`;
        }
    }

    // Lógica do Painel "Gerenciar Assinaturas"
    const actionSelector = document.getElementById('subscription-action-selector');
    const actionForms = document.querySelectorAll('.action-form');
    const actionResultsDiv = document.getElementById('action-results');

    actionSelector.addEventListener('change', function () {
        const selectedValue = this.value;
        actionForms.forEach(form => form.classList.toggle('active', form.id === `form-${selectedValue}`));
        actionResultsDiv.innerHTML = ''; // Limpa resultados ao trocar de ação
    });

    // Função genérica para exibir resultados
    function showResults(data, success = true) {
        const content = success ? JSON.stringify(data, null, 2) : `<p style="color:red;">${data.message}</p>`;
        actionResultsDiv.innerHTML = `<pre style="background-color:#f0f0f0; padding:1rem; border-radius:6px;">${content}</pre>`;
    }

    // Adiciona listeners de submit para cada formulário de ação
    document.getElementById('form-search').addEventListener('submit', async (e) => {
        e.preventDefault();
        const query = document.getElementById('search-id').value;
        try {
            const response = await fetch(`/api/admin/subscriptions/search?query=${encodeURIComponent(query)}`);
            const result = await response.json();
            if (!response.ok) throw result;
            showResults(result);
        } catch (error) {
            showResults(error, false);
        }
    });

    document.getElementById('form-update-value').addEventListener('submit', async (e) => {
        e.preventDefault();
        const id = document.getElementById('update-value-id').value;
        const amount = parseFloat(document.getElementById('update-value-amount').value);
        try {
            const response = await fetch(`/api/admin/subscriptions/${id}/value`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ transactionAmount: amount })
            });
            const result = await response.json();
            if (!response.ok) throw result;
            showResults(result);
        } catch (error) {
            showResults(error, false);
        }
    });

    // Listener para Pausar/Cancelar
    document.getElementById('form-pause-cancel').addEventListener('submit', async (e) => {
        e.preventDefault();
        const id = document.getElementById('pause-cancel-id').value;
        // Pega o status do botão que foi clicado
        const status = e.submitter.value;
        try {
            const response = await fetch(`/api/admin/subscriptions/${id}/status`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ status: status })
            });
            const result = await response.json();
            if (!response.ok) throw result;
            showResults(result);
        } catch (error) {
            showResults(error, false);
        }
    });

    // Listener para Reativar
    document.getElementById('form-reactivate').addEventListener('submit', async (e) => {
        e.preventDefault();
        const id = document.getElementById('reactivate-id').value;
        try {
            const response = await fetch(`/api/admin/subscriptions/${id}/status`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ status: 'authorized' }) // Reativar é mudar o status para 'authorized'
            });
            const result = await response.json();
            if (!response.ok) throw result;
            showResults(result);
        } catch (error) {
            showResults(error, false);
        }
    });
});