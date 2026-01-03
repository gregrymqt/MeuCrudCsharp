import { useState, useCallback } from "react";
import type { TeamMemberFormValues } from "../types/about.types";
import { AboutService } from "../services/about.service";
import { AlertService } from "../../../shared/services/alert.service";


export const useTeamMembers = () => {
  const [isLoading, setIsLoading] = useState(false);

  // --- ADICIONAR MEMBRO ---
  const addMember = useCallback(
    async (data: TeamMemberFormValues, onSuccess?: () => void) => {
      setIsLoading(true);
      try {
        await AboutService.createTeamMember(data);

        // Feedback visual
        await AlertService.success(
          "Bem-vindo!",
          "Novo membro adicionado ao time."
        );

        if (onSuccess) onSuccess();
      } catch (error) {
        AlertService.error(
          "Atenção",
          "Não foi possível adicionar o membro. Verifique os dados."
        );
        console.error(error);
      } finally {
        setIsLoading(false);
      }
    },
    []
  );

  // --- ATUALIZAR MEMBRO ---
  const updateMember = useCallback(
    async (
      id: number | string,
      data: TeamMemberFormValues,
      onSuccess?: () => void
    ) => {
      setIsLoading(true);
      try {
        await AboutService.updateTeamMember(id, data);

        // Toast rápido para edição
        AlertService.notify(
          "Membro atualizado",
          "Dados salvos com sucesso.",
          "success"
        );

        if (onSuccess) onSuccess();
      } catch (error) {
        AlertService.error(
          "Erro",
          "Falha ao atualizar as informações do membro."
        );
        console.error(error);
      } finally {
        setIsLoading(false);
      }
    },
    []
  );

  // --- REMOVER MEMBRO ---
  const deleteMember = useCallback(
    async (id: number | string, onSuccess?: () => void) => {
      // Confirmação Mobile-First (botões invertidos)
      const { isConfirmed } = await AlertService.confirm(
        "Remover membro?",
        "O colaborador será removido da lista da equipe.",
        "Sim, remover"
      );

      if (!isConfirmed) return;

      setIsLoading(true);
      try {
        await AboutService.deleteTeamMember(id);

        AlertService.notify("Removido", "Membro removido da equipe.", "info");

        if (onSuccess) onSuccess();
      } catch (error) {
        AlertService.error("Erro", "Não foi possível remover este membro.");
        console.error(error);
      } finally {
        setIsLoading(false);
      }
    },
    []
  );

  return {
    isLoading,
    addMember,
    updateMember,
    deleteMember,
  };
};
