export interface PaymentItems {
  id: string;
  amount: number;
  status: string;
  createdAt: string; // O JSON retorna data como string ISO
  description?: string;
  paymentMethod?: string;
}