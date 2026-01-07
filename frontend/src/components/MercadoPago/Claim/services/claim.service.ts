import { ApiService } from "../../../../shared/services/api.service";
import type {
  ClaimSummary,
  ClaimDetail,
  ReplyClaimPayload,
} from "../types/claims.type";

export const ClaimService = {
  // ============================================================
  // ÁREA DO ADMINISTRADOR
  // ============================================================

  admin: {
    // Lista paginada (Inbox)
    getAll: async (page = 1, searchTerm = "", statusFilter = "") => {
      const query = `?page=${page}&searchTerm=${searchTerm}&statusFilter=${statusFilter}`;
      return await ApiService.get<{
        claims: ClaimSummary[];
        totalPages: number;
      }>(`/admin/claims${query}`);
    },

    // Detalhes com Chat [cite: 2]
    getDetails: async (id: number) => {
      return await ApiService.get<ClaimDetail>(`/admin/claims/${id}`);
    },

    // Responder ao aluno [cite: 3]
    reply: async (internalId: number, message: string) => {
      const payload: ReplyClaimPayload = { internalId, message };
      return await ApiService.post("/admin/claims/reply", payload);
    },
  },

  // ============================================================
  // ÁREA DO USUÁRIO (ALUNO)
  // ============================================================

  user: {
    // Minhas Reclamações [cite: 4]
    getMyClaims: async () => {
      return await ApiService.get<ClaimSummary[]>("/user/claims");
    },

    // Ver Detalhes (Chat Pessoal) [cite: 4]
    getMyDetails: async (internalId: number) => {
      return await ApiService.get<ClaimDetail>(`/user/claims/${internalId}`);
    },

    // Responder à Loja [cite: 4]
    reply: async (internalId: number, message: string) => {
      // Agora o ID vai na URL, igual ao método do usuário
      const payload = { message };
      return await ApiService.post(
        `/admin/claims/${internalId}/reply`,
        payload
      );
    },

    // Pedir Mediação (Escalar) [cite: 4]
    requestMediation: async (internalId: number) => {
      return await ApiService.post(`/user/claims/${internalId}/mediation`, {});
    },
  },
};
