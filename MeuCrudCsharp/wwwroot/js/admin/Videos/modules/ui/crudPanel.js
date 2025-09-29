import * as api from '../api/videosAPI.js';
import {createVideoHubConnection} from '../services/signalRService.js';
import {setupModal, setupThumbnailPreview} from './commonUI.js';


// --- Estado do Painel ---
let crudState = {currentPage: 1, isLoading: false, allDataLoaded: false};

// --- Seletores de DOM ---
const createForm = document.getElementById('create-video-form');
const fileInput = document.getElementById('video-file-input');
const resetButton = document.querySelector('.file-input-reset');
const uploadStatusDiv = document.getElementById('upload-status');
const metadataFieldset = document.getElementById('metadata-fieldset');
const storageIdentifierInput = document.getElementById('storageIdentifier');
const saveButton = document.getElementById('save-video-button');
const courseSelect = document.getElementById('video-course-select');
const newCourseInput = document.getElementById('video-course-new-name');
const createThumbnailInput = document.getElementById('video-thumbnail-file');
const createThumbnailPreview = document.getElementById('create-thumbnail-preview');
const videosTableBody = document.getElementById('videos-table-body');

const editModal = document.getElementById('edit-modal');
const editForm = document.getElementById('edit-video-form');
const editVideoIdInput = document.getElementById('edit-video-id');
const editThumbnailInput = document.getElementById('edit-video-thumbnail-file');
const editThumbnailPreview = document.getElementById('edit-thumbnail-preview');

// --- Funções Internas do Módulo ---

function renderCrudTableRow(video) {
    const row = document.createElement('tr');
    row.innerHTML = `
        <td>${video.title}</td>
        <td>${video.courseName}</td>
        <td><span class="status-badge status-${video.status.toLowerCase()}">${video.status}</span></td>
        <td>${new Date(video.uploadDate).toLocaleDateString()}</td>
        <td class="actions">
            <button class="btn btn-secondary btn-sm btn-edit">Editar</button>
            <button class="btn btn-danger btn-sm btn-delete">Deletar</button>
        </td>
    `;
    row.querySelector('.btn-edit').addEventListener('click', () => openEditVideoModal(video));
    row.querySelector('.btn-delete').addEventListener('click', () => handleVideoDelete(video.id));
    videosTableBody.appendChild(row);
}

async function loadData() {
    if (crudState.isLoading || crudState.allDataLoaded) return;
    crudState.isLoading = true;

    try {

        const paginatedResult = await api.getPaginatedVideos(crudState.currentPage);
        const videos = paginatedResult.items;

        if (!videos || videos.length === 0) {
            crudState.allDataLoaded = true;
            if (crudState.currentPage === 1) {
                videosTableBody.innerHTML = '<tr><td colspan="5" class="text-center">Nenhum vídeo encontrado.</td></tr>';
            }
            return;
        }
        videos.forEach(renderCrudTableRow);
        crudState.currentPage++;
    } catch (error) {
        console.error('Erro ao carregar vídeos:', error);
    } finally {
        crudState.isLoading = false;
    }
}

function openEditVideoModal(video) {
    editVideoIdInput.value = video.id;
    editForm.querySelector('#edit-video-title').value = video.title;
    editForm.querySelector('#edit-video-description').value = video.description;
    editThumbnailPreview.src = video.thumbnailUrl || '';
    editThumbnailPreview.style.display = video.thumbnailUrl ? 'block' : 'none';
    editModal.style.display = 'block';
}

function closeEditVideoModal() {
    editModal.style.display = 'none';
}

async function handleVideoDelete(videoId) {

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
    if (result.isConfirmed) {
        try {
            const response = await api.deleteVideo(videoId);
            Swal.fire('Excluído!', response.message, 'success');
            document.dispatchEvent(new CustomEvent('reloadAllVideos'));
        } catch (error) {
            Swal.fire('Erro!', error.message, 'error');
        }
    }
}

async function loadCoursesIntoSelect() {
    try {
        const coursesResponse = await api.getCourses();
        const courses = coursesResponse.items;
        while (courseSelect.options.length > 2) courseSelect.remove(2);
        courses.forEach(course => courseSelect.add(new Option(course.name, course.name)));
    } catch (error) {
        console.error('Erro ao carregar cursos:', error);
    }
}

function monitorProcessingProgress(storageId) {
    const hubCallbacks = {
        onProgress: (data) => {
            uploadStatusDiv.innerHTML = `<p><strong>Status:</strong> ${data.message} (${data.progress}%)</p>`;
            Swal.update({text: `${data.message} (${data.progress}%)`});

            if (data.isComplete || data.isError) {
                videoHub.stop();
                if (data.isComplete) {
                    Swal.fire('Sucesso!', 'Vídeo processado e pronto.', 'success');
                    metadataFieldset.disabled = false;
                    saveButton.disabled = false;
                } else {
                    Swal.fire('Erro!', `Processamento falhou: ${data.message}`, 'error');
                }
            }
        }
    };
    const videoHub = createVideoHubConnection(hubCallbacks);
    videoHub.start()
        .then(() => videoHub.subscribe(storageId))
        .catch(err => console.error("Erro na conexão SignalR: ", err));
}

