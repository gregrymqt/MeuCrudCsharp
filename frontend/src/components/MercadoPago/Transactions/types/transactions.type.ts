import type { Payment } from "../../../../types/models";

export interface PaymentItems extends Pick<Payment, 'id' | 'amount' | 'createdAt'> {
  status: string; // Ou mapear para um Enum se tiver PaymentStatus global
  description?: string;
}