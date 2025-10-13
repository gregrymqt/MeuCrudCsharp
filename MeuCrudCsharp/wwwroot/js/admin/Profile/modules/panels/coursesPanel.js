// /js/admin/modules/panels/coursesPanel.js

import * as api from '../api/adminAPI.js';
import {openModal, closeModal} from '../ui/modals.js';

// --- Seletores de DOM ---
const coursesTableBody = document.getElementById('courses-table-body');
const createCourseForm = document.getElementById('create-course-form');
const editCourseModal = document.getElementById('edit-course-modal');
const editCourseForm = document.getElementById('edit-course-form');
const editCourseId = document.getElementById('edit-course-id');
const editCourseName = document.getElementById('edit-course-name');
const editCourseDescription = document.getElementById('edit-course-description');

// NOVO: Seletores para o formulário de busca
const searchCourseForm = document.getElementById('search-course-form');
const searchCourseInput = document.getElementById('search-course-input');

// --- Funções de Renderização e Ações ---

function renderCoursesTable(courses) {
    coursesTableBody.innerHTML = ''; // Limpa a tabela

    if (!courses || courses.length === 0) {
        coursesTableBody.innerHTML = '<tr><td colspan="3" class="text-center empty-state">Nenhum curso encontrado.</td></tr>';
        return;
    }

    courses.forEach(course => {
        // Verificação segura das propriedades
        const coursePublicId = course.publicId ?? 'ID_INVALIDO';
        const name = course.name ?? 'Curso sem nome';
        const description = course.description || 'Sem descrição'; // '||' funciona bem para strings vazias

        const row = document.createElement('tr');
        row.innerHTML = `
    <td>${name}</td>
    <td>${description}</td>
    <td class="text-right">
        <button type="button" class="btn btn-secondary btn-sm btn-edit-course" data-course-public-id="${coursePublicId}">Editar</button>
        <button type="button" class="btn btn-danger btn-sm btn-delete-course" data-course-public-id="${coursePublicId}">Excluir</button>
    </td>
`;
        coursesTableBody.appendChild(row);
    });
}

async function handleCourseDelete(courseId) {
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
    if (result.isConfirmed) {
        try {
            await api.deleteCourse(courseId);
            api.invalidateCache('allCourses');
            await loadCourses();
            Swal.fire('Excluído!', 'O curso foi excluído.', 'success');
        } catch (error) {
            Swal.fire('Erro!', error.message, 'error');
        }
    }
}

function openEditCourseModal(course) {
    editCourseId.value = course.publicId;
    editCourseName.value = course.name;
    editCourseDescription.value = course.description || '';
    openModal(editCourseModal);
}

// --- Funções Principais de Carregamento e Inicialização ---

// Função que lida com a resposta da API e chama a renderização
function handleApiResponse(response) {
    let coursesList = []; // Inicia uma lista vazia

    // CASO 1: A resposta é um objeto de paginação (tem a propriedade 'items')
    if (response && Array.isArray(response.items)) {
        coursesList = response.items;

        // BÔNUS: Aqui você pode atualizar os controles de paginação da sua tela
        // usando response.pageNumber, response.totalPages, etc.
    }
    // CASO 2: A resposta é um array simples (resultado da busca)
    else if (Array.isArray(response)) {
        coursesList = response;
    }
    // Se não for nenhum dos casos, a lista permanece vazia.

    // Agora sim, chamamos a função de renderização com a lista correta
    renderCoursesTable(coursesList);
}

// Agora, suas funções de carregar dados ficam mais simples:

// Função para carregar os cursos paginados
export async function loadCourses() {
    try {
        coursesTableBody.innerHTML = `<tr><td colspan="3" class="text-center">Carregando...</td></tr>`;
        const response = await api.getCourses(); // Chama a rota paginada
        handleApiResponse(response); // Usa o manipulador inteligente
        initializeCoursesPanel();
    } catch (error) {
        coursesTableBody.innerHTML = `<tr><td colspan="3" class="text-center text-danger">${error.message}</td></tr>`;
    }
}

