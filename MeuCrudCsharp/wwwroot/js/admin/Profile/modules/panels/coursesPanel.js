// /js/admin/modules/panels/coursesPanel.js

import * as api from '../api/adminAPI.js';
import { openModal, closeModal } from '../ui/modals.js';

// --- Seletores de DOM ---
const coursesTableBody = document.getElementById('courses-table-body');
const createCourseForm = document.getElementById('create-course-form');
const editCourseModal = document.getElementById('edit-course-modal');
const editCourseForm = document.getElementById('edit-course-form');
const editCourseId = document.getElementById('edit-course-id');
const editCourseName = document.getElementById('edit-course-name');
const editCourseDescription = document.getElementById('edit-course-description');

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

async function handleCourseDelete(courseId) {
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
    editCourseId.value = course.id;
    editCourseName.value = course.name;
    editCourseDescription.value = course.description || '';
    openModal(editCourseModal);
}

export async function loadCourses() {
    try {
        coursesTableBody.innerHTML = `<tr><td colspan="3" class="text-center">Carregando...</td></tr>`;
        const courses = await api.getCourses();
        renderCoursesTable(courses);
    } catch (error) {
        coursesTableBody.innerHTML = `<tr><td colspan="3" class="text-center text-danger">${error.message}</td></tr>`;
    }
}

export function initializeCoursesPanel() {

    coursesTableBody.addEventListener('click', async (e) => {
        const target = e.target;
        const courseId = target.dataset.courseId;
        if (target.classList.contains('btn-edit-course')) {
            const courses = await api.getCourses();
            const course = courses.find(c => c.id == courseId);
            if (course) openEditCourseModal(course);
        }
        if (target.classList.contains('btn-delete-course')) {
            handleCourseDelete(courseId);
        }
    });

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

    document.getElementById('close-edit-course-modal')?.addEventListener('click', () => closeModal(editCourseModal));

}