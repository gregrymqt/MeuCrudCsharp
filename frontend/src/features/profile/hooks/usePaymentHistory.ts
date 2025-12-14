// features/profile/hooks/usePaymentHistory.ts
import { useState, useEffect, useCallback } from 'react';
import { ProfileService } from '../services/profile.service'; // Ajuste o caminho conforme sua estrutura
import type { PaymentItem } from '../types/profile.types';
import { ApiError } from '../../../shared/services/api.service';

export const usePaymentHistory = () => {
  const [payments, setPayments] = useState<PaymentItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchPayments = useCallback(async () => {
    try {
      setLoading(true);
      // Chama o endpoint GET /payment-history 
      const data = await ProfileService.getPaymentHistory();
      setPayments(data);
      setError(null);
    } catch (err) {
      if(err instanceof ApiError){
      console.error(err);
      setError('Não foi possível carregar o histórico de pagamentos.');
      }
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchPayments();
  }, [fetchPayments]);

  return { payments, loading, error, refetch: fetchPayments };
};