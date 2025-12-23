import { useState, useEffect, useCallback } from 'react';
import { ApiError } from '../../../../shared/services/api.service';
import { TransactionService } from '../services/transactions.service';
import type { PaymentItems } from '../types/transactions.type';


export const usePaymentHistory = () => {
    const [payments, setPayments] = useState<PaymentItems[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const fetchPayments = useCallback(async () => {
        try {
            setLoading(true);
            const data = await TransactionService.getPaymentHistory(); // [cite: 17]
            // Ordenação opcional: Do mais recente para o mais antigo
            const sorted = data.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
            setPayments(sorted);
            setError(null);
        } catch (err) {
            if (err instanceof ApiError) {
                console.error(err);
                setError('Não foi possível carregar o histórico de pagamentos.');
            }
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        fetchPayments(); // [cite: 19]
    }, [fetchPayments]);

    return { payments, loading, error, refetch: fetchPayments };
};