// Funções Globais para a página de Vídeos
const editModal = document.getElementById('edit-modal');
const editForm = document.getElementById('edit-video-form');
const editVideoIdInput = document.getElementById('edit-video-id');
const editVideoTitleInput = document.getElementById('edit-video-title');
const editVideoDescriptionInput = document.getElementById('edit-video-description');
const editThumbnailInput = document.getElementById('edit-video-thumbnail-file');
const editThumbnailPreview = document.getElementById('edit-thumbnail-preview');


// --- Atualize a Função de Abrir o Modal ---
function openEditModal(video) {
    editVideoIdInput.value = video.id;
    editVideoTitleInput.value = video.title;
    editVideoDescriptionInput.value = video.description;

    // Mostra a imagem atual no preview
    if (video.thumbnailUrl) {
        editThumbnailPreview.src = video.thumbnailUrl;
        editThumbnailPreview.style.display = 'block';
    } else {
        editThumbnailPreview.style.display = 'none';
    }

    editModal.style.display = 'block';
}

function closeEditModal() {
    editModal.style.display = 'none';
}

window.onclick = function (event) {
    if (event.target == editModal) {
        closeEditModal();
    }
};

async function deleteVideo(videoId) {
    if (!confirm('Você tem certeza que deseja deletar este vídeo? Esta ação não pode ser desfeita.')) {
        return;
    }
    try {
        const response = await fetch(`/api/admin/videos/${videoId}`, { method: 'DELETE' });
        const result = await response.json();
        if (!response.ok) throw new Error(result.message || 'Falha ao deletar o vídeo.');
        alert(result.message);
        document.dispatchEvent(new CustomEvent('reloadAllVideos'));
    } catch (error) {
        console.error('Erro ao deletar:', error);
        alert(`Erro: ${error.message}`);
    }
}

