import * as api from '../api/adminAPI.js';
import {openModal, closeModal} from '../ui/modals.js';
import cacheService from '../../../../core/cacheService.js';
import {initializePagination, updatePaginationState} from "../ui/pagination.js";


const studentsTableBody = document.getElementById('students-table-body');
const studentDetailsModal = document.getElementById('studentDetailsModal'); // O modal que você adicionou ao HTML
let isStudentDocumentListenerAttached = false;
const STUDENTS_PER_PAGE = 10;

/**
 * Mapeia o status da API para uma classe CSS correspondente.
 * @param {string} status - O status da assinatura (ex: 'approved', 'cancelled').
 * @returns {string} A classe CSS para o status.
 */
function getStatusClass(status = '') {
    const safeStatus = status.toLowerCase();
    switch (safeStatus) {
        case 'approved':
        case 'active': // Adicionando sinônimos
        case 'ativa':
            return 'status-active';
        case 'cancelled':
        case 'cancelada':
            return 'status-cancelled';
        case 'paused':
        case 'pausada':
            return 'status-paused';
        default:
            return 'status-inactive'; // Uma classe padrão para status desconhecidos
    }
}

function renderStudentsTable(students) {
    studentsTableBody.innerHTML = ''; // Limpa a tabela

    if (!students || students.length === 0) {
        studentsTableBody.innerHTML = '<tr><td colspan="4" class="text-center empty-state">Nenhum aluno encontrado.</td></tr>';
        return;
    }

    students.forEach(student => {
        // Verificação segura dos dados
        const name = student.name ?? 'Aluno sem nome';
        const email = student.email ?? 'Email não informado';
        const subscriptionStatus = student.subscriptionStatus ?? 'Desconhecido';
        const Id = student.id;
        const subscriptionId = student.subscriptionId ?? 'Sem assinatura'
        let registrationDate = 'Data não registrada'; // Mensagem padrão melhor
        if (student.registrationDate) { // Verifica se não é null ou undefined
            try {
                registrationDate = new Date(student.registrationDate).toLocaleDateString('pt-BR');
            } catch (e) {
                console.warn(`Data de registro inválida: ${student.registrationDate}`);
                registrationDate = 'Data inválida';
            }
        }

        // **MELHORIA:** Usando classes CSS em vez de estilos inline para o status
        const statusClass = getStatusClass(subscriptionStatus);

        const row = document.createElement('tr');

        row.innerHTML = `
    <td data-label="Nome">${name}</td>
    <td data-label="Email">${email}</td>
    <td data-label="Status da Assinatura">
        <span class="status-badge ${statusClass}">${subscriptionStatus}</span>
    </td>
    <td data-label="Id da Assinatura">${subscriptionId}</td>
    <td data-label="Data de Cadastro">${registrationDate}</td>
    <td data-label="Ações" class="text-right">
        <button class="btn btn-light btn-sm rounded-circle btn-view-details" data-student-id="${Id}" title="Ver Detalhes">
            <i class="bi bi-eye"></i>
        </button>
    </td>
`;
        studentsTableBody.appendChild(row);
    });
}

/**
 * ADICIONADO: Nova função para buscar e exibir os alunos de uma página específica.
 * Esta função agora centraliza a lógica de carregamento, renderização e atualização da paginação.
 * @param {number} page - O número da página a ser buscada.
 */
async function fetchAndDisplayStudents(page = 1) {
    try {
        // Mostra o estado de carregamento na tabela
        studentsTableBody.innerHTML = `<tr><td colspan="4" class="text-center">Carregando...</td></tr>`;

        // ALTERADO: A chamada de API agora envia a página e o limite de itens
        const result = await api.getStudents(page, STUDENTS_PER_PAGE);

        // Renderiza a tabela apenas com a lista de items da resposta da API
        renderStudentsTable(result.items);

        // Atualiza os controles de paginação (números e botões) com os metadados da resposta
        updatePaginationState('students', result);

    } catch (error) {
        studentsTableBody.innerHTML = `<tr><td colspan="4" class="text-center text-danger">${error.message}</td></tr>`;
    }
}

