import { useState, useCallback } from "react";
import { HomeService } from "../services/home.service";
import type { ServiceFormValues } from "../types/home.types";
import { AlertService } from "../../../shared/services/alert.service";

export const useHomeServices = () => {
  const [isLoading, setIsLoading] = useState(false);

  // --- CRIAR SERVIÇO ---
  const createService = useCallback(
    async (data: ServiceFormValues, onSuccess?: () => void) => {
      setIsLoading(true);
      try {
        await HomeService.createService(data);

        await AlertService.success(
          "Serviço Criado!",
          "O novo card de serviço já está visível."
        );

        if (onSuccess) onSuccess();
      } catch (error) {
        AlertService.error("Atenção", "Erro ao criar o serviço.");
        console.error(error);
      } finally {
        setIsLoading(false);
      }
    },
    []
  );

  // --- ATUALIZAR SERVIÇO ---
  const updateService = useCallback(
    async (id: number, data: ServiceFormValues, onSuccess?: () => void) => {
      setIsLoading(true);
      try {
        await HomeService.updateService(id, data);

        AlertService.notify("Serviço Atualizado", "Dados salvos.", "success");

        if (onSuccess) onSuccess();
      } catch (error) {
        AlertService.error("Erro", "Falha ao salvar as alterações.");
        console.error(error);
      } finally {
        setIsLoading(false);
      }
    },
    []
  );

  // --- DELETAR SERVIÇO ---
  const deleteService = useCallback(
    async (id: number, onSuccess?: () => void) => {
      const { isConfirmed } = await AlertService.confirm(
        "Excluir Serviço?",
        "Este card não aparecerá mais na página inicial.",
        "Sim, excluir"
      );

      if (!isConfirmed) return;

      setIsLoading(true);
      try {
        await HomeService.deleteService(id);

        AlertService.notify("Excluído", "Serviço removido.", "info");

        if (onSuccess) onSuccess();
      } catch (error) {
        AlertService.error("Erro", "Não foi possível excluir o serviço.");
        console.error(error);
      } finally {
        setIsLoading(false);
      }
    },
    []
  );

  return {
    isLoading,
    createService,
    updateService,
    deleteService,
  };
};
