import { useState } from 'react';
import { ProfileService } from '../services/profile.service';
import { ApiError } from '../../../shared/services/api.service'; // Ajuste o caminho se necessário

export const useSubscriptionActions = (onSuccess?: () => void) => {
  const [loading, setLoading] = useState(false);

  // Alterar Status (Pausar, Cancelar, Reativar)
  const changeStatus = async (newStatus: 'paused' | 'cancelled' | 'active') => {
    // Mapa para texto amigável no confirm
    const actionText = {
      paused: 'pausar',
      cancelled: 'cancelar',
      active: 'reativar'
    };

    if (!window.confirm(`Tem certeza que deseja ${actionText[newStatus]} a assinatura?`)) return;

    try {
      setLoading(true);
      await ProfileService.updateSubscriptionStatus({ status: newStatus }); // [cite: 8]
      alert('Status atualizado com sucesso!');
      if (onSuccess) onSuccess();
    } catch (error) {
      // Tratamento de erro robusto baseado no seu LogErro.txt [cite: 9]
      const msg = error instanceof ApiError ? error.message : 'Erro ao atualizar status.';
      alert(msg);
    } finally {
      setLoading(false);
    }
  };

  // Trocar Cartão (Recebe o token do Mercado Pago)
  const changeCard = async (cardToken: string) => {
    try {
      setLoading(true);
      await ProfileService.updateSubscriptionCard({ token: cardToken }); // [cite: 12]
      alert('Cartão da assinatura atualizado com sucesso!');
      if (onSuccess) onSuccess();
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Erro ao atualizar cartão.';
      alert(msg);
      // Relançamos o erro para que o componente do Mercado Pago saiba que falhou visualmente
      throw err; 
    } finally {
      setLoading(false);
    }
  };

  // Solicitar Reembolso
  const requestRefund = async () => {
    if (!window.confirm("Atenção: Seu acesso será revogado imediatamente. Deseja pedir reembolso?")) return;

    try {
      setLoading(true);
      const res = await ProfileService.requestRefund(); // [cite: 16]
      alert(res.message);
      if (onSuccess) onSuccess();
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Erro ao solicitar reembolso.';
      alert(msg);
    } finally {
      setLoading(false);
    }
  };

  return { loading, changeStatus, changeCard, requestRefund };
};