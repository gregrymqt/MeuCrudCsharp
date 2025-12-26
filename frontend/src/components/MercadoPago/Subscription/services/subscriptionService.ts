import  { ApiService } from "../../../../shared/services/api.service";
import type { SubscriptionDetailsDto, SubscriptionResponseDto } from "../types/subscription.type";


export const SubscriptionService = {
  /**
   * Busca os detalhes da assinatura do usuário logado.
   * Endpoint: /api/subscription-details
   */
  getDetails: async (): Promise<SubscriptionDetailsDto> => {
    // O ApiService já concatena a BASE_URL (/api), então passamos apenas o endpoint relativo
    return await ApiService.get<SubscriptionDetailsDto>('/subscription-details');
  },

  /**
   * Atualiza o status da assinatura (pausar, reativar ou cancelar).
   * Endpoint: /api/subscription/status
   * * @param status - O novo status desejado.
   * Valores permitidos pelo backend: "paused", "authorized", "cancelled"
   */
  updateStatus: async (status: string): Promise<void> => {
    // O backend espera um objeto que mapeie para SubscriptionResponseDto.
    // Como o C# só lê a propriedade .Status no controller, podemos enviar um objeto parcial.
    const body: Partial<SubscriptionResponseDto> = {
      status: status
    };

    return await ApiService.put<void>('/subscription/status', body);
  }
};