import { useState } from 'react';
import { AlertService } from '../../../../shared/services/alert.service';
import { ApiError } from '../../../../shared/services/api.service';
import { TransactionService } from '../services/transactions.service';


export const useRefund = () => {
    const [isProcessing, setIsProcessing] = useState(false);

    const requestRefund = async (paymentId: string) => {
        // 1. Confirmação (Usa seu AlertService.confirm do arquivo)
        const { isConfirmed } = await AlertService.confirm(
            'Solicitar Estorno?',
            'O processo será iniciado e você será notificado assim que concluir.' // [cite: 24]
        );

        if (!isConfirmed) return; // [cite: 25]

        try {
            setIsProcessing(true);

            // 2. Apenas envia o pedido HTTP
            await TransactionService.requestRefund(paymentId);

            // Feedback imediato de que o pedido foi ENVIADO (não concluído)
            AlertService.notify(
                'Solicitação Enviada',
                'Aguarde a confirmação...',
                'info' // [cite: 20]
            );

        } catch (err) {
            const msg = err instanceof ApiError ? err.message : 'Erro ao solicitar.';
            AlertService.error('Erro', msg); // [cite: 16]
        } finally {
            setIsProcessing(false);
        }
    };

    return { requestRefund, isProcessing };
};