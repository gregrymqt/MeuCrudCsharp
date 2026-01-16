import { useState, useEffect, useCallback } from "react";
import { WalletService } from "../services/wallet.service";
import type { WalletCard } from "../types/wallet.type";
import { AlertService } from "../../../../shared/services/alert.service";
import { ApiError } from "../../../../shared/services/api.service";

export const useWallet = () => {
  const [cards, setCards] = useState<WalletCard[]>([]);
  const [loading, setLoading] = useState<boolean>(false);

  // --- 1. BUSCAR CARTÕES ---
  const fetchCards = useCallback(async () => {
    setLoading(true);
    try {
      const data = await WalletService.getAllCards();
      setCards(data);
    } catch (error) {
      if (error instanceof ApiError) {
        console.error(error);
        AlertService.notify(
          "Erro de conexão",
          error.message,
          "error"
        );
      }
    } finally {
      setLoading(false);
    }
  }, []);

  // Carrega automaticamente ao montar o componente
  useEffect(() => {
    fetchCards();
  }, [fetchCards]);

  // --- 2. ADICIONAR CARTÃO ---
  const addCard = async (cardToken: string): Promise<boolean> => {
    setLoading(true);
    try {
      const newCard = await WalletService.addCard({ cardToken });

      // Atualiza a lista localmente adicionando o novo item (sem precisar refetch)
      setCards((prev) => [...prev, newCard]);

      await AlertService.success(
        "Cartão Adicionado!",
        `Final ${newCard.lastFourDigits} salvo na sua carteira.`
      );
      return true; // Sucesso
    } catch (error) {
      if (error instanceof ApiError) {
        console.error(error);
        AlertService.error("Erro ao adicionar", error.message);
        return false; // Falha
      }
      throw error;
    } finally {
      setLoading(false);
    }
  };

  // --- 3. REMOVER CARTÃO ---
  const removeCard = async (cardId: string) => {
    // A. Validação de Segurança Visual (Front-end First)
    const targetCard = cards.find((c) => c.id === cardId);

    if (targetCard?.isSubscriptionActiveCard) {
      // Bloqueia e avisa sem nem chamar o backend
      AlertService.error(
        "Ação Bloqueada",
        "Este cartão é o método de pagamento da sua assinatura ativa. Adicione outro cartão antes de remover este."
      );
      return;
    }

    // B. Confirmação do Usuário
    const confirmation = await AlertService.confirm(
      "Remover cartão?",
      `Deseja excluir o cartão final ${targetCard?.lastFourDigits}?`
    );

    if (!confirmation.isConfirmed) return;

    // C. Chamada para API
    setLoading(true);
    try {
      await WalletService.deleteCard(cardId);

      // Remove da lista local
      setCards((prev) => prev.filter((c) => c.id !== cardId));

      // Feedback não intrusivo (Toast)
      AlertService.notify(
        "Removido",
        "Cartão excluído com sucesso.",
        "success"
      );
    } catch (error) {
      if (error instanceof ApiError) {
        console.error(error);
        AlertService.error("Erro ao remover", error.message);
        return;
      }
    } finally {
      setLoading(false);
    }
  };

  return {
    cards,
    loading,
    addCard,
    removeCard,
    refreshWallet: fetchCards, // Exposto caso você queira um botão de "Recarregar" manual
  };
};
