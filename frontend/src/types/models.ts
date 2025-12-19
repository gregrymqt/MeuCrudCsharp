// ==========================================
// ENUMS (Convertidos para Constantes para evitar erro do Vite)
// ==========================================

// Roles são strings no Identity
export const AppRoles = {
  Admin: "Admin",
  User: "User",
  Manager: "Manager"
} as const;
export type AppRoles = typeof AppRoles[keyof typeof AppRoles];

// ChargebackStatus são inteiros (0, 1, 2...)
export const ChargebackStatus = {
  Novo: 0,
  AguardandoEvidencias: 1,
  EvidenciasEnviadas: 2,
  Ganhamos: 3,
  Perdemos: 4
} as const;
export type ChargebackStatus = typeof ChargebackStatus[keyof typeof ChargebackStatus];

export const ClaimStatus = {
  Novo: 0,
  EmAnalise: 1,
  RespondidoPeloVendedor:2,
  ResolvidoGanhamos: 3,
  ResolvidoPerdemos: 4
} as const;
export type ClaimStatus = typeof ClaimStatus[keyof typeof ClaimStatus];

export const PlanFrequencyType = {
  Days: 0,
  Months: 1
} as const;
export type PlanFrequencyType = typeof PlanFrequencyType[keyof typeof PlanFrequencyType];

export const VideoStatus = {
  Processing: 0,
  Available: 1,
  Error: 2
} as const;
export type VideoStatus = typeof VideoStatus[keyof typeof VideoStatus];

// ==========================================
// INTERFACES
// ==========================================

export interface IdentityUser {
  userName?: string;
  normalizedUserName?: string;
  email?: string;
  normalizedEmail?: string;
  emailConfirmed: boolean;
  phoneNumber?: string;
  phoneNumberConfirmed: boolean;
  twoFactorEnabled: boolean;
}

export interface User extends IdentityUser {
  name?: string;
  avatarUrl?: string;
  createdAt: string; // Vem como ISO String ("2025-12-12T...")
  googleId?: string;
  customerId?: string;
  
  subscription?: Subscription;
  payments?: Payment[];
}

export interface TransactionBase {
  id: string;
  externalId?: string;
  userId: string;
  user?: User;
  status?: string;
  payerEmail?: string;
  createdAt: string;
  updatedAt?: string;
  paymentId?: string;
  amount: number;
}

export interface Plan {
  id: number;
  publicId: string;
  externalPlanId: string;
  name: string;
  description?: string;
  transactionAmount: number;
  currencyId: string;
  frequencyInterval: number;
  frequencyType: PlanFrequencyType;
  isActive: boolean;
}

export interface Subscription extends TransactionBase {
  plan?: Plan;
  planPublicId: string;
  lastFourCardDigits?: string;
  currentPeriodStartDate: string;
  currentPeriodEndDate: string;
  paymentMethodId?: string;
  cardTokenId?: string;
  nextBillingDate?: string;
}

export interface Payment extends TransactionBase {
  method?: string;
  installments: number;
  dateApproved?: string;
  lastFourDigits?: string;
  customerCpf?: string;
  subscriptionId: string;
  subscription?: Subscription;
}

export interface Chargeback {
  id: number;
  chargebackId: number;
  paymentId: number;
  userId?: string;
  user?: User;
  status: ChargebackStatus;
  amount: number;
  createdAt: string;
  internalNotes?: string;
}

export interface Claim {
  id: number;
  notificationId: number;
  claimId?: string;
  type?: string;
  dataCreated: string;
  status: ClaimStatus;
  internalNotes?: string;
  mercadoPagoClaimUrl?: string;
  typePayment?: string;
  userId?: string;
  user?: User;
}

export interface Course {
  id: number;
  publicId: string;
  name: string;
  description: string;
  videos?: Video[];
}

export interface Video {
  id: string;
  publicId: string;
  title: string;
  description: string;
  storageIdentifier: string;
  uploadDate: string;
  duration: string;
  status: VideoStatus;
  courseId: number;
  course?: Course;
  thumbnailUrl?: string;
}

export interface MercadoPagoNotification {
  action?: string;
  api_version?: string;
  // CORREÇÃO DO ERRO DE 'ANY': Usamos unknown ou Record para objetos dinâmicos desconhecidos
  data?: Record<string, unknown> | null; 
  date_created: string;
  id: number;
  live_mode: boolean;
  type?: string;
  user_id: string;
}