async function loadStudentsPublicID(id) {
    const cacheKey = `student_details_${id}`; // Chave de cache única por aluno

    // 1. Tenta buscar do cache primeiro
    const cachedStudent = cacheService.get(cacheKey);
    if (cachedStudent) {
        return cachedStudent;
    }

    // 2. Se não estiver no cache (cache miss), busca na API
    try {
        console.log(`Cache MISS. Buscando na API para o aluno ID: ${id}`);
        const student = await api.getStudentsPublicId(id);

        // 3. Salva o resultado da API no cache para futuras requisições
        if (student) {
            cacheService.set(cacheKey, student); // Usa a duração padrão de 5 minutos
        }

        return student;
    } catch (error) {
        alert(`Erro ao buscar aluno: ${error.message}`);
        console.error(error);
        return null;
    }
}

async function openStudentModal(studentId) {
    const student = await loadStudentsPublicID(studentId);

    if (!student) return;

    try {
        document.getElementById('modalStudentId').textContent = student.id ?? 'Não informado';
        document.getElementById('modalStudentName').textContent = student.name ?? 'Não informado';
        document.getElementById('modalStudentEmail').textContent = student.email ?? 'Não informado';
        document.getElementById('modalStudentStatus').textContent = student.subscriptionStatus ?? 'Desconhecido';
        document.getElementById('modalStudentPlan').textContent = student.planName ?? 'N/A';
        document.getElementById('modalSubscriptionId').textContent = student.subscriptionId ?? 'Não Informado';

        let registrationDate = 'Data não informada';
        if (student.registrationDate) {
            try {
                registrationDate = new Date(student.registrationDate).toLocaleDateString('pt-BR', {
                    day: '2-digit', month: '2-digit', year: 'numeric',
                    hour: '2-digit', minute: '2-digit'
                });
            } catch (e) {
                console.warn("Formato de data inválido recebido:", student.registrationDate);
                registrationDate = 'Data inválida';
            }
        }
        document.getElementById('modalStudentRegistrationDate').textContent = registrationDate;

        // **MUDANÇA PRINCIPAL**: Usando a função importada do seu módulo de modais
        openModal(studentDetailsModal);

    } catch (error) {
        console.error("Falha ao preencher os detalhes do aluno no modal:", error);
        alert("Ocorreu um erro ao exibir os detalhes do aluno.");
    }
}


export async function initializeStudentsPanel() {
    // 1. Mantém a lógica dos event listeners para os modais de detalhes
    if (!isStudentDocumentListenerAttached) {
        document.addEventListener('click', (event) => {
            const detailsButton = event.target.closest('.btn-view-details');
            if (detailsButton) {
                const studentId = detailsButton.dataset.studentId;
                if (studentId) {
                    openStudentModal(studentId);
                }
            }
        });
        isStudentDocumentListenerAttached = true;
        console.log('Listener de clique no DOCUMENTO para detalhes de estudantes foi anexado.');
    }

    const studentDetailsModal = document.getElementById('studentDetailsModal');
    const closeModalButton = studentDetailsModal.querySelector('[data-bs-dismiss="modal"]');
    if (closeModalButton && !closeModalButton.hasAttribute('data-click-listener-attached')) {
        closeModalButton.addEventListener('click', () => {
            closeModal(studentDetailsModal);
        });
        closeModalButton.setAttribute('data-click-listener-attached', 'true');
    }

    // 2. ADICIONADO: Inicializa a funcionalidade dos botões de paginação
    // O segundo argumento é a função que será chamada quando o usuário clicar em "próximo" ou "anterior".
    initializePagination('students', (newPage) => {
        fetchAndDisplayStudents(newPage);
    });

    // 3. ADICIONADO: Faz a chamada inicial para carregar a primeira página de alunos
    fetchAndDisplayStudents(1);
}