// Função para carregar os resultados da busca
export async function searchCourses(name) {
    const submitButton = searchCourseForm.querySelector('button');

    try {
        searchCourseInput.disabled = true; // Desativa o input
        if (submitButton) submitButton.disabled = true; // Desativa o botão
        coursesTableBody.innerHTML = `<tr><td colspan="3" class="text-center"><div class="loading-spinner"></div></td></tr>`; // Mostra um spinner

        const response = await api.searchCoursesByName(name);
        handleApiResponse(response);
    } catch (error) {
        coursesTableBody.innerHTML = `<tr><td colspan="3" class="text-center text-danger">${error.message}</td></tr>`;
    } finally {
        searchCourseInput.disabled = false;
        if (submitButton) submitButton.disabled = false;
    }
}

function initializeCoursesPanel() {
    if (searchCourseForm && !searchCourseForm.hasAttribute('data-events-attached')) {
        let debounceTimer;

        searchCourseForm.addEventListener('submit', (e) => {
            e.preventDefault();
            const searchTerm = searchCourseInput.value.trim();
            searchCourses(searchTerm);
        });

        const searchCourseInput = document.getElementById('search-course-input'); // Use o ID correto
        searchCourseInput.addEventListener('input', () => {
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(() => {
                const searchTerm = searchCourseInput.value.trim();
                if (searchTerm === '') {
                    loadCourses();
                } else {
                    searchCourses(searchTerm);
                }
            }, 500);
        });

        searchCourseForm.setAttribute('data-events-attached', 'true');
        console.log('Listeners do formulário de busca de cursos foram anexados.');
    }

    if (coursesTableBody && !coursesTableBody.hasAttribute('data-click-listener-attached')) {
        coursesTableBody.addEventListener('click', async (e) => {
            const target = e.target.closest('button[data-course-public-id]');
            if (!target) return;

            const courseId = target.dataset.coursePublicId;
            if (!courseId) {
                alert("ERRO: ID do curso não encontrado no elemento do botão!");
                return;
            }

            try {
                if (target.classList.contains('btn-edit-course')) {
                    const course = await api.getCoursesPublicId(courseId);
                    openEditCourseModal(course);
                }
                if (target.classList.contains('btn-delete-course')) {
                    await handleCourseDelete(courseId);
                }
            } catch (error) {
                console.error('Falha na ação do curso:', error);
            }
        });

        coursesTableBody.setAttribute('data-click-listener-attached', 'true');
        console.log('Listener da tabela de cursos foi anexado.');
    }

    if (createCourseForm && !createCourseForm.hasAttribute('data-submit-listener-attached')) {

        createCourseForm.addEventListener('submit', async (e) => {
            // ... (código existente de criação)
            e.preventDefault();
            const submitButton = createCourseForm.querySelector('button[type="submit"]');
            submitButton.disabled = true;
            submitButton.textContent = 'Saving...';

            const courseData = {
                name: document.getElementById('course-name-new').value,
                description: document.getElementById('course-description-new').value
            };

            try {
                const response = await api.createCourse(courseData);
                Swal.fire({
                    title: 'Success!',
                    text: `Course created successfully! ID: ${response.id}`,
                    icon: 'success',
                    timer: 2000,
                    showConfirmButton: false
                });
                createCourseForm.reset();
                await loadCourses();
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
        createCourseForm.setAttribute('data-submit-listener-attached', 'true');
        console.log('Listener do formulário de criação de cursos foi anexado.');
    }

    if (editCourseForm && !editCourseForm.hasAttribute('data-submit-listener-attached')) {

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
                const response = await api.updateCourse(courseId, updatedData);
                Swal.fire({
                    title: 'Updated!',
                    text: `Course updated successfully! ID: ${response.id}`,
                    icon: 'success',
                    timer: 2000,
                    showConfirmButton: false
                });
                closeModal(editCourseModal); // Correção: use a função importada
                api.invalidateCache('allCourses'); // Invalida o cache para forçar recarga
                await loadCourses();
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
        editCourseForm.setAttribute('data-submit-listener-attached', 'true');
        console.log('Listener do formulário de edição de cursos foi anexado.');
    }

    document.getElementById('close-edit-course-modal')?.addEventListener('click', () => closeModal(editCourseModal));
}