/**
 * @file Manages all client-side logic for the admin dashboard, including navigation,
 * data fetching with caching, and CRUD operations for Plans, Courses, and Students.
 */
document.addEventListener('DOMContentLoaded', function() {
    // --- Sidebar Navigation & Panel Switching ---
    const sidebarLinks = document.querySelectorAll('.sidebar-link');
    const contentPanels = document.querySelectorAll('.content-panel');

    sidebarLinks.forEach(link => {
        link.addEventListener('click', (e) => {
            e.preventDefault();

            // Remove 'active' class from all links and panels
            sidebarLinks.forEach(l => l.classList.remove('active'));
            contentPanels.forEach(c => c.classList.remove('active'));

            // Add 'active' to the clicked link and its corresponding panel
            link.classList.add('active');
            const contentId = link.id.replace('nav-', 'content-');
            const activePanel = document.getElementById(contentId);
            if (activePanel) {
                activePanel.classList.add('active');
            }

            // Load data for the clicked tab, but only once.
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
                activePanel.dataset.loaded = 'true'; // Mark panel as loaded
            }
        });
    });

    // --- Plan Editing Modal Elements ---
    const editModal = document.getElementById('edit-plan-modal');
    const editForm = document.getElementById('edit-plan-form');
    const closeEditModalButton = document.getElementById('close-edit-plan-modal');

    // Campos do formulário de edição do planos
    const editPlanId = document.getElementById('edit-plan-id');
    const editPlanReason = document.getElementById('edit-plan-reason');
    const editPlanAmount = document.getElementById('edit-plan-amount');
    const editPlanFrequencyType = document.getElementById('edit-plan-frequency-type');
    const editPlanBackUrl = document.getElementById('edit-plan-back-url');

    // --- Course Editing Modal Elements ---
    const editCourseModal = document.getElementById('edit-course-modal');
    const editCourseForm = document.getElementById('edit-course-form');
    const closeEditCourseModalButton = document.getElementById('close-edit-course-modal');
    const editCourseId = document.getElementById('edit-course-id');
    const editCourseName = document.getElementById('edit-course-name');
    const editCourseDescription = document.getElementById('edit-course-description');


    // --- Course Management Elements ---
    const coursesTableBody = document.getElementById('courses-table-body');
    const searchCourseInput = document.getElementById('search-course-input');
    const createCourseForm = document.getElementById('create-course-form');

    // Simple session-level cache to avoid re-fetching data.
    const sessionCache = {};

    /**
     * Fetches data from a given API endpoint, caches it in the session, and renders it.
     * If the data is already in the cache, it uses the cached version instead of fetching.
     * @param {string} cacheKey - The key to use for storing/retrieving data from the session cache.
     * @param {string} apiUrl - The URL of the API endpoint to fetch data from.
     * @param {function} renderFunction - The function to call to render the fetched data.
     * @param {HTMLElement} tableBody - The table body element to display loading/error messages.
     */
    async function fetchAndCache(cacheKey, apiUrl, renderFunction, tableBody) {
        if (sessionCache[cacheKey]) {
            console.log(`Loading '${cacheKey}' from session cache.`);
            renderFunction(sessionCache[cacheKey]);
            return;
        }

        try {
            tableBody.innerHTML = `<tr><td colspan="5" class="text-center">Loading...</td></tr>`;
            const response = await fetch(apiUrl);
            if (!response.ok) {
                throw new Error('Failed to fetch data from the server.');
            }
            const data = await response.json();

            sessionCache[cacheKey] = data;
            renderFunction(data);

        } catch (error) {
            console.error(`Error loading '${cacheKey}':`, error);
            tableBody.innerHTML = `<tr><td colspan="5" class="text-center text-danger">${error.message}</td></tr>`;
        }
    }

    const API_COURSES_URL = '/api/admin/courses';

    /**
     * Loads and renders the list of courses.
     */
    async function loadCourses() {await fetchAndCache('allCourses', API_COURSES_URL, renderCoursesTable, coursesTableBody);}

    /**
     * Renders the list of courses into the corresponding table.
     * @param {Array<object>} courses - An array of course objects to render.
     */
    function renderCoursesTable(courses) {
        coursesTableBody.innerHTML = '';
        if (!courses || courses.length === 0) {
            coursesTableBody.innerHTML = '<tr><td colspan="3" class="text-center">No courses found.</td></tr>';
            return;
        }

        courses.forEach(course => {
            const row = document.createElement('tr');
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

    /**
     * Handles the submission of the 'Create Course' form.
     * Sends a POST request to the API and updates the UI on success or failure.
     */
    if (createCourseForm) {
        createCourseForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            const submitButton = createCourseForm.querySelector('button[type="submit"]');
            submitButton.disabled = true;
            submitButton.textContent = 'Saving...';

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
                    throw new Error(errorData.message || 'Failed to create the course.');
                }

                Swal.fire({
                    title: 'Success!',
                    text: 'Course created successfully!',
                    icon: 'success',
                    timer: 2000,
                    showConfirmButton: false
                });

                createCourseForm.reset();
                await loadCourses(); // Recarrega a lista

            } catch (error) {
                console.error('Error creating course:', error);
                Swal.fire({
                    title: 'Error!',
                    text: error.message,
                    icon: 'error'
                });
            } finally {
                submitButton.disabled = false;
                submitButton.textContent = 'Save Course';
            }
        });
    }

    /**
     * Opens the edit modal and populates it with the selected course's data.
     * @param {object} course - The course object to edit.
     */
    function openEditCourseModal(course) {
        editCourseId.value = course.id;
        editCourseName.value = course.name;
        editCourseDescription.value = course.description || '';
        editCourseModal.style.display = 'block';
    }

    /**
     * Closes and resets the course edit modal.
     */
    function closeEditCourseModal() {
        editCourseModal.style.display = 'none';
        editCourseForm.reset();
    }

    // Handles the submission of the course edit form.
    editCourseForm.addEventListener('submit', async (e) => {
        e.preventDefault();
        const courseId = editCourseId.value;
        const submitButton = editCourseForm.querySelector('button[type="submit"]');
        submitButton.disabled = true;
        submitButton.textContent = 'Saving...';

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
                throw new Error(errorData.message || 'Failed to update the course.');
            }

            Swal.fire({
                title: 'Updated!',
                text: 'Course updated successfully.',
                icon: 'success',
                timer: 2000,
                showConfirmButton: false
            });

            closeEditCourseModal();
            await loadCourses(); // Recarrega a lista

        } catch (error) {
            console.error('Error updating course:', error);
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

    /**
     * Prompts the user for confirmation and then deletes a course.
     * @param {string} courseId - The ID of the course to delete.
     */
    async function deleteCourse(courseId) {
        // Use SweetAlert for a better confirmation dialog.
        const result = await Swal.fire({
            title: 'Are you sure?',
            text: "This action cannot be undone and may fail if there are associated videos.",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#d33',
            cancelButtonColor: '#6c757d',
            confirmButtonText: 'Yes, delete it!',
            cancelButtonText: 'Cancel'
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
                throw new Error(errorData.message || 'Failed to delete the course.');
            }

            Swal.fire(
                'Deleted!',
                'The course has been deleted.',
                'success'
            );

            await loadCourses(); // Recarrega a lista

        } catch (error) {
            console.error('Error deleting course:', error);
            Swal.fire(
                'Error!',
                error.message,
                'error'
            );
        }
    }

    // =====================================================================
    // == PLAN MANAGEMENT ==
    // =====================================================================

    const API_PLANS_URL = '/api/admin/plans';
    const createPlanForm = document.getElementById('create-plan-form');
    const plansTableBody = document.getElementById('plans-table-body');

    /**
     * Loads and renders the list of subscription plans.
     */
    async function loadPlans() {
        await fetchAndCache('allPlans', API_PLANS_URL, renderPlansTable, plansTableBody);
    }

    function renderPlansTable(plans) {
        plansTableBody.innerHTML = '';
        if (!plans || plans.length === 0) {
            plansTableBody.innerHTML = '<tr><td colspan="5" class="text-center">No plans found.</td></tr>';
            return;
        }

        plans.forEach(plan => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${plan.reason || 'N/A'}</td>
                <td>${plan.autoRecurring?.frequencyType === 'years' ? 'Annual' : 'Monthly'}</td>
                <td>$${plan.autoRecurring?.transactionAmount.toFixed(2)}</td>
                <td><span class="status-badge status-${plan.status?.toLowerCase()}">${plan.status || 'N/A'}</span></td>
                <td class="actions">
                    <button class="btn btn-secondary btn-sm btn-edit" data-plan-id="${plan.id}">Editar</button>
                    <button class="btn btn-danger btn-sm btn-delete" data-plan-id="${plan.id}">Excluir</button>
                </td>
            `;
            plansTableBody.appendChild(row);
        });
    }

    /**
     * Handles the submission of the 'Create Plan' form.
     */
    const createPlanStatus = document.getElementById('create-plan-status');
    if (createPlanForm) {
        createPlanForm.addEventListener('submit', async function (e) {
            e.preventDefault();
            const saveButton = createPlanForm.querySelector('button[type="submit"]');
            saveButton.disabled = true;
            saveButton.textContent = 'Creating...';
            createPlanStatus.innerHTML = '<p>Sending data to payment provider...</p>';

            const planData = {
                reason: document.getElementById('plan-reason').value,
                autoRecurring: {
                    frequency: 1, // Fixed to 1 for simplicity
                    frequencyType: document.getElementById('plan-type').value,
                    transactionAmount: parseFloat(document.getElementById('plan-amount').value),
                },
                backUrl: "https://www.yoursite.com/confirmation" // Return URL
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
                    throw new Error(result.message || 'An error occurred while creating the plan.');
                }
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
    }

    /**
     * Opens the edit modal and populates it with the selected plan's data.
     * @param {object} plan - The plan object to edit.
     */
    function openEditModal(plan) {
        editPlanId.value = plan.id;
        editPlanReason.value = plan.reason;
        editPlanAmount.value = plan.autoRecurring.transactionAmount;
        editPlanFrequencyType.value = plan.autoRecurring.frequencyType;
        editPlanBackUrl.value = plan.back_url || ''; // Opcional
        editModal.style.display = 'block';
    }

    /**
     * Closes and resets the plan edit modal.
     */
    function closeEditModal() {
        editModal.style.display = 'none';
        editForm.reset();
    }

    // Handles the submission of the plan edit form.
    editForm.addEventListener('submit', async (e) => {
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
            const response = await fetch(`${API_PLANS_URL}/${planId}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(updatedData)
            });

            if (!response.ok) {
                const errorData = await response.json().catch(() => null);
                throw new Error(errorData?.message || 'Failed to update the plan.');
            }

            Swal.fire({
                title: 'Success!',
                text: 'Plan updated successfully!',
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

    /**
     * Prompts the user for confirmation and then deletes a plan.
     * @param {string} planId - The ID of the plan to delete.
     */
    async function deletePlan(planId) {
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

        if (!result.isConfirmed) {
            return;
        }

        try {
            const response = await fetch(`${API_PLANS_URL}/${planId}`, {
                method: 'DELETE'
            });

            if (!response.ok) {
                const errorData = await response.json().catch(() => null);
                throw new Error(errorData?.message || 'Falha ao excluir o plano.');
            }

            Swal.fire(
                'Deleted!',
                'The plan has been deleted.',
                'success'
            );

            await loadPlans();

        } catch (error) {
            console.error('Error deleting plan:', error);
            Swal.fire(
                'Error!',
                error.message,
                'error'
            );
        }
    }

    /**
     * Filters the courses table based on user input in the search field.
     */
    searchCourseInput.addEventListener('input', (e) => {
        const searchTerm = e.target.value.toLowerCase();
        const filteredCourses = allCoursesCache.filter(course =>
            course.name.toLowerCase().includes(searchTerm) ||
            (course.description && course.description.toLowerCase().includes(searchTerm))
        );
        renderCoursesTable(filteredCourses);
    });

    //
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