document.addEventListener('DOMContentLoaded', function () {
    // Seleção de Elementos DOM da página de Vídeos
    const createForm = document.getElementById('create-video-form');
    const fileInput = document.getElementById('video-file-input');
    const uploadStatusDiv = document.getElementById('upload-status');
    const metadataFieldset = document.getElementById('metadata-fieldset');
    const storageIdentifierInput = document.getElementById('storage-identifier-input');
    const saveButton = document.getElementById('save-video-button');
    const titleInput = document.getElementById('video-title');
    const descriptionInput = document.getElementById('video-description');
    const courseNameInput = document.getElementById('course-name');
    const createThumbnailInput = document.getElementById('video-thumbnail-file');
    const createThumbnailPreview = document.getElementById('create-thumbnail-preview');
    const courseSelect = document.getElementById('video-course-select');
    const newCourseInput = document.getElementById('video-course-new-name');

    const navCrud = document.getElementById('nav-crud');
    const navViewer = document.getElementById('nav-viewer');
    const panelCrud = document.getElementById('panel-crud');
    const panelViewer = document.getElementById('panel-viewer');

    const videosTableBody = document.getElementById('videos-table-body');
    const viewerPlaylist = document.getElementById('viewer-playlist');
    const videoPlayer = document.getElementById('video-player');
    let hlsInstance;

    // Estado para Cache e Rolagem Infinita
    let crudState = { currentPage: 1, isLoading: false, allDataLoaded: false, cache: {} };
    let viewerState = { currentPage: 1, isLoading: false, allDataLoaded: false, cache: {} };

    // Funções de Renderização
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

        // Adicionando os event listeners diretamente aqui (mais seguro que 'onclick')
        row.querySelector('.btn-edit').addEventListener('click', () => {
            openEditModal(video); // Passando o objeto de vídeo inteiro
        });

        row.querySelector('.btn-delete').addEventListener('click', () => {
            deleteVideo(video.id); // Supondo que você tenha uma função deleteVideo(id)
        });

        videosTableBody.appendChild(row);
    }

    function renderViewerPlaylistItem(video) {
        const item = document.createElement('div');
        item.className = 'playlist-item';
        item.innerHTML = `<h4>${video.title}</h4><p>${video.courseName}</p>`;
        item.addEventListener('click', () => playVideo(video.storageIdentifier, item));
        viewerPlaylist.appendChild(item);
    }

    // Lógica de Carregamento Genérica
    async function loadData(state, renderFunction, containerElement) {
        if (state.isLoading || state.allDataLoaded) return;
        state.isLoading = true;
        if (state.cache[state.currentPage]) {
            state.cache[state.currentPage].forEach(renderFunction);
            state.isLoading = false;
            return;
        }
        try {
            const response = await fetch(`/api/admin/videos?page=${state.currentPage}&pageSize=15`);
            if (!response.ok) throw new Error('Erro ao buscar vídeos.');
            const videos = await response.json();
            state.cache[state.currentPage] = videos;
            if (videos.length < 15) state.allDataLoaded = true;
            if (state.currentPage === 1 && videos.length === 0) {
                containerElement.innerHTML = (containerElement.tagName === 'TBODY')
                    ? '<tr><td colspan="5" style="text-align:center;">Nenhum vídeo encontrado.</td></tr>'
                    : '<p style="padding:1rem;">Nenhum vídeo disponível.</p>';
            } else {
                videos.forEach(renderFunction);
            }
            state.currentPage++;
        } catch (error) {
            console.error('Erro ao carregar dados:', error);
        } finally {
            state.isLoading = false;
        }
    }

    // Lógica da Aba "Visualizar Vídeos"
    function playVideo(storageIdentifier, clickedItem) {
        document.querySelectorAll('.playlist-item.playing').forEach(el => el.classList.remove('playing'));
        clickedItem.classList.add('playing');
        const manifestUrl = `/api/videos/${storageIdentifier}/manifest.m3u8`;
        if (hlsInstance) hlsInstance.destroy();
        if (Hls.isSupported()) {
            hlsInstance = new Hls();
            hlsInstance.loadSource(manifestUrl);
            hlsInstance.attachMedia(videoPlayer);
            hlsInstance.on(Hls.Events.MANIFEST_PARSED, () => videoPlayer.play());
        } else if (videoPlayer.canPlayType('application/vnd.apple.mpegurl')) {
            videoPlayer.src = manifestUrl;
            videoPlayer.play();
        }
    }

    // Lógica de Eventos e Inicialização
    function resetAndLoadCrud() {
        crudState = { currentPage: 1, isLoading: false, allDataLoaded: false, cache: {} };
        videosTableBody.innerHTML = '';
        loadData(crudState, renderCrudTableRow, videosTableBody);
    }

    function resetAndLoadViewer() {
        viewerState = { currentPage: 1, isLoading: false, allDataLoaded: false, cache: {} };
        viewerPlaylist.innerHTML = '';
        loadData(viewerState, renderViewerPlaylistItem, viewerPlaylist);
    }

    navCrud.addEventListener('click', () => {
        navCrud.classList.add('active');
        navViewer.classList.remove('active');
        panelCrud.classList.add('active');
        panelViewer.classList.remove('active');
        if (hlsInstance) hlsInstance.destroy();
        if (videosTableBody.innerHTML === '') resetAndLoadCrud();
    });

    navViewer.addEventListener('click', () => {
        navViewer.classList.add('active');
        navCrud.classList.remove('active');
        panelViewer.classList.add('active');
        panelCrud.classList.remove('active');
        if (viewerPlaylist.innerHTML === '') resetAndLoadViewer();
    });

    document.addEventListener('reloadAllVideos', () => {
        resetAndLoadCrud();
        resetAndLoadViewer();
    });

    viewerPlaylist.addEventListener('scroll', () => {
        if (viewerPlaylist.scrollTop + viewerPlaylist.clientHeight >= viewerPlaylist.scrollHeight - 50) {
            loadData(viewerState, renderViewerPlaylistItem, viewerPlaylist);
        }
    });

    window.addEventListener('scroll', () => {
        if (!panelCrud.classList.contains('active')) return;
        if ((window.innerHeight + window.scrollY) >= document.body.offsetHeight - 200) {
            loadData(crudState, renderCrudTableRow, videosTableBody);
        }
    });

    resetAndLoadCrud();

    // CREATE: Lógica para Upload e Criação de Metadados
    fileInput.addEventListener('change', async function (event) {
        const file = event.target.files[0];
        if (!file) return;

        metadataFieldset.disabled = true;
        saveButton.disabled = true;
        uploadStatusDiv.innerHTML = `<p style="color: #0d6efd;">Enviando arquivo, por favor aguarde...</p>`;

        const formData = new FormData();
        formData.append('videoFile', file);

        try {
            const response = await fetch('/api/admin/videos/upload', { method: 'POST', body: formData });
            if (!response.ok) {
                const errorData = await response.json();
                throw new Error(errorData.message || 'Ocorreu um erro no upload.');
            }
            const result = await response.json();
            uploadStatusDiv.innerHTML = `<p style="color: #198754;"><strong>Upload concluído!</strong> Agora, preencha os detalhes do vídeo abaixo.</p>`;
            storageIdentifierInput.value = result.storageIdentifier;
            metadataFieldset.disabled = false;
            saveButton.disabled = false;
        } catch (error) {
            console.error('Erro no upload:', error);
            uploadStatusDiv.innerHTML = `<p style="color: #dc3545;"><strong>Erro no upload:</strong> ${error.message}</p>`;
        }
    });

    createForm.addEventListener('submit', async function (event) {
        event.preventDefault();
        if (!storageIdentifierInput.value) {
            alert('Por favor, envie um arquivo de vídeo primeiro.');
            return;
        }
        saveButton.disabled = true;
        saveButton.textContent = 'Salvando...';

        const formData = new FormData();
        formData.append('Title', titleInput.value);
        formData.append('Description', descriptionInput.value);
        formData.append('StorageIdentifier', storageIdentifierInput.value);
        formData.append('CourseName', courseSelect.value === 'new_course' ? newCourseInput.value : courseSelect.options[courseSelect.selectedIndex].text);

        // Adiciona o arquivo da thumbnail, se um foi selecionado
        if (createThumbnailInput.files.length > 0) {
            formData.append('ThumbnailFile', createThumbnailInput.files[0]);
        }

        try {
            const response = await fetch('/api/admin/videos', {
                method: 'POST',
                body: formData
            });
            if (!response.ok) {
                const errorData = await response.json();
                const errorMessages = Object.values(errorData.errors || {}).flat().join('\n');
                throw new Error(errorMessages || 'Ocorreu um erro ao salvar os dados.');
            }
            alert('Vídeo cadastrado com sucesso!');
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
            console.error('Erro ao salvar metadados:', error);
            alert(`Erro ao salvar: ${error.message}`);
        } finally {
            saveButton.disabled = false;
            saveButton.textContent = 'Salvar Vídeo';
        }
    });

    // UPDATE: Lógica para o Formulário de Edição no Modal
    editForm.addEventListener('submit', async function (event) {
        event.preventDefault();
        const videoId = editVideoIdInput.value;

        // MUDANÇA CRÍTICA: Usando FormData para enviar arquivos
        const formData = new FormData();
        formData.append('Title', editVideoTitleInput.value);
        formData.append('Description', editVideoDescriptionInput.value);

        // Adiciona o arquivo da thumbnail, se um novo foi selecionado
        if (editThumbnailInput.files.length > 0) {
            formData.append('ThumbnailFile', editThumbnailInput.files[0]);
        }

        try {
            const response = await fetch(`/api/admin/videos/${videoId}`, {
                method: 'PUT',
                body: formData
            });
            if (!response.ok) throw new Error('Falha ao atualizar o vídeo.');
            alert('Vídeo atualizado com sucesso!');
            closeEditModal();
            // Dispara o evento para recarregar as listas
            document.dispatchEvent(new CustomEvent('reloadAllVideos'));
        } catch (error) {
            console.error('Erro ao atualizar:', error);
            alert(`Erro: ${error.message}`);
        }
    });

    // --- Lógica de Pré-visualização da Imagem ---
    function setupThumbnailPreview(fileInput, previewImage) {
        fileInput.addEventListener('change', () => {
            const file = fileInput.files[0];
            if (file) {
                // Cria uma URL temporária para o arquivo selecionado
                previewImage.src = URL.createObjectURL(file);
                previewImage.style.display = 'block';
            } else {
                previewImage.style.display = 'none';
            }
        });
    }
    setupThumbnailPreview(createThumbnailInput, createThumbnailPreview);
    setupThumbnailPreview(editThumbnailInput, editThumbnailPreview);

});