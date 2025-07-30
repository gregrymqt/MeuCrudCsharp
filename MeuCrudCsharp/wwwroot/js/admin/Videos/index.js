// =====================================================================
// Funções Globais (acessíveis via onclick)
// =====================================================================
const editModal = document.getElementById('edit-modal');
const editForm = document.getElementById('edit-video-form');
const editVideoIdInput = document.getElementById('edit-video-id');
const editVideoTitleInput = document.getElementById('edit-video-title');
const editVideoDescriptionInput = document.getElementById('edit-video-description');

function openEditModal(videoId, title, description) {
    editVideoIdInput.value = videoId;
    editVideoTitleInput.value = title;
    editVideoDescriptionInput.value = description;
    editModal.style.display = 'block';
}

function closeEditModal() {
    editModal.style.display = 'none';
}

window.onclick = function (event) {
    if (event.target == editModal) {
        closeEditModal();
    }
}

// --- NOVA FUNÇÃO PARA DELETAR ---
async function deleteVideo(videoId) {
    if (!confirm('Você tem certeza que deseja deletar este vídeo? Esta ação não pode ser desfeita.')) {
        return;
    }
    try {
        const response = await fetch(`/api/admin/videos/${videoId}`, { method: 'DELETE' });
        const result = await response.json();
        if (!response.ok) throw new Error(result.message || 'Falha ao deletar o vídeo.');
        alert(result.message);
        // Dispara o evento para recarregar a lista do zero
        document.getElementById('tab-manage').dispatchEvent(new CustomEvent('reload'));
    } catch (error) {
        console.error('Erro ao deletar:', error);
        alert(`Erro: ${error.message}`);
    }
}


