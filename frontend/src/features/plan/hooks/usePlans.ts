import { useState, useEffect, useCallback } from 'react';
import { planService } from '../services/planService';
import { type PlanUI } from '../types/plan.type';
import { ApiError } from '../../../shared/services/api.service'; // [cite: 26]

export const usePlans = (pageSize: number = 6) => {
  // Estado dos Dados
  const [plans, setPlans] = useState<PlanUI[]>([]);
  
  // Estados de Controle
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0); // [cite: 9]
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchPlans = useCallback(async (pageNum: number) => {
    setIsLoading(true);
    setError(null);
    try {
      // Chama o serviço
      const data = await planService.getAllPaginated(pageNum, pageSize);
      
      setPlans(data.items); // [cite: 7]
      setTotalPages(data.totalPages); // [cite: 9]
      
    } catch (err) {
      // Tratamento de erro usando sua classe customizada
      if (err instanceof ApiError) {
        setError(err.message || 'Erro ao carregar planos.'); // [cite: 26]
      } else {
        setError('Não foi possível carregar os planos no momento.');
      }
    } finally {
      setIsLoading(false);
    }
  }, [pageSize]);

  // Carga inicial e quando a página muda
  useEffect(() => {
    fetchPlans(page);
  }, [page, fetchPlans]);

  // Funções de Navegação
  const nextPage = () => {
    if (page < totalPages) setPage(p => p + 1);
  };

  const prevPage = () => {
    if (page > 1) setPage(p => p - 1);
  };

  const refresh = () => fetchPlans(page);

  return {
    plans,
    isLoading,
    error,
    pagination: {
      currentPage: page,
      totalPages,
      nextPage,
      prevPage,
      hasNext: page < totalPages,
      hasPrev: page > 1
    },
    refresh
  };
};