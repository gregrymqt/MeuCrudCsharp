import { useState, useEffect, useCallback } from "react";
import { ApiError } from "../../../../shared/services/api.service";
import { TransactionService } from "../services/transactions.service";
import type { PaymentItems } from "../types/transactions.type";

export const usePaymentHistory = () => {
  const [payments, setPayments] = useState<PaymentItems[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchPayments = useCallback(async () => {
    try {
      setLoading(true);
      const data = await TransactionService.getPaymentHistory();

      // Ordenação: Backend já deve mandar ordenado, mas garante aqui também
      const sorted = data.sort(
        (a, b) =>
          new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
      );

      setPayments(sorted);
      setError(null);
    } catch (err) {
      const msg = err as ApiError;
      setError(msg ? msg.message : "Erro ao carregar o histórico.");
      console.error(err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchPayments();
  }, [fetchPayments]);

  return { payments, loading, error, refetch: fetchPayments };
};
