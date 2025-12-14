import { useEffect } from 'react';
import { socketService } from '../services/socket.service';
import { AppHubs } from '../enums/hub.enums';

export const useSocketListener = <T>(
  hub: AppHubs,       // <--- NOVO PARAMETRO: Qual Hub?
  eventName: string, 
  callback: (data: T) => void
) => {
  useEffect(() => {
    // 1. O hook apenas registra o ouvinte na conexão JÁ EXISTENTE
    socketService.on<T>(hub, eventName, callback);

    // 2. Limpeza ao desmontar componente
    return () => {
      socketService.off(hub, eventName);
    };
  }, [hub, eventName, callback]);
};