document.addEventListener('DOMContentLoaded', function () {
    // =====================================================================
    // Seleção de Elementos DOM
    // =====================================================================
    const createForm = document.getElementById('create-video-form');
    const fileInput = document.getElementById('video-file-input');
    const uploadStatusDiv = document.getElementById('upload-status');
    const metadataFieldset = document.getElementById('metadata-fieldset');
    const storageIdentifierInput = document.getElementById('storage-identifier-input');
    const saveButton = document.getElementById('save-video-button');
    const titleInput = document.getElementById('video-title');
    const descriptionInput = document.getElementById('video-description');
    const courseNameInput = document.getElementById('course-name');

    const tabCreate = document.getElementById('tab-create');
    const tabManage = document.getElementById('tab-manage');
    const contentCreate = document.getElementById('content-create');
    const contentManage = document.getElementById('content-manage');
    const videosTableBody = document.getElementById('videos-table-body');



    // =====================================================================
    // NOVO: Estado para Cache e Rolagem Infinita
    // =====================================================================
    let currentPage = 1;
    let isLoading = false;
    let allDataLoaded = false;
    const videoCache = {}; // Cache simples em memória (objeto JS)


    // =====================================================================
    // READ: Lógica de Carregamento Refatorada
    // =====================================================================

    // Função para renderizar os vídeos na tabela
    function renderVideos(videos) {
        if (currentPage === 1 && videos.length === 0) {
            videosTableBody.innerHTML = '<tr><td colspan="5" style="text-align:center;">Nenhum vídeo encontrado.</td></tr>';
            return;
        }

        videos.forEach(video => {
            const safeTitle = video.title.replace(/'/g, "\\'");
            const safeDescription = (video.description || '').replace(/'/g, "\\'");
            const row = `
                    <tr>
                        <td>${video.title}</td>
                        <td>${video.courseName}</td>
                        <td><span class="status-badge status-${video.status.toLowerCase()}">${video.status}</span></td>
                        <td>${new Date(video.uploadDate).toLocaleDateString()}</td>
                        <td class="actions">
                            <button class="btn btn-secondary btn-sm" onclick="openEditModal('${video.id}', '${safeTitle}', '${safeDescription}')">Editar</button>
                            <button class="btn btn-danger btn-sm" onclick="deleteVideo('${video.id}')">Deletar</button>
                        </td>
                    </tr>
                `;
            videosTableBody.insertAdjacentHTML('beforeend', row);
        });
    }

    // Função principal para buscar os vídeos
    async function loadVideos() {
        if (isLoading || allDataLoaded) return; // Previne múltiplas chamadas

        isLoading = true;
        // Opcional: Adicionar um ícone de "loading" no final da tabela

        // 1. LÓGICA DE CACHE
        if (videoCache[currentPage]) {
            console.log(`Carregando página ${currentPage} do cache.`);
            renderVideos(videoCache[currentPage]);
            isLoading = false;
            return;
        }

        try {
            console.log(`Buscando página ${currentPage} da API.`);
            const response = await fetch(`/api/admin/videos?page=${currentPage}&pageSize=10`);
            if (!response.ok) throw new Error('Erro ao buscar vídeos.');

            const videos = await response.json();

            // Armazena no cache
            videoCache[currentPage] = videos;

            if (videos.length < 10) {
                allDataLoaded = true; // Se a API retorna menos que o tamanho da página, não há mais dados.
            }

            renderVideos(videos);
            currentPage++;

        } catch (error) {
            console.error('Erro ao carregar vídeos:', error);
            // Opcional: Mostrar erro no final da tabela
        } finally {
            isLoading = false;
            // Opcional: Remover o ícone de "loading"
        }
    }

    // Função para resetar e carregar a lista
    function resetAndLoadVideos() {
        currentPage = 1;
        isLoading = false;
        allDataLoaded = false;
        videosTableBody.innerHTML = ''; // Limpa a tabela
        // Não limpa o cache, para o caso do usuário voltar para a aba
        loadVideos();
    }

    // =====================================================================
    // Lógica de Eventos
    // =====================================================================

    // Lógica de troca de abas
    tabCreate.addEventListener('click', () => {
        tabCreate.classList.add('active');
        tabManage.classList.remove('active');
        contentCreate.classList.add('active');
        contentManage.classList.remove('active');
    });

    tabManage.addEventListener('click', () => {
        tabManage.classList.add('active');
        tabCreate.classList.remove('active');
        contentManage.classList.add('active');
        contentCreate.classList.remove('active');
        // Só carrega se a tabela estiver vazia
        if (videosTableBody.innerHTML === '') {
            loadVideos();
        }
    });

    // Evento customizado para forçar o recarregamento (usado após delete/update)
    tabManage.addEventListener('reload', resetAndLoadVideos);

    // 2. LÓGICA DE ROLAGEM INFINITA
    window.addEventListener('scroll', () => {
        // Só executa a lógica se a aba de gerenciamento estiver ativa
        if (!contentManage.classList.contains('active')) return;

        // Verifica se o usuário chegou perto do final da página
        if ((window.innerHeight + window.scrollY) >= document.body.offsetHeight - 200) {
            loadVideos();
        }
    });

    // =====================================================================
    // CREATE: Lógica para Upload e Criação de Metadados
    // =====================================================================
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

        const videoData = {
            title: titleInput.value,
            description: descriptionInput.value,
            courseName: courseNameInput.value,
            storageIdentifier: storageIdentifierInput.value
        };

        try {
            const response = await fetch('/api/admin/videos', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(videoData)
            });
            if (!response.ok) {
                const errorData = await response.json();
                const errorMessages = Object.values(errorData.errors || {}).flat().join('\n');
                throw new Error(errorMessages || 'Ocorreu um erro ao salvar os dados.');
            }
            alert('Vídeo cadastrado com sucesso!');
            createForm.reset();
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

    // =====================================================================
    // UPDATE: Lógica para o Formulário de Edição no Modal
    // =====================================================================
    editForm.addEventListener('submit', async function (event) {
        event.preventDefault();
        const videoId = editVideoIdInput.value;
        const updatedData = {
            title: editVideoTitleInput.value,
            description: editVideoDescriptionInput.value
        };
        try {
            const response = await fetch(`/api/admin/videos/${videoId}`, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(updatedData)
            });
            if (!response.ok) throw new Error('Falha ao atualizar o vídeo.');
            alert('Vídeo atualizado com sucesso!');
            closeEditModal();
            // Dispara o evento para recarregar a lista
            tabManage.dispatchEvent(new CustomEvent('reload'));
        } catch (error) {
            console.error('Erro ao atualizar:', error);
            alert(`Erro: ${error.message}`);
        }
    });


    const form = document.getElementById('create-plan-form');
    const saveplanButton = document.getElementById('save-plan-button');
    const formStatus = document.getElementById('form-status');
    const plansTableBody = document.getElementById('plans-table-body');

    // --- Lógica para Criar um Novo Plano ---
    form.addEventListener('submit', async function (event) {
        event.preventDefault();
        saveplanButton.disabled = true;
        saveplanButton.textContent = 'Criando...';
        formStatus.innerHTML = '';

        // Coleta os dados do formulário
        const planData = {
            reason: document.getElementById('plan-reason').value,
            auto_recurring: {
                frequency: parseInt(document.getElementById('plan-frequency').value),
                frequency_type: document.getElementById('plan-frequency-type').value,
                transaction_amount: parseFloat(document.getElementById('plan-amount').value),
                currency_id: document.getElementById('plan-currency').value
            },
            back_url: document.getElementById('plan-back-url').value
        };

        try {
            // O backend precisa ter um endpoint para receber esta chamada
            const response = await fetch('/api/admin/plans', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(planData)
            });

            const result = await response.json();

            if (!response.ok) {
                throw new Error(result.message || 'Ocorreu um erro ao criar o plano.');
            }

            formStatus.innerHTML = `<p style="color: var(--success-color);"><strong>Plano criado com sucesso!</strong> ID: ${result.id}</p>`;
            form.reset();
            loadPlans(); // Recarrega a lista de planos

        } catch (error) {
            console.error('Erro ao criar plano:', error);
            formStatus.innerHTML = `<p style="color: var(--danger-color);"><strong>Erro:</strong> ${error.message}</p>`;
        } finally {
            saveplanButton.disabled = false;
            saveplanButton.textContent = 'Criar Plano';
        }
    });

    // --- Lógica para Listar Planos Existentes ---
    async function loadPlans() {
        try {
            // O backend precisa ter um endpoint para listar os planos
            const response = await fetch('/api/admin/plans');
            if (!response.ok) throw new Error('Falha ao buscar planos.');

            const plans = await response.json();
            plansTableBody.innerHTML = '';

            if (plans.length === 0) {
                plansTableBody.innerHTML = '<tr><td colspan="3" style="text-align: center;">Nenhum plano criado ainda.</td></tr>';
                return;
            }

            plans.forEach(plan => {
                const row = `
                    <tr>
                        <td>${plan.id}</td>
                        <td>${plan.reason}</td>
                        <td>${plan.auto_recurring.transaction_amount.toFixed(2)} ${plan.auto_recurring.currency_id}</td>
                    </tr>
                `;
                plansTableBody.innerHTML += row;
            });

        } catch (error) {
            console.error('Erro ao carregar planos:', error);
            plansTableBody.innerHTML = `<tr><td colspan="3" style="text-align: center; color: var(--danger-color);">${error.message}</td></tr>`;
        }
    }

    // Carrega os planos ao iniciar a página
    loadPlans();

});