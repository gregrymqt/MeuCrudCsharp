// src/features/support/services/support.service.ts

import { ApiService } from '../../../shared/services/api.service'; // Ajuste o caminho se necessário
import type { 
  SupportTicket, 
  CreateSupportTicketPayload, 
  SupportTicketStatus, 
  UpdateSupportTicketPayload,
  SupportApiResponse,
  PaginatedResult // Importe o novo type
} from '../types/support.types';

export const SupportService = {
  createTicket: async (payload: CreateSupportTicketPayload): Promise<SupportApiResponse<void>> => {
    return await ApiService.post<SupportApiResponse<void>>('/support', payload);
  },

  // --- CORREÇÃO AQUI ---
  // 1. Agora aceita page e pageSize
  // 2. O retorno agora é PaginatedResult<SupportTicket> e não SupportTicket[]
  getAllTickets: async (page: number, pageSize: number): Promise<SupportApiResponse<PaginatedResult<SupportTicket>>> => {
    // Passamos via Query String
    return await ApiService.get<SupportApiResponse<PaginatedResult<SupportTicket>>>(
      `/support?page=${page}&pageSize=${pageSize}`
    );
  },

  getTicketById: async (ticketId: string): Promise<SupportApiResponse<SupportTicket>> => {
    return await ApiService.get<SupportApiResponse<SupportTicket>>(`/support/${ticketId}`);
  },

  updateTicketStatus: async (ticketId: string, status: SupportTicketStatus): Promise<SupportApiResponse<void>> => {
    const payload: UpdateSupportTicketPayload = { status };
    return await ApiService.put<SupportApiResponse<void>>(`/support/${ticketId}`, payload);
  }
};