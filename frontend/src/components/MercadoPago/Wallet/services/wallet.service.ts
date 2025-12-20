import { ApiService } from "../../../../shared/services/api.service";
import type { WalletCard, AddCardPayload } from "../types/wallet.type";


const ENDPOINT = '/v1/wallet';

export const WalletService = {
  
  /**
   * Busca todos os cartões do usuário logado.
   * O backend já cruza os dados para dizer qual é o "isSubscriptionActiveCard".
   */
  getAllCards: async (): Promise<WalletCard[]> => {
    // O ApiService já monta '/api' + '/v1/wallet'
    return await ApiService.get<WalletCard[]>(ENDPOINT);
  },

  /**
   * Envia o token do cartão para ser salvo no Customer do MP.
   * Retorna o cartão criado já formatado.
   */
  addCard: async (payload: AddCardPayload): Promise<WalletCard> => {
    return await ApiService.post<WalletCard>(ENDPOINT, payload);
  },

  /**
   * Remove um cartão.
   * O Backend valida se o cartão é o da assinatura ativa e lança erro 400 se for.
   */
  deleteCard: async (cardId: string): Promise<void> => {
    return await ApiService.delete<void>(`${ENDPOINT}/${cardId}`);
  }
};