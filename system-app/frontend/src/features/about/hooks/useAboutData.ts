import { useState, useCallback, useEffect } from "react";
import { AboutService } from "../services/about.service"; // Ajuste o caminho se necessário
import type { AboutSectionData, TeamMember } from "../types/about.types";

export const useAboutData = () => {
  const [sections, setSections] = useState<AboutSectionData[]>([]);
  const [teamMembers, setTeamMembers] = useState<TeamMember[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const response = await AboutService.getAboutPage();
      
      // Mapeia conforme a estrutura do seu DTO
      setSections(response.sections || []);
      setTeamMembers(response.teamSection?.members || []);
      
    } catch (err) {
      console.error("Erro ao buscar dados da página About", err);
      setError("Não foi possível carregar os dados.");
    } finally {
      setIsLoading(false);
    }
  }, []);

  // Busca automática ao montar o componente
  useEffect(() => {
    fetchData();
  }, [fetchData]);

  return {
    sections,
    teamMembers,
    isLoading,
    error,
    refreshData: fetchData, // Exportamos a função para permitir recarregamento manual (ex: após um CRUD)
  };
};