import { useState, useEffect } from 'react';
import { socketService } from '../../../../shared/services/socket.service';
import { AlertService } from '../../../../shared/services/alert.service';
import { AppHubs } from '../../../../shared/enums/hub.enums';
import { VideoStatus } from '../../../../types/models';

interface ProgressData {
  message: string;
  progress: number;
  isComplete: boolean;
  isError: boolean;
}

export const useVideoProcessing = (
  storageIdentifier: string, 
  currentStatus: number,
  onComplete?: () => void
) => {
  const [progress, setProgress] = useState(0);
  const [statusMessage, setStatusMessage] = useState('');
  const [isProcessing, setIsProcessing] = useState(currentStatus === VideoStatus.Processing);

  useEffect(() => {
    // Só conecta se o vídeo estiver processando
    if (currentStatus !== VideoStatus.Processing) return;

    const hub = AppHubs.Video;
    let mounted = true;

    const startConnection = async () => {
      // 1. Conectar ao Hub
      await socketService.connect(hub);

      // 2. Ouvir atualizações (Callback)
      socketService.on<ProgressData>(hub, 'ProgressUpdate', (data) => {
        if (!mounted) return;

        setProgress(data.progress);
        setStatusMessage(data.message);

        if (data.isComplete) {
          setIsProcessing(false);
          AlertService.notify('Processamento Concluído', 'O vídeo está pronto para uso.', 'success');
          if (onComplete) onComplete();
        }

        if (data.isError) {
          setIsProcessing(false);
          AlertService.notify('Erro no Processamento', data.message, 'error');
          if (onComplete) onComplete(); // Recarrega para pegar status de erro
        }
      });

      // 3. Inscrever-se no grupo específico desse vídeo (Invoke do Backend)
      // O backend espera: public async Task SubscribeToJobProgress(string storageIdentifier)
      await socketService.invoke(hub, 'SubscribeToJobProgress', storageIdentifier);
    };

    startConnection();

    // Cleanup: Remove o listener ao desmontar ou mudar de vídeo
    return () => {
      mounted = false;
      socketService.off(hub, 'ProgressUpdate');
      // Opcional: Desconectar se não houver mais vídeos (pode ser complexo gerenciar, deixar conectado é ok)
    };
  }, [storageIdentifier, currentStatus, onComplete]);

  return { progress, statusMessage, isProcessing };
};