import { useState, useCallback } from "react";
import { AlertService } from "../../../shared/services/alert.service";
import { AboutService } from "../services/about.service";
import type { AboutSectionFormValues } from "../types/about.types";

export const useAboutSection = () => {
  const [isLoading, setIsLoading] = useState(false);

  // --- CRIAR NOVA SEÇÃO ---
  const createSection = useCallback(
    async (data: AboutSectionFormValues, onSuccess?: () => void) => {
      setIsLoading(true);
      try {
        await AboutService.createSection(data);

        // Feedback de Sucesso
        await AlertService.success(
          "Sucesso!",
          "A nova seção foi criada corretamente."
        );

        if (onSuccess) onSuccess();
      } catch (error) {
        // Feedback de Erro
        AlertService.error(
          "Erro ao criar",
          "Não foi possível criar a seção. Tente novamente."
        );
        console.error(error);
      } finally {
        setIsLoading(false);
      }
    },
    []
  );

  // --- ATUALIZAR SEÇÃO ---
  const updateSection = useCallback(
    async (
      id: number,
      data: AboutSectionFormValues,
      onSuccess?: () => void
    ) => {
      setIsLoading(true);
      try {
        await AboutService.updateSection(id, data);

        // Notificação não bloqueante para update (opcional, ou use success)
        AlertService.notify(
          "Atualizado",
          "As alterações foram salvas.",
          "success"
        );

        if (onSuccess) onSuccess();
      } catch (error) {
        AlertService.error(
          "Erro na atualização",
          "Falha ao salvar as alterações."
        );
        console.error(error);
      } finally {
        setIsLoading(false);
      }
    },
    []
  );

  // --- DELETAR SEÇÃO ---
  const deleteSection = useCallback(
    async (id: number, onSuccess?: () => void) => {
      // 1. Confirmação do Usuário
      const { isConfirmed } = await AlertService.confirm(
        "Tem certeza?",
        "Você está prestes a remover esta seção. Essa ação não pode ser desfeita.",
        "Sim, remover"
      );

      if (!isConfirmed) return;

      setIsLoading(true);
      try {
        await AboutService.deleteSection(id);

        AlertService.notify(
          "Removido",
          "A seção foi excluída com sucesso.",
          "success"
        );

        if (onSuccess) onSuccess();
      } catch (error) {
        AlertService.error(
          "Erro ao excluir",
          "Não foi possível remover o item."
        );
        console.error(error);
      } finally {
        setIsLoading(false);
      }
    },
    []
  );

  return {
    isLoading,
    createSection,
    updateSection,
    deleteSection,
  };
};
