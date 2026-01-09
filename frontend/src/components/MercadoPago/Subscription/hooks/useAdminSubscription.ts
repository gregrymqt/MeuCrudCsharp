import { useState, useCallback } from 'react';
import { AdminSubscriptionService } from '../services/AdminSubscriptionService';
import { ApiError } from '../../../../shared/services/api.service';
import type { AdminSubscriptionDetail } from '../types/adminSubscription.type';

export const useAdminSubscription = () => {
  const [subscription, setSubscription] = useState<AdminSubscriptionDetail | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Função para buscar a assinatura (pelo ID ou Query)
  const searchSubscription = useCallback(async (query: string) => {
    if (!query) return;

    setLoading(true);
    setError(null);
    setSubscription(null); // Limpa o estado anterior enquanto busca

    try {
      const data = await AdminSubscriptionService.searchSubscription(query);
      setSubscription(data);
    } catch (err) {
      // Tenta pegar a mensagem do backend ou usa uma genérica
      const msg = err instanceof ApiError ? err.message : 'Erro ao buscar assinatura.';
      setError(msg);
    } finally {
      setLoading(false);
    }
  }, []);

  // Função para atualizar o Valor
  const updateValue = useCallback(async (amount: number) => {
    if (!subscription?.id) {
        setError('Nenhuma assinatura selecionada para atualização.');
        return;
    }

    setLoading(true);
    setError(null);

    try {
      const updatedData = await AdminSubscriptionService.updateValue(subscription.id, amount);
      // Atualiza o estado local com os dados novos retornados pela API
      setSubscription(updatedData);
    } catch (err) {
        const msg = err instanceof ApiError ? err.message : 'Erro ao atualizar valor.';
        setError(msg);
    } finally {
      setLoading(false);
    }
  }, [subscription]);

  // Função para atualizar o Status
  const updateStatus = useCallback(async (status: 'authorized' | 'paused' | 'cancelled') => {
    if (!subscription?.id) {
        setError('Nenhuma assinatura selecionada para atualização.');
        return;
    }

    setLoading(true);
    setError(null);

    try {
      const updatedData = await AdminSubscriptionService.updateStatus(subscription.id, status);
      // Atualiza o estado local com os dados novos retornados pela API
      setSubscription(updatedData);
    } catch (err) {
      const msg = err instanceof ApiError ? err.message : 'Erro ao atualizar status.';
      setError(msg);
    } finally {
      setLoading(false);
    }
  }, [subscription]);

  // Utilitário para limpar erros manualmente na UI (ex: ao fechar um toast)
  const clearError = useCallback(() => setError(null), []);

  return {
    subscription,
    loading,
    error,
    searchSubscription,
    updateValue,
    updateStatus,
    clearError
  };
};