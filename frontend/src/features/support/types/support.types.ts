// src/features/support/types/support.types.ts

export type SupportTicketStatus = 'Open' | 'InProgress' | 'Closed';

export interface SupportTicket {
  id: string;
  userId: string;
  context: string;
  explanation: string;
  status: SupportTicketStatus;
  createdAt: string;
}

export interface CreateSupportTicketPayload {
  context: string;
  explanation: string;
}

export interface UpdateSupportTicketPayload {
  status: SupportTicketStatus;
}

// Interface Genérica de Resposta da API
export interface SupportApiResponse<T> {
  success: boolean;
  message?: string;
  data?: T;
}

// --- PAGINAÇÃO ---
export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  currentPage: number;
  pageSize: number;
  totalPages: number;
}