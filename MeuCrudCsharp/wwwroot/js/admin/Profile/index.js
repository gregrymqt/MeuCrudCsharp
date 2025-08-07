document.addEventListener('DOMContentLoaded', function() {
    // =====================================================================
    // Lógica de Navegação da Sidebar
    // =====================================================================
    const sidebarLinks = document.querySelectorAll('.sidebar-link');
    const contentPanels = document.querySelectorAll('.content-panel');

    sidebarLinks.forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();

            // Remove a classe 'active' de todos
            sidebarLinks.forEach(l => l.classList.remove('active'));
            contentPanels.forEach(c => c.classList.remove('active'));

            // Adiciona 'active' ao link e ao painel correspondente
            link.classList.add('active');
            const contentId = link.id.replace('nav-', 'content-');
            const activePanel = document.getElementById(contentId);
            if (activePanel) {
                activePanel.classList.add('active');
            }

            // Lógica para carregar os dados da aba clicada (apenas uma vez)
            if (activePanel && !activePanel.dataset.loaded) {
                switch (link.id) {
                    case 'nav-plans':
                        loadPlans();
                        break;
                    case 'nav-courses':
                        loadCourses();
                        break;
                    case 'nav-students':
                        loadStudents();
                        break;
                }
                activePanel.dataset.loaded = 'true'; // Marca o painel como carregado
            }
        });
    });

    const editModal = document.getElementById('edit-plan-modal');
    const editForm = document.getElementById('edit-plan-form');
    const closeEditModalButton = document.getElementById('close-edit-plan-modal');

    // Campos do formulário de edição do planos
    const editPlanId = document.getElementById('edit-plan-id');
    const editPlanReason = document.getElementById('edit-plan-reason');
    const editPlanAmount = document.getElementById('edit-plan-amount');
    const editPlanFrequencyType = document.getElementById('edit-plan-frequency-type');
    const editPlanBackUrl = document.getElementById('edit-plan-back-url');

    // Modal de Edição de Curso
    const editCourseModal = document.getElementById('edit-course-modal');
    const editCourseForm = document.getElementById('edit-course-form');
    const closeEditCourseModalButton = document.getElementById('close-edit-course-modal');
    const editCourseId = document.getElementById('edit-course-id');
    const editCourseName = document.getElementById('edit-course-name');
    const editCourseDescription = document.getElementById('edit-course-description');


    // --- Seleção de Elementos DOM para Cursos ---
    const coursesTableBody = document.getElementById('courses-table-body');
    const searchCourseInput = document.getElementById('search-course-input');
    const createCourseForm = document.getElementById('create-course-form'); // Adicionado para o CREATE


     // Endpoint base para os planos
    const sessionCache = {};

    // =====================================================================
    // READ: Buscar e Renderizar os Planos e Cursos
    // =====================================================================
    async function fetchAndCache(cacheKey, apiUrl, renderFunction, tableBody) {
        // 1. Tenta carregar do cache primeiro
        if (sessionCache[cacheKey]) {
            console.log(`Carregando '${cacheKey}' do cache da sessão.`);
            renderFunction(sessionCache[cacheKey]);
            return;
        }

        // 2. Se não estiver no cache, busca na API
        try {
            tableBody.innerHTML = `<tr><td colspan="5" class="text-center">Carregando...</td></tr>`;
            const response = await fetch(apiUrl);
            if (!response.ok) {
                throw new Error('Falha ao buscar os dados do servidor.');
            }
            const data = await response.json();

            // 3. Armazena no cache e renderiza
            sessionCache[cacheKey] = data;
            renderFunction(data);

        } catch (error) {
            console.error(`Erro ao carregar '${cacheKey}':`, error);
            tableBody.innerHTML = `<tr><td colspan="5" class="text-center text-danger">${error.message}</td></tr>`;
        }
    }

    const API_COURSES_URL = '/api/admin/courses';

    async function loadCourses() {await fetchAndCache('allCourses', API_COURSES_URL, renderCoursesTable, coursesTableBody);}

    function renderCoursesTable(courses) {
        coursesTableBody.innerHTML = '';
        if (!courses || courses.length === 0) {
            coursesTableBody.innerHTML = '<tr><td colspan="3" class="text-center">Nenhum curso encontrado.</td></tr>';
            return;
        }

        courses.forEach(course => {
            const row = document.createElement('tr');
            // Adicionamos data attributes para facilitar a seleção dos botões
            row.innerHTML = `
                <td>${course.name}</td>
                <td>${course.description || 'Sem descrição'}</td>
                <td class="actions">
                    <button class="btn btn-secondary btn-sm btn-edit-course" data-course-id="${course.id}">Editar</button>
                    <button class="btn btn-danger btn-sm btn-delete-course" data-course-id="${course.id}">Excluir</button>
                </td>
            `;
            coursesTableBody.appendChild(row);
        });
    }

    // =====================================================================
    // CREATE: Criar um Novo Curso
    // =====================================================================
    if (createCourseForm) {
        createCourseForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const submitButton = createCourseForm.querySelector('button[type="submit"]');
            submitButton.disabled = true;
            submitButton.textContent = 'Salvando...';

            const courseData = {
                name: document.getElementById('course-name-new').value,
                description: document.getElementById('course-description-new').value
            };

            try {
                const response = await fetch(API_COURSES_URL, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(courseData)
                });

                if (!response.ok) {
                    const errorData = await response.json();
                    throw new Error(errorData.message || 'Falha ao criar o curso.');
                }

                // --- MUDANÇA AQUI ---
                Swal.fire({
                    title: 'Sucesso!',
                    text: 'Curso criado com sucesso!',
                    icon: 'success',
                    timer: 2000,
                    showConfirmButton: false
                });

                createCourseForm.reset();
                await loadCourses(); // Recarrega a lista

            } catch (error) {
                console.error('Erro ao criar curso:', error);
                // --- MUDANÇA AQUI ---
                Swal.fire({
                    title: 'Erro!',
                    text: error.message,
                    icon: 'error'
                });
            } finally {
                submitButton.disabled = false;
                submitButton.textContent = 'Salvar Curso';
            }
        });
    }

    // =====================================================================
    // UPDATE: Abrir e Submeter o Modal de Edição
    // =====================================================================
    function openEditCourseModal(course) {
        editCourseId.value = course.id;
        editCourseName.value = course.name;
        editCourseDescription.value = course.description || '';
        editCourseModal.style.display = 'block';
    }

    function closeEditCourseModal() {
        editCourseModal.style.display = 'none';
        editCourseForm.reset();
    }

    editCourseForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const courseId = editCourseId.value;
        const submitButton = editCourseForm.querySelector('button[type="submit"]');
        submitButton.disabled = true;
        submitButton.textContent = 'Salvando...';

        const updatedData = {
            name: editCourseName.value,
            description: editCourseDescription.value
        };

        try {
            const response = await fetch(`${API_COURSES_URL}/${courseId}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(updatedData)
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Falha ao atualizar o curso.');
            }

            // --- MUDANÇA AQUI ---
            Swal.fire({
                title: 'Atualizado!',
                text: 'Curso atualizado com sucesso.',
                icon: 'success',
                timer: 2000,
                showConfirmButton: false
            });

            closeEditCourseModal();
            await loadCourses(); // Recarrega a lista

        } catch (error) {
            console.error('Erro ao atualizar curso:', error);
            // --- MUDANÇA AQUI ---
            Swal.fire({
                title: 'Erro!',
                text: error.message,
                icon: 'error'
            });
        } finally {
            submitButton.disabled = false;
            submitButton.textContent = 'Salvar Alterações';
        }
    });

    // =====================================================================
    // DELETE: Excluir um Curso
    // =====================================================================
    async function deleteCourse(courseId) {
        // --- MUDANÇA AQUI ---
        // Usamos o Swal.fire para pedir confirmação
        const result = await Swal.fire({
            title: 'Você tem certeza?',
            text: "Esta ação não pode ser desfeita e pode falhar se houver vídeos associados.",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Sim, excluir!',
            cancelButtonText: 'Cancelar'
        });

        // Se o usuário clicou em "Sim, excluir!", o resultado será confirmado.
        if (!result.isConfirmed) {
            return;
        }

        try {
            const response = await fetch(`${API_COURSES_URL}/${courseId}`, {
                method: 'DELETE'
            });

            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Falha ao excluir o curso.');
            }

            // --- MUDANÇA AQUI ---
            Swal.fire(
                'Excluído!',
                'O curso foi excluído com sucesso.',
                'success'
            );

            await loadCourses(); // Recarrega a lista

        } catch (error) {
            console.error('Erro ao excluir curso:', error);
            // --- MUDANÇA AQUI ---
            Swal.fire(
                'Erro!',
                error.message,
                'error'
            );
        }
    }

    // =====================================================================
    // READ: Buscar e Renderizar os Planos
    // =====================================================================

    const API_PLANS_URL = '/api/admin/plans';
    const createPlanForm = document.getElementById('create-plan-form');
    const plansTableBody = document.getElementById('plans-table-body');

    async function loadPlans() {
        await fetchAndCache('allPlans', API_PLANS_URL, renderPlansTable, plansTableBody);
    }

    function renderPlansTable(plans) {
        plansTableBody.innerHTML = ''; // Limpa a tabela
        if (!plans || plans.length === 0) {
            plansTableBody.innerHTML = '<tr><td colspan="5" class="text-center">Nenhum plano encontrado.</td></tr>';
            return;
        }

        plans.forEach(plan => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${plan.reason || 'N/A'}</td>
                <td>${plan.autoRecurring?.frequencyType === 'years' ? 'Anual' : 'Mensal'}</td>
                <td>R$ ${plan.autoRecurring?.transactionAmount.toFixed(2).replace('.', ',')}</td>
                <td><span class="status-badge status-${plan.status?.toLowerCase()}">${plan.status || 'N/A'}</span></td>
                <td class="actions">
                    <button class="btn btn-secondary btn-sm btn-edit" data-plan-id="${plan.id}">Editar</button>
                    <button class="btn btn-danger btn-sm btn-delete" data-plan-id="${plan.id}">Excluir</button>
                </td>
            `;
            plansTableBody.appendChild(row);
        });
    }

    // =====================================================================
    // Lógica do Painel "Criar Plano"
    // =====================================================================
   
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
                await Swal.fire({
                    title: 'Sucesso!',
                    text: `Plano criado com sucesso! ID: ${result.id}`,
                    icon: 'success'
                });

                createPlanForm.reset();
                await loadPlans();
            } catch (error) {
                Swal.fire({
                    title: 'Erro!',
                    text: error.message,
                    icon: 'error'
                });
            } finally {
                saveButton.disabled = false;
                saveButton.textContent = 'Criar Plano';
            }
        });
    }

    // =====================================================================
    // UPDATE: Abrir e Submeter o Modal de Edição do Plano
    // =====================================================================
    function openEditModal(plan) {
        editPlanId.value = plan.id;
        editPlanReason.value = plan.reason;
        editPlanAmount.value = plan.autoRecurring.transactionAmount;
        editPlanFrequencyType.value = plan.autoRecurring.frequencyType;
        editPlanBackUrl.value = plan.back_url || ''; // Opcional
        editModal.style.display = 'block';
    }

    function closeEditModal() {
        editModal.style.display = 'none';
        editForm.reset();
    }

    editForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const planId = editPlanId.value;
        const submitButton = editForm.querySelector('button[type="submit"]');
        submitButton.disabled = true;
        submitButton.textContent = 'Salvando...';

        const updatedData = {
            reason: editPlanReason.value,
            back_url: editPlanBackUrl.value,
            auto_recurring: {
                transaction_amount: parseFloat(editPlanAmount.value)
            }
        };

        try {
            const response = await fetch(`${API_BASE_URL}/${planId}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(updatedData)
            });

            if (!response.ok) {
                // Tenta extrair uma mensagem de erro mais detalhada do backend
                const errorData = await response.json().catch(() => null);
                throw new Error(errorData?.message || 'Falha ao atualizar o plano.');
            }

            // --- MUDANÇA AQUI ---
            Swal.fire({
                title: 'Sucesso!',
                text: 'Plano atualizado com sucesso!',
                icon: 'success',
                timer: 2000,
                showConfirmButton: false
            });

            closeEditModal();
            await loadPlans(); // Recarrega a lista

        } catch (error) {
            console.error('Erro ao atualizar plano:', error);
            // --- MUDANÇA AQUI ---
            Swal.fire({
                title: 'Erro!',
                text: error.message,
                icon: 'error'
            });
        } finally {
            submitButton.disabled = false;
            submitButton.textContent = 'Salvar Alterações';
        }
    }); 

    // =====================================================================
    // DELETE: Excluir um Plano
    // =====================================================================
    async function deletePlan(planId) {
        // --- MUDANÇA AQUI ---
        // Substituindo o confirm() nativo pelo SweetAlert2
        const result = await Swal.fire({
            title: 'Você tem certeza?',
            text: "Esta ação não pode ser desfeita.",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Sim, excluir!',
            cancelButtonText: 'Cancelar'
        });

        // Se o usuário não confirmou, a função para aqui.
        if (!result.isConfirmed) {
            return;
        }

        try {
            const response = await fetch(`${API_BASE_URL}/${planId}`, {
                method: 'DELETE'
            });

            if (!response.ok) {
                const errorData = await response.json().catch(() => null);
                throw new Error(errorData?.message || 'Falha ao excluir o plano.');
            }

            // --- MUDANÇA AQUI ---
            Swal.fire(
                'Excluído!',
                'O plano foi excluído com sucesso.',
                'success'
            );

            await loadPlans(); // Recarrega a lista

        } catch (error) {
            console.error('Erro ao excluir plano:', error);
            // --- MUDANÇA AQUI ---
            Swal.fire(
                'Erro!',
                error.message,
                'error'
            );
        }
    }

    // =====================================================================
    // SEARCH: Filtrar a tabela de cursos
    // =====================================================================
    searchCourseInput.addEventListener('input', (e) => {
        const searchTerm = e.target.value.toLowerCase();
        const filteredCourses = allCoursesCache.filter(course =>
            course.name.toLowerCase().includes(searchTerm) ||
            (course.description && course.description.toLowerCase().includes(searchTerm))
        );
        renderCoursesTable(filteredCourses);
    });

    // =====================================================================
    // Event Listeners
    // =====================================================================
    closeEditModalButton.addEventListener('click', closeEditModal);
    closeEditCourseModalButton.addEventListener('click', closeEditCourseModal);

    // Delegação de eventos para os botões de editar e excluir
    plansTableBody.addEventListener('click', async (e) => {
        const target = e.target;
        if (target.classList.contains('btn-edit')) {
            const planId = target.dataset.planId;
            // Busca os dados completos do plano para preencher o modal
            const response = await fetch(`${API_BASE_URL}/${planId}`);
            const plan = await response.json();
            openEditModal(plan);
        }
        if (target.classList.contains('btn-delete')) {
            const planId = target.dataset.planId;
            await deletePlan(planId);
        }
    });

    // Delegação de eventos para os botões na tabela de cursos
    coursesTableBody.addEventListener('click', (e) => {
        const target = e.target;
        const courseId = target.dataset.courseId;

        if (target.classList.contains('btn-edit-course')) {
            const course = allCoursesCache.find(c => c.id === courseId);
            if (course) {
                openEditCourseModal(course);
            }
        }
        if (target.classList.contains('btn-delete-course')) {
            deleteCourse(courseId);
        }
    });

    // Lógica do Painel "Alunos"
    function renderStudentsTable(students) {
        const studentsTableBody = document.getElementById('students-table-body');
        studentsTableBody.innerHTML = ''; // Limpa a tabela

        if (!students || students.length === 0) {
            studentsTableBody.innerHTML = '<tr><td colspan="4" style="text-align: center;">Nenhum aluno encontrado.</td></tr>';
            return;
        }

        students.forEach(student => {
            // Lógica para definir a cor do status
            let statusColor = 'grey'; // Padrão
            const status = student.subscriptionStatus?.toLowerCase() || '';

            if (status === 'approved' || status === 'ativa') {
                statusColor = 'green';
            } else if (status === 'cancelled' || status === 'cancelada') {
                statusColor = 'red';
            } else if (status === 'paused' || status === 'pausada') {
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
    }

    /**
     * Busca os dados dos alunos, utilizando a função de cache.
     */
    async function loadStudents() {
        const studentsTableBody = document.getElementById('students-table-body');
        // Reutiliza a função fetchAndCache para buscar e cachear os dados dos alunos
        await fetchAndCache('allStudents', '/api/admin/students', renderStudentsTable, studentsTableBody);
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