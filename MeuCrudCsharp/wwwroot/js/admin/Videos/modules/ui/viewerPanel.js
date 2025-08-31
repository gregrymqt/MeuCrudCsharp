import * as api from '../api/videosAPI.js';

// --- Estado do Painel ---
let viewerState = {
    currentPage: 1,
    isLoading: false,
    allDataLoaded: false,
};

// --- Seletores de DOM ---
const viewerPlaylist = document.getElementById('viewer-playlist');

// --- Funções Internas do Módulo ---

/**
 * Renderiza um item na playlist como um link que abre em uma nova aba.
 * @param {object} video - O objeto de vídeo vindo da API, que deve conter 'publicId'.
 */
function renderViewerPlaylistItem(video) {
    // Cria um elemento <a> para o redirecionamento
    const itemLink = document.createElement('a');
    itemLink.className = 'playlist-item-link';

    // Constrói a URL para a página do player, passando o PublicId
    itemLink.href = `/Videos/Player?id=${video.publicId}`;

    // (UX Melhorada) target="_blank" abre o vídeo em uma nova aba,
    // para que o admin não perca sua posição na lista.
    itemLink.target = '_blank';

    itemLink.innerHTML = `<h4>${video.title}</h4><p>${video.courseName || 'Curso não definido'}</p>`;

    viewerPlaylist.appendChild(itemLink);
}

/**
 * Carrega a próxima página de vídeos e os renderiza na playlist.
 */
async function loadData() {
    if (viewerState.isLoading || viewerState.allDataLoaded) return;

    viewerState.isLoading = true;

    try {
        const paginatedResult = await api.getPaginatedVideos(viewerState.currentPage);
        const videos = paginatedResult.items;

        if (!videos || videos.length === 0) {
            viewerState.allDataLoaded = true;
            if (viewerState.currentPage === 1) {
                viewerPlaylist.innerHTML = '<p style="padding:1rem; text-align:center;">Nenhum vídeo encontrado.</p>';
            }
            return;
        }

        videos.forEach(renderViewerPlaylistItem);
        viewerState.currentPage++;

    } catch (error) {
        console.error('Erro ao carregar vídeos para o viewer:', error);
        viewerPlaylist.innerHTML += '<p style="padding:1rem; color:red; text-align:center;">Erro ao carregar mais vídeos.</p>';
    } finally {
        viewerState.isLoading = false;
    }
}

/**
 * Reseta o estado e o conteúdo da playlist e carrega os dados novamente.
 */
export function resetAndLoadViewer() {
    viewerState = { currentPage: 1, isLoading: false, allDataLoaded: false };
    viewerPlaylist.innerHTML = '';
    loadData();
}

/**
 * Inicializa o painel do viewer, configurando o scroll infinito.
 */
export function initializeViewerPanel() {
    // Configura o scroll infinito para a playlist
    viewerPlaylist.addEventListener('scroll', () => {
        if (viewerPlaylist.scrollTop + viewerPlaylist.clientHeight >= viewerPlaylist.scrollHeight - 100) {
            loadData();
        }
    });
}
