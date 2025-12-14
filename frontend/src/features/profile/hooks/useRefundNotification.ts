import { useEffect, useState } from 'react';
import { socketService } from '../../../shared/services/socket.service';
import { AppHubs } from '../../../shared/enums/hub.enums'; // 
import { AlertService } from '../../../shared/services/alert.service'; // 
import { useSocketListener } from '../../../shared/hooks/useSocket';

interface RefundStatusData {
  status: 'pending' | 'completed' | 'failed';
  message?: string;
}

export const useRefundNotification = (onSuccess?: () => void) => {
  const [refundStatus, setRefundStatus] = useState<'idle' | 'processing' | 'completed'>('idle');

  // 1. Garante a conexão com o Hub de Reembolso ao montar o componente
  useEffect(() => {
    const connectToHub = async () => {
      await socketService.connect(AppHubs.Refund); // Conecta no /RefundProcessingHub 
    };
    connectToHub();

    // Opcional: Desconectar ao desmontar se quiser economizar recursos
    // return () => socketService.disconnect(AppHubs.Refund);
  }, []);

  // 2. Ouve o evento "ReceiveRefundStatus" (mesmo nome do legado )
  useSocketListener<RefundStatusData>(
    AppHubs.Refund, 
    'ReceiveRefundStatus', 
    (data) => {
      console.log("Socket Refund:", data);

      if (data.status === 'completed') {
        setRefundStatus('completed');
        
        // Substitui o Swal manual do legado [cite: 4, 5] pelo seu Service 
        AlertService.notify(
          'Reembolso Confirmado!',
          'O valor foi estornado para o seu cartão.',
          'success'
        );

        if (onSuccess) onSuccess();
      } else if (data.status === 'failed') {
        setRefundStatus('idle');
        AlertService.notify('Falha no Reembolso', data.message, 'error');
      }
    }
  );

  return { refundStatus, setRefundStatus };
};