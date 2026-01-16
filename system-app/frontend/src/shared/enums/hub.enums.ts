export const AppHubs = {
  Payment: '/PaymentProcessingHub',
  Refund: '/RefundProcessingHub',
  Video: '/videoProcessingHub'
} as const;

export type AppHubs = typeof AppHubs[keyof typeof AppHubs];