import { useEffect, useState } from 'react';
import { AppHubs } from '../../../../shared/enums/hub.enums';
import { useSocketListener } from '../../../../shared/hooks/useSocket';
import { AlertService } from '../../../../shared/services/alert.service';
import { socketService } from '../../../../shared/services/socket.service';


interface RefundStatusData {
    status: 'pending' | 'completed' | 'failed';
    message?: string;
} // [cite: 3]

export const useRefundNotification = (onSuccess?: () => void) => {
    const [refundStatus, setRefundStatus] = useState<'idle' | 'processing' | 'completed'>('idle'); // [cite: 4]

    // 1. Garante a conexão com o Hub de Reembolso ao montar
    useEffect(() => {
        const connectToHub = async () => {
            await socketService.connect(AppHubs.Refund); // [cite: 5, 26]
        };
        connectToHub();

        // Desconectar ao desmontar (boa prática para liberar recursos)
        return () => socketService.disconnect(AppHubs.Refund); // [cite: 5, 37]
    }, []);

    // 2. Ouve o evento "ReceiveRefundStatus"
    useSocketListener<RefundStatusData>(
        AppHubs.Refund, // [cite: 6, 26]
        'ReceiveRefundStatus', // [cite: 6]
        (data) => {
            console.log("Socket Refund:", data); // [cite: 6]

            if (data.status === 'completed') {
                setRefundStatus('completed'); // [cite: 6]

                // Dispara o Toast de Sucesso conforme seu arquivo
                AlertService.notify(
                    'Reembolso Confirmado!', // [cite: 7]
                    'O valor foi estornado para o seu cartão.', // [cite: 7]
                    'success'
                );

                if (onSuccess) onSuccess(); // Atualiza a lista se necessário [cite: 7]

            } else if (data.status === 'failed') {
                setRefundStatus('idle'); // [cite: 7]
                AlertService.notify('Falha no Reembolso', data.message, 'error'); // [cite: 7]
            }
        }
    );

    return { refundStatus, setRefundStatus }; // [cite: 8]
};