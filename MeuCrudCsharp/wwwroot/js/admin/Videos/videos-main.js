// /js/admin/videos/videos-main.js
import { initializeNavigation } from './modules/ui/navigation.js';
import { initializeCrudPanel, resetAndLoadCrud } from './modules/ui/crudPanel.js';
import { initializeViewerPanel, resetAndLoadViewer } from './modules/ui/viewerPanel.js';
// import { destroyPlayer } from './modules/services/videoPlayerService.js';


document.addEventListener('DOMContentLoaded', () => {
    // 1. Inicializa a navegação principal (sidebar, troca de painéis)
    initializeNavigation();

    // 2. Inicializa a lógica específica de cada painel
    initializeCrudPanel();
    initializeViewerPanel();

    // 3. Carrega os dados do painel de CRUD por padrão
    // A navegação já deixou o painel visível, agora carregamos os dados nele.
    resetAndLoadCrud();

    // 4. Ouve por um evento personalizado para recarregar os dados de todos os painéis
    // Útil quando uma ação em um painel (ex: criar vídeo) deve atualizar outro (ex: lista de visualização)
    document.addEventListener('reloadAllVideos', () => {
        console.log('Evento reloadAllVideos recebido. Recarregando painéis...');
        resetAndLoadCrud();
        resetAndLoadViewer();
    });
});
