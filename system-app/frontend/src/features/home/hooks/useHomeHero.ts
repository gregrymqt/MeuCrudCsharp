import { useState, useCallback } from "react";
import { HomeService } from "../services/home.service";
import type { HeroFormValues } from "../types/home.types";
import { AlertService } from "../../../shared/services/alert.service";

export const useHomeHero = () => {
  const [isLoading, setIsLoading] = useState(false);

  // --- CRIAR HERO ---
  const createHero = useCallback(
    async (data: HeroFormValues, onSuccess?: () => void) => {
      setIsLoading(true);
      try {
        await HomeService.createHero(data);

        // Feedback de Sucesso
        await AlertService.success(
          "Sucesso!",
          "Novo slide adicionado ao Hero."
        );

        if (onSuccess) onSuccess();
      } catch (error) {
        // Tratamento de Erro
        AlertService.error(
          "Erro ao criar",
          "Não foi possível salvar o slide. Verifique os dados."
        );
        console.error(error);
      } finally {
        setIsLoading(false);
      }
    },
    []
  );

  // --- ATUALIZAR HERO ---
  const updateHero = useCallback(
    async (id: number, data: HeroFormValues, onSuccess?: () => void) => {
      setIsLoading(true);
      try {
        await HomeService.updateHero(id, data);

        // Toast não bloqueante para edição rápida
        AlertService.notify(
          "Atualizado",
          "As alterações no slide foram salvas.",
          "success"
        );

        if (onSuccess) onSuccess();
      } catch (error) {
        AlertService.error("Erro", "Falha ao atualizar o slide.");
        console.error(error);
      } finally {
        setIsLoading(false);
      }
    },
    []
  );

  // --- DELETAR HERO ---
  const deleteHero = useCallback(async (id: number, onSuccess?: () => void) => {
    // Confirmação Mobile-First
    const { isConfirmed } = await AlertService.confirm(
      "Remover Slide?",
      "Essa ação removerá a imagem e o texto do carrossel permanentemente.",
      "Sim, remover"
    );

    if (!isConfirmed) return;

    setIsLoading(true);
    try {
      await HomeService.deleteHero(id);

      AlertService.notify("Removido", "Slide removido com sucesso.", "info");

      if (onSuccess) onSuccess();
    } catch (error) {
      AlertService.error("Erro", "Não foi possível remover este item.");
      console.error(error);
    } finally {
      setIsLoading(false);
    }
  }, []);

  return {
    isLoading,
    createHero,
    updateHero,
    deleteHero,
  };
};
