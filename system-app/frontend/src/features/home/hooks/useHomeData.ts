import { useState, useCallback, useEffect } from "react";
import { HomeService } from "../services/home.service";
import type { HeroSlideData, ServiceData } from "../types/home.types";
import { ApiError } from "../../../shared/services/api.service";

export const useHomeData = () => {
  const [heroSlides, setHeroSlides] = useState<HeroSlideData[]>([]);
  const [services, setServices] = useState<ServiceData[]>([]);
  
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchData = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await HomeService.getHomeContent();
      
      // Separa os dados conforme a estrutura do DTO HomeContent
      setHeroSlides(data.hero || []);
      setServices(data.services || []);
      
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : "Erro ao carregar dados da Home.";
      setError(msg);
      // Opcional: Mostrar alerta visual apenas se for crítico
      console.error(msg);
    } finally {
      setIsLoading(false);
    }
  }, []);

  // Busca automática ao montar o componente
  useEffect(() => {
    fetchData();
  }, [fetchData]);

  return {
    heroSlides,
    services,
    isLoading,
    error,
    refreshData: fetchData, // Essencial para recarregar após um CRUD
  };
};