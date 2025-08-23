import * as api from '../api/adminAPI.js';

const studentsTableBody = document.getElementById('students-table-body');

function renderStudentsTable(students) {
    studentsTableBody.innerHTML = '';

    if (!students || students.length === 0) {
        studentsTableBody.innerHTML = '<tr><td colspan="4" class="text-center">Nenhum aluno encontrado.</td></tr>';
        return;
    }

    students.forEach(student => {
        let statusColor = 'grey';
        const status = student.subscriptionStatus?.toLowerCase() || '';
        if (status === 'approved' || status === 'ativa') statusColor = 'green';
        else if (status === 'cancelled' || status === 'cancelada') statusColor = 'red';
        else if (status === 'paused' || status === 'pausada') statusColor = 'orange';

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