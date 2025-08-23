// /js/admin/videos/videos-main.js
import { initializeCrudPanel, resetAndLoadCrud } from './modules/ui/crudPanel.js';
import { initializeViewerPanel, resetAndLoadViewer } from './modules/ui/viewerPanel.js';
import { destroyPlayer } from './modules/services/videoPlayerService.js';

document.addEventListener('DOMContentLoaded', () => {
    // Seletores dos painéis e navegação
    const navCrud = document.getElementById('nav-crud');
    const navViewer = document.getElementById('nav-viewer');
    const panelCrud = document.getElementById('panel-crud');
    const panelViewer = document.getElementById('panel-viewer');

    initializeCrudPanel();
    initializeViewerPanel();

    // Carrega os dados do painel ativo por padrão
    resetAndLoadCrud();

    // Ouve pelo evento personalizado para recarregar tudo
    document.addEventListener('reloadAllVideos', () => {
        resetAndLoadCrud();
        resetAndLoadViewer();
    });
});