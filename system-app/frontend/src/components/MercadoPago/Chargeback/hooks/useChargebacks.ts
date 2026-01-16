import { useState, useEffect, useCallback } from "react";
import { ChargebackService } from "../services/chargeBack.service";
import type { ChargebackPaginatedResponse } from "../types/chargeback.type";
import { ApiError } from "../../../../shared/services/api.service";

// Estado inicial dos filtros
interface ChargebackFilters {
  page: number;
  searchTerm: string;
  statusFilter: string;
}

export const useChargebacks = () => {
  const [data, setData] = useState<ChargebackPaginatedResponse | null>(null);
  const [loading, setLoading] = useState<boolean>(true); // Começa carregando
  const [error, setError] = useState<string | null>(null);

  // Estado local dos filtros
  const [filters, setFilters] = useState<ChargebackFilters>({
    page: 1,
    searchTerm: "",
    statusFilter: "",
  });

  // Função que chama o Service
  const fetchChargebacks = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await ChargebackService.getAll(
        filters.page,
        filters.searchTerm,
        filters.statusFilter
      );
      setData(response);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
        console.error(err);
      }
    } finally {
      setLoading(false);
    }
  }, [filters]); // Recria a função se os filtros mudarem

  // Effect: Dispara a busca sempre que os filtros (page/search/status) mudarem
  useEffect(() => {
    fetchChargebacks();
  }, [fetchChargebacks]);

  // Helpers para atualizar filtros facilmente no componente visual
  const setPage = (page: number) => setFilters((prev) => ({ ...prev, page }));
  const setSearchTerm = (term: string) =>
    setFilters((prev) => ({ ...prev, searchTerm: term, page: 1 })); // Resetar p/ pág 1 ao buscar
  const setStatusFilter = (status: string) =>
    setFilters((prev) => ({ ...prev, statusFilter: status, page: 1 }));

  return {
    // Dados
    chargebacks: data?.chargebacks || [],
    pagination: {
      currentPage: data?.currentPage || 1,
      totalPages: data?.totalPages || 1,
      hasPreviousPage: data?.hasPreviousPage || false,
      hasNextPage: data?.hasNextPage || false,
    },
    // Estados de UI
    loading,
    error,
    // Ações para o componente usar
    filters,
    setPage,
    setSearchTerm,
    setStatusFilter,
    refresh: fetchChargebacks, // Caso precise recarregar manualmente
  };
};
