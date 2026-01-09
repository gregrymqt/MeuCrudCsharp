// Baseado no input do chat: public record AutoRecurringDto
export interface AutoRecurringDto {
  frequency: number;
  frequency_type: string | null;
  transaction_amount: number;
  currency_id: string | null;
  start_date: string; // DateTime ISO string
  end_date: string;   // DateTime ISO string
}

// Baseado no LogErro.txt 
export interface SubscriptionResponseDto {
  id: string | null;
  status: string | null;
  preapproval_plan_id: string | null;
  payer_id: number | null;
  payer_email: string | null;
  reason: string | null;
  date_created: string;
  last_modified: string;
  next_payment_date: string | null;
  auto_recurring: AutoRecurringDto | null; // Agora tipado corretamente
  payment_method_id: string | null;
}

// Baseado no LogErro.txt 
export interface SubscriptionDetailsDto {
  subscriptionId: string | null;
  planName: string | null;
  status: string | null;
  amount: number;
  lastFourCardDigits: string | null;
  nextBillingDate: string | null;
}

