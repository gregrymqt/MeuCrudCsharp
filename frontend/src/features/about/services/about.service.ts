import { ApiService } from '../../../shared/services/api.service'; // Ajuste o caminho conforme necessário
import type { AboutSectionContent } from '../types/about.types';

// Endpoint base para esta feature
const ENDPOINT = '/about-sections';

export const AboutService = {
  /**
   * Busca todas as seções da página Sobre Nós.
   * O backend deve retornar uma lista misturada de seções (Hero e Team).
   */
  getAllSections: async (): Promise<AboutSectionContent[]> => {
    // Usa o método GET genérico da sua ApiService [cite: 24]
    return await ApiService.get<AboutSectionContent[]>(ENDPOINT);
  }
};