export function initializeCrudPanel() {

    fileInput.addEventListener('change', async (event) => {
        const file = event.target.files[0];
        if (!file) return;

        if (fileInput.files.length > 0) {
            resetButton.style.display = 'block';
        } else {
            // Se o usuário abrir a caixa de diálogo e cancelar, a contagem será 0
            resetButton.style.display = 'none';
        }

        const originalFileNameInput = document.getElementById('originalFileName');
        originalFileNameInput.value = file.name;

        metadataFieldset.disabled = true;
        saveButton.disabled = true;
        Swal.fire({
            title: 'Enviando...',
            text: 'Aguarde o upload do arquivo.',
            allowOutsideClick: false,
            didOpen: () => Swal.showLoading()
        });

        const formData = new FormData();
        formData.append('videoFile', file);

        try {
            // CORREÇÃO: Usando a função da API
            const result = await api.uploadVideoFile(formData);
            storageIdentifierInput.value = result.storageIdentifier;
            uploadStatusDiv.innerHTML = `<p><strong>Aguardando início do processamento...</strong></p>`;
            Swal.update({title: 'Processando Vídeo...', text: 'Isso pode levar alguns minutos.'});

            // CORREÇÃO: Usando o serviço do SignalR
            monitorProcessingProgress(result.storageIdentifier);

            metadataFieldset.disabled = false;
            saveButton.disabled = false;
        } catch (error) {
            Swal.fire('Erro!', error.message, 'error');
            uploadStatusDiv.innerHTML = `<p style="color:red;"><strong>Erro:</strong> ${error.message}</p>`;

            metadataFieldset.disabled = true;
            saveButton.disabled = true;
        }
    });

    resetButton.addEventListener('click', function() {
        // Limpa o valor do input de arquivo. Isso remove a seleção.
        fileInput.value = '';

        // Esconde o botão 'X' novamente
        resetButton.style.display = 'none';

        // Opcional: Limpa também qualquer mensagem de status
        if (uploadStatusDiv) {
            uploadStatusDiv.innerHTML = '';
        }
    });

    createForm.addEventListener('submit', async function (event) {
        event.preventDefault();
        if (!storageIdentifierInput.value) {
            Swal.fire('Attention', 'Please upload a video file first.', 'warning');
            return;
        }
        saveButton.disabled = true;
        saveButton.textContent = 'Saving...';

        const formData = new FormData(createForm);

        try {
            const response = await api.saveVideoMetadata(formData);

            await Swal.fire({
                title: 'Success!',
                text: `Video registered successfully! ${response.videoId}`,
                icon: 'success'
            });

            document.dispatchEvent(new CustomEvent('reloadAllVideos'));
            if (courseSelect.value === 'new_course') {
                await loadCoursesIntoSelect();
            }
            createForm.reset();
            newCourseInput.style.display = 'none';
            metadataFieldset.disabled = true;
            saveButton.disabled = true;
            uploadStatusDiv.innerHTML = '';
        } catch (error) {
            console.error('Error saving metadata:', error);
            Swal.fire('Save Error', error.message, 'error');
        } finally {
            saveButton.disabled = false;
            saveButton.textContent = 'Save Video';
        }
    });

    editForm.addEventListener('submit', async function (event) {
        event.preventDefault();
        const videoId = editVideoIdInput.value;
        const formData = new FormData(editForm);

        try {
            const response = await api.updateVideoMetadata(videoId, formData);

            Swal.fire({
                title: 'Updated!',
                text: `Video updated successfully! ${response.videoId}`,
                icon: 'success',
                timer: 2000,
                showConfirmButton: false
            });

            closeEditModal();
            document.dispatchEvent(new CustomEvent('reloadAllVideos'));
        } catch (error) {
            console.error('Error updating video:', error);
            Swal.fire('Error!', error.message, 'error');
        }
    });
    courseSelect.addEventListener('change', function () {
        const isNew = this.value === 'new_course';
        newCourseInput.style.display = isNew ? 'block' : 'none';
        newCourseInput.required = isNew;
    });

    setupThumbnailPreview(createThumbnailInput, createThumbnailPreview);
    setupThumbnailPreview(editThumbnailInput, editThumbnailPreview);
    setupModal(editModal, null, closeEditVideoModal);

    // --- Carregamento Inicial ---
    loadCoursesIntoSelect();
    loadData();
}

export function resetAndLoadCrud() {
    crudState = {currentPage: 1, isLoading: false, allDataLoaded: false};
    videosTableBody.innerHTML = '';
    loadData();
}