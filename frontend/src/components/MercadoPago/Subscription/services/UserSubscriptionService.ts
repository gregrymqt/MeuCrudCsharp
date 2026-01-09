import  { ApiService } from "../../../../shared/services/api.service";
import type { SubscriptionDetailsDto, SubscriptionResponseDto } from "../types/userSubscription.type";


export const SubscriptionService = {

  getDetails: async (): Promise<SubscriptionDetailsDto> => {
    // Controller: [Route("api/subscriptions")] + [HttpGet("details")]
    // URL Final deve ser: /api/subscriptions/details
    return await ApiService.get<SubscriptionDetailsDto>('/subscriptions/details'); 
  },

  updateStatus: async (status: string): Promise<void> => {
    const body: Partial<SubscriptionResponseDto> = {
      status: status
    };
    // Controller: [Route("api/subscriptions")] + [HttpPut("status")]
    // URL Final deve ser: /api/subscriptions/status
    return await ApiService.put<void>('/subscriptions/status', body);
  }
};