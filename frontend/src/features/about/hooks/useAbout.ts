import { useState, useCallback } from 'react';
import { AlertService } from '../../../shared/services/alert.service';
import { AboutService } from '../services/about.service';
import type { AboutSectionContent } from '../types/about.types';


export const useAbout = () => {
  // Estado para armazenar a lista de seções (Texto ou Time)
  const [sections, setSections] = useState<AboutSectionContent[]>([]);
  
  // Estado de carregamento para feedback visual
  const [isLoading, setIsLoading] = useState<boolean>(false);

  /**
   * Função para buscar os dados.
   * Usamos useCallback para que ela possa ser dependência de useEffects sem loop infinito.
   */
  const fetchSections = useCallback(async () => {
    setIsLoading(true);
    try {
      const data = await AboutService.getAllSections();
      setSections(data);
    } catch (error) {
      // Usa seu AlertService para mostrar erro visualmente ao usuário 
      AlertService.error(
        'Ops!',
        'Não foi possível carregar as informações sobre nós. Tente novamente mais tarde.'
      );
      console.error('Erro ao buscar seções sobre nós:', error);
    } finally {
      setIsLoading(false);
    }
  }, []);

  return {
    sections,
    isLoading,
    fetchSections
  };
};