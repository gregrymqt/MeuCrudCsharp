// /js/admin/modules/panels/coursesPanel.js

import * as api from '../api/adminAPI.js';
import {openModal, closeModal} from '../ui/modals.js';
import {initializePagination, updatePaginationState} from "../ui/pagination.js";

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

const COURSES_PER_PAGE = 10;
// --- Funções de Renderização e Ações ---

function renderCoursesTable(courses) {
    const coursesTableBody = document.getElementById('courses-table-body'); // Garanta que essa variável esteja acessível
    coursesTableBody.innerHTML = ''; // Limpa a tabela

    if (!courses || courses.length === 0) {
        coursesTableBody.innerHTML = '<tr><td colspan="3" class="text-center empty-state">Nenhum curso encontrado.</td></tr>';
        return;
    }

    courses.forEach(course => {
        const coursePublicId = course.publicId ?? 'ID_INVALIDO';
        const name = course.name ?? 'Curso sem nome';
        const description = course.description || 'Sem descrição';

        const row = document.createElement('tr');
        row.innerHTML = `
            <td>${name}</td>
            <td>${description}</td>
            <td class="text-right">
                <div class="dropdown">
                    <button class="btn btn-light btn-sm rounded-circle" type="button" data-bs-toggle="dropdown" aria-expanded="false">
                        &#8942;
                    </button>
                    <ul class="dropdown-menu dropdown-menu-end">
                        <li><a class="dropdown-item btn-edit-course" href="#" data-course-public-id="${coursePublicId}">Editar</a></li>
                        <li><a class="dropdown-item btn-delete-course text-danger" href="#" data-course-public-id="${coursePublicId}">Excluir</a></li>
                    </ul>
                </div>
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

async function fetchAndDisplayCourses(page = 1) {
    try {
        coursesTableBody.innerHTML = `<tr><td colspan="3" class="text-center">Carregando...</td></tr>`;

        // ALTERADO: Chama a API com parâmetros de paginação
        const response = await api.getCourses(page, COURSES_PER_PAGE);

        // Usa o manipulador inteligente para processar a resposta
        handleApiResponse(response, true); // Passamos 'true' para indicar que é uma carga paginada

    } catch (error) {
        coursesTableBody.innerHTML = `<tr><td colspan="3" class="text-center text-danger">${error.message}</td></tr>`;
    }
}

/**
 * ALTERADO: Manipulador de resposta agora controla também a visibilidade da paginação.
 * @param {object|Array} response - A resposta da API.
 * @param {boolean} isPaginated - Indica se a resposta deve ter controles de paginação.
 */
function handleApiResponse(response, isPaginated = false) {
    const paginationContainer = document.getElementById('courses-pagination');
    let coursesList = [];

    // CASO 1: Resposta paginada
    if (isPaginated && response && Array.isArray(response.items)) {
        coursesList = response.items;
        // Atualiza os controles de paginação
        updatePaginationState('courses', response);
    }
    // CASO 2: Resposta de busca (array simples)
    else if (Array.isArray(response)) {
        coursesList = response;
        // Se for um resultado de busca, escondemos a paginação.
        if (paginationContainer) {
            paginationContainer.style.display = 'none';
        }
    }

    renderCoursesTable(coursesList);
}

// Função para carregar os cursos paginados
export function loadCourses() {
    fetchAndDisplayCourses(1);
}

// Função para carregar os resultados da busca
async function searchCourses(searchTerm) {
    try {
        coursesTableBody.innerHTML = `<tr><td colspan="3" class="text-center">Buscando...</td></tr>`;

        // A API de busca pode retornar um array simples
        const response = await api.searchCoursesByName(searchTerm); // Supondo que você tenha um endpoint de busca

        // Usa o manipulador, indicando que NÃO é uma carga paginada
        handleApiResponse(response, false);

    } catch (error) {
        coursesTableBody.innerHTML = `<tr><td colspan="3" class="text-center text-danger">${error.message}</td></tr>`;
    }
}

export function initializeCoursesPanel() {
    if (searchCourseForm && !searchCourseForm.hasAttribute('data-events-attached')) {
        let debounceTimer;

        searchCourseForm.addEventListener('submit', (e) => {
            e.preventDefault();
            const searchTerm = searchCourseInput.value.trim();
            if (searchTerm) searchCourses(searchTerm);
        });

        searchCourseInput.addEventListener('input', () => {
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(() => {
                const searchTerm = searchCourseInput.value.trim();
                if (searchTerm === '') {
                    loadCourses(); // Recarrega a lista paginada
                } else {
                    searchCourses(searchTerm);
                }
            }, 500);
        });

        searchCourseForm.setAttribute('data-events-attached', 'true');
    }

    // ADICIONADO: Inicializa a funcionalidade dos botões de paginação
    initializePagination('courses', (newPage) => {
        fetchAndDisplayCourses(newPage);
    });

    // ADICIONADO: Faz a chamada inicial para carregar a primeira página de cursos
    fetchAndDisplayCourses(1);

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