import { useState, useEffect, useCallback } from 'react';
import  { AlertService } from '../../../../shared/services/alert.service';
import { ClaimService } from '../services/claim.service';
import type { ChatMessage, ReplyFormData } from '../types/claims.type';
import { ApiError } from '../../../../shared/services/api.service';


// Props para saber quem está usando o hook
interface UseClaimChatProps {
  claimId: number;
  role: 'admin' | 'user';
}

export const useClaimChatLogic = ({ claimId, role }: UseClaimChatProps) => {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isSending, setIsSending] = useState(false);

  // 1. Função de Busca (Memoizada para usar no useEffect)
  const fetchMessages = useCallback(async () => {
    try {
      setIsLoading(true);
      let data;
      
      // Decide qual service chamar baseado na role
      if (role === 'admin') {
        data = await ClaimService.admin.getDetails(claimId);
      } else {
        data = await ClaimService.user.getMyDetails(claimId);
      }

      if (data && data.messages) {
        setMessages(data.messages);
      }
    } catch (error) {
      console.error("Erro ao buscar mensagens", error);
    } finally {
      setIsLoading(false);
    }
  }, [claimId, role]);

  // 2. Polling e Carga Inicial
  useEffect(() => {
    fetchMessages(); // Carga inicial

    // Polling a cada 30 segundos para simular tempo real
    const interval = setInterval(() => {
        // Chamada silenciosa (sem setar isLoading global para não piscar a tela)
        const silentUpdate = async () => {
             try {
                const data = role === 'admin' 
                    ? await ClaimService.admin.getDetails(claimId)
                    : await ClaimService.user.getMyDetails(claimId);
                if (data?.messages) setMessages(data.messages);
             } catch (e) { console.error("Erro ao buscar mensagens", e);}
        };
        silentUpdate();
    }, 30000);

    return () => clearInterval(interval);
  }, [fetchMessages, claimId, role]);

  // 3. Função de Envio (Conectada ao GenericForm)
  const handleSendResponse = async (formData: ReplyFormData) => {
    // Validação básica
    if (!formData.message) return;

    setIsSending(true);
    try {
      if (role === 'admin') {
        await ClaimService.admin.reply(claimId, formData.message);
      } else {
        await ClaimService.user.reply(claimId, formData.message);
      }

      // Feedback visual e atualização
      AlertService.notify('Sucesso', 'Mensagem enviada.', 'success');
      
      // Limpa o form (o GenericForm cuida do reset se configurado, ou atualizamos a lista)
      await fetchMessages(); 
      return true; // Retorna true para o GenericForm saber que deu certo
    } catch (error) {
      if (error instanceof ApiError){
        AlertService.error('Erro', error.message);
        return false;
      } 
      AlertService.error('Erro', 'Não foi possível enviar a mensagem.');
      return false;
    } finally {
      setIsSending(false);
    }
  };

  // 4. Função Extra para User: Escalar Mediação
  const handleRequestMediation = async () => {
    if (role !== 'user') return;

    const { isConfirmed } = await AlertService.confirm(
        'Tem certeza?', 
        'Isso envolverá o Mercado Pago para julgar o caso.'
    );

    if (isConfirmed) {
        try {
            await ClaimService.user.requestMediation(claimId);
            AlertService.success('Solicitada', 'O Mercado Pago irá intervir.');
            await fetchMessages(); // Atualiza status
        } catch (error) {
          if (error instanceof ApiError){
            AlertService.error('Erro', error.message);
            return;
          }
            AlertService.error('Erro', 'Falha ao solicitar mediação.');
        }
    }
  };

  return { 
    messages, 
    handleSendResponse, 
    handleRequestMediation, // Exportado caso precise usar no botão
    isLoading: isLoading && messages.length === 0, // Loading inicial
    isSending 
  };
};