// src/types/payment.types.ts

export type PlanFrequency = 'monthly' | 'quarterly' | 'semiannual' | 'annual';

export interface PlanDetails {
  id: string;
  name: string; // ex: "Plano Premium Anual"
  frequency: PlanFrequency;
  amount: number;
}

export interface PaymentLayoutProps {
  plan: PlanDetails; // O plano que o usuário escolheu anteriormente
  userParams?: {
    email: string;
    name: string;
  };
}

// Identificadores das abas da Sidebar
export type PaymentMethodId = 'pix' | 'credit-card';

export interface PaymentSocketMessage {
  message: string;      // "Pagamento processado com sucesso!", "A processar..."
  status: 'processing' | 'approved' | 'failed' | 'error';
  isCompleted: boolean; // Indica se o fluxo terminou
  paymentId?: string;
}