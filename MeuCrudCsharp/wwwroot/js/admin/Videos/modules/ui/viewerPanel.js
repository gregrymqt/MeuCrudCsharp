// /js/admin/videos/modules/ui/viewerPanel.js
import * as api from '../api/videosAPI.js';
import * as player from '../services/videoPlayerService.js';

// --- Estado do Painel ---
let viewerState = {
    currentPage: 1,
    isLoading: false,
    allDataLoaded: false,
};

// --- Seletores de DOM ---
const viewerPlaylist = document.getElementById('viewer-playlist');
const videoPlayerElement = document.getElementById('video-player');

// --- Funções Internas do Módulo ---

/**
 * Renderiza um item na playlist e adiciona o evento de clique para tocar o vídeo.
 * @param {object} video - O objeto de vídeo vindo da API.
 */
function renderViewerPlaylistItem(video) {
    const item = document.createElement('div');
    item.className = 'playlist-item';
    item.innerHTML = `<h4>${video.title}</h4><p>${video.courseName}</p>`;

    item.addEventListener('click', () => {
        // Remove a classe 'playing' de qualquer outro item
        document.querySelectorAll('.playlist-item.playing').forEach(el => el.classList.remove('playing'));
        // Adiciona a classe ao item clicado
        item.classList.add('playing');
        // Usa o serviço para tocar o stream de vídeo
        player.playStream(video.storageIdentifier, videoPlayerElement);
    });

    viewerPlaylist.appendChild(item);
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


// --- Função Principal Exportada ---

/**
 * Inicializa o painel do viewer, configurando o player e o scroll infinito.
 */
export function initializeViewerPanel() {
    // Inicializa o serviço do player de vídeo
    player.initializePlayer(videoPlayerElement);

    // Configura o scroll infinito para a playlist
    viewerPlaylist.addEventListener('scroll', () => {
        // Carrega mais itens quando o scroll chega perto do final
        if (viewerPlaylist.scrollTop + viewerPlaylist.clientHeight >= viewerPlaylist.scrollHeight - 100) {
            loadData();
        }
    });

}