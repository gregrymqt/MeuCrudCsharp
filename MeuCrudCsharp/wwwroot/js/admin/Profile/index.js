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
        createPlanForm.addEventListener('submit', async function(e) {
            e.preventDefault();
            const saveButton = createPlanForm.querySelector('button[type="submit"]');
            saveButton.disabled = true;
            saveButton.textContent = 'Criando...';
            createPlanStatus.innerHTML = '<p>Enviando dados para o Mercado Pago...</p>';
            
            const planData = {
                reason: document.getElementById('plan-reason').value,
                auto_recurring: {
                    frequency: 1, // Fixo em 1 para simplicidade
                    frequency_type: document.getElementById('plan-type').value,
                    transaction_amount: parseFloat(document.getElementById('plan-amount').value),
                    currency_id: 'BRL' // Fixo em BRL
                },
                back_url: "https://www.seusite.com/confirmacao" // URL de retorno
            };
            
            try {
                // Você precisará criar este endpoint no backend
                // const response = await fetch('/api/admin/plans', { method: 'POST', ... });
                // const result = await response.json();
                // if(!response.ok) throw new Error(result.message);
                
                // Simulação de sucesso para fins de UI
                await new Promise(resolve => setTimeout(resolve, 1500));
                const simulatedResult = { id: '2c938084726fca480172750000000000' }; // ID de exemplo
                
                createPlanStatus.innerHTML = `<p style="color: var(--success-color);"><strong>Plano criado com sucesso!</strong> ID: ${simulatedResult.id}</p>`;
                createPlanForm.reset();

            } catch (error) {
                createPlanStatus.innerHTML = `<p style="color: var(--danger-color);">Erro: ${error.message}</p>`;
            } finally {
                saveButton.disabled = false;
                saveButton.textContent = 'Criar Plano';
            }
        });
    }

    // =====================================================================
    // Lógica do Painel "Alunos"
    // =====================================================================
    async function loadStudents() {
        const studentsTableBody = document.getElementById('students-table-body');
        studentsTableBody.innerHTML = '<tr><td colspan="4" style="text-align: center;">Carregando alunos...</td></tr>';
        
        try {
            // Você precisará criar este endpoint no backend
            // const response = await fetch('/api/admin/students');
            // const students = await response.json();

            // Simulação de dados para fins de UI
            await new Promise(resolve => setTimeout(resolve, 1000));
            const students = [
                { name: 'João da Silva', email: 'joao.silva@email.com', status: 'Ativa', date: new Date() },
                { name: 'Maria Oliveira', email: 'maria.o@email.com', status: 'Cancelada', date: new Date() },
                { name: 'Carlos Pereira', email: 'carlos.p@email.com', status: 'Pausada', date: new Date() }
            ];

            studentsTableBody.innerHTML = ''; // Limpa a tabela

            if (students.length === 0) {
                studentsTableBody.innerHTML = '<tr><td colspan="4" style="text-align: center;">Nenhum aluno encontrado.</td></tr>';
                return;
            }

            students.forEach(student => {
                const statusColor = student.status === 'Ativa' ? 'green' : (student.status === 'Cancelada' ? 'red' : 'orange');
                const row = `
                    <tr>
                        <td>${student.name}</td>
                        <td>${student.email}</td>
                        <td><span style="color: ${statusColor}; font-weight: 600;">${student.status}</span></td>
                        <td>${student.date.toLocaleDateString()}</td>
                    </tr>
                `;
                studentsTableBody.innerHTML += row;
            });

        } catch (error) {
            studentsTableBody.innerHTML = `<tr><td colspan="4" style="text-align: center; color: red;">${error.message}</td></tr>`;
        }
    }

    // =====================================================================
    // Lógica do Painel "Gerenciar Assinaturas"
    // =====================================================================
    const actionSelector = document.getElementById('subscription-action-selector');
    const actionForms = document.querySelectorAll('.action-form');

    if (actionSelector) {
        actionSelector.addEventListener('change', function() {
            const selectedValue = this.value;
            actionForms.forEach(form => {
                // O método toggle com o segundo argumento booleano simplifica a lógica
                form.classList.toggle('active', form.id === `form-${selectedValue}`);
            });
        });

        // Adiciona listeners de submit para cada formulário de ação
        actionForms.forEach(form => {
            form.addEventListener('submit', function(e) {
                e.preventDefault();
                const actionType = this.id.replace('form-', '');
                alert(`Lógica para a ação "${actionType}" via API aqui.`);
                // Aqui você faria a chamada fetch para o endpoint correspondente
            });
        });
    }
});