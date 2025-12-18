import { useState } from 'react';
import { ProfileService } from '../services/profile.service';
import { ApiError } from '../../../shared/services/api.service';
import { AlertService } from '../../../shared/services/alert.service'; // [cite: 36] Certifique-se do caminho correto

export const useSubscriptionActions = (onSuccess?: () => void) => {
  const [loading, setLoading] = useState(false);

  // Alterar Status (Pausar, Cancelar, Reativar)
  const changeStatus = async (newStatus: 'paused' | 'cancelled' | 'active') => {
    const actionText = {
      paused: 'pausar',
      cancelled: 'cancelar',
      active: 'reativar'
    };

    // [Refatoração] Substituindo window.confirm pelo AlertService
    const { isConfirmed } = await AlertService.confirm(
      'Confirmar alteração',
      `Tem certeza que deseja ${actionText[newStatus]} a assinatura?`
    );

    if (!isConfirmed) return;

    try {
      setLoading(true);
      await ProfileService.updateSubscriptionStatus({ status: newStatus }); // [cite: 39]
      
      // [Refatoração] Feedback de sucesso visual
      AlertService.success('Sucesso!', 'Status atualizado com sucesso.');
      
      if (onSuccess) onSuccess();
    } catch (error) {
      const msg = error instanceof ApiError ? error.message : 'Erro ao atualizar status.';
      // [Refatoração] Feedback de erro visual
      AlertService.error('Erro', msg);
    } finally {
      setLoading(false);
    }
  };

  // Trocar Cartão (Recebe o token do Mercado Pago)
  const changeCard = async (cardToken: string) => {
    try {
      setLoading(true);
      await ProfileService.updateSubscriptionCard({ token: cardToken }); // [cite: 43]
      
      AlertService.success('Tudo certo!', 'Cartão da assinatura atualizado.');
      
      if (onSuccess) onSuccess();
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Erro ao atualizar cartão.';
      AlertService.error('Falha na atualização', msg);
      
      // Mantemos o throw para que o MercadoPagoBrick saiba que deu erro
      throw err; 
    } finally {
      setLoading(false);
    }
  };

  // Solicitar Reembolso
  const requestRefund = async () => {
    // [Refatoração] Confirmação crítica com AlertService
    const { isConfirmed } = await AlertService.confirm(
      'Atenção!', 
      'Seu acesso será revogado imediatamente. Deseja realmente pedir reembolso?',
      'Sim, pedir reembolso',
      'Cancelar'
    );

    if (!isConfirmed) return;

    try {
      setLoading(true);
      const res = await ProfileService.requestRefund(); // [cite: 48]
      
      AlertService.success('Solicitado!', res.message || 'Reembolso em processamento.');
      
      if (onSuccess) onSuccess();
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Erro ao solicitar reembolso.';
      AlertService.error('Erro', msg);
    } finally {
      setLoading(false);
    }
  };

  return { loading, changeStatus, changeCard, requestRefund };
};