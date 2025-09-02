import * as api from '../api/adminAPI.js';


const studentsTableBody = document.getElementById('students-table-body');

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
            // Formatação segura da data
            let
        registrationDate = 'Data inválida';
        try {
            registrationDate = new Date(student.registrationDate).toLocaleDateString('pt-BR');
        } catch (e) {
            console.warn(`Data de registro inválida para o aluno ${name}: ${student.registrationDate}`);
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
    <td data-label="Data de Cadastro">${registrationDate}</td>
    <td data-label="Ações" class="text-right">
        <button class="btn btn-secondary btn-sm btn-view-details" data-student-id="${Id}">Detalhes</button>
    </td>
`;

        studentsTableBody.appendChild(row);

    });
}

export async function loadStudents() {
    try {
        studentsTableBody.innerHTML = `<tr><td colspan="4" class="text-center">Carregando...</td></tr>`;
        const students = await api.getStudents();
        renderStudentsTable(students);
    } catch (error) {
        studentsTableBody.innerHTML = `<tr><td colspan="4" class="text-center text-danger">${error.message}</td></tr>`;
    }
}

export function initializeStudentsPanel() {
}