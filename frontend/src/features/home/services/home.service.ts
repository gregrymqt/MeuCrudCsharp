import { ApiService } from '../../../shared/services/api.service';
import { 
  ContentType, 
  type RawContentItem, 
  type HomeContent, 
} from '../types/home.types';

export const HomeService = {
  /**
   * Busca todos os conteúdos da Home e os separa por categoria.
   */
  getHomeData: async (): Promise<HomeContent> => {
    // 1. Busca lista completa do endpoint (ex: /contents?page=home)
    const rawData = await ApiService.get<RawContentItem[]>('/contents'); 

    // 2. Inicializa o objeto de resposta
    const content: HomeContent = {
      hero: [],
      services: [],
      about: null
    };

    // 3. Itera e distribui (Map & Filter numa única passada para performance)
    rawData.forEach(item => {
      switch (item.contentType) {
        
        case ContentType.HERO:
          content.hero.push({
            id: item.id,
            title: item.title,
            subtitle: item.subtitle || '',
            imageUrl: item.imageUrl || '',
            actionText: item.actionText || 'Saiba Mais',
            actionUrl: item.actionUrl || '#'
          });
          break;

        case ContentType.SERVICE:
          content.services.push({
            id: item.id,
            title: item.title,
            description: item.description || '',
            iconClass: item.iconClass || 'fas fa-star', // Fallback icon
            actionText: item.actionText || 'Ver Detalhes',
            actionUrl: item.actionUrl || '#'
          });
          break;

        case ContentType.ABOUT:
          // Geralmente About é um item único, pegamos o último ou primeiro encontrado
          content.about = {
            id: item.id,
            title: item.title,
            description: item.description || '',
            imageUrl: item.imageUrl || '',
            // Supomos que os benefícios venham numa string separada por ponto e vírgula no 'metadata'
            benefits: item.metadata ? item.metadata.split(';') : [] 
          };
          break;
      }
    });

    return content;
  }
};