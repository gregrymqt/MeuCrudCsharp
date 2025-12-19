// src/types/claims.ts

import { type FieldValues } from "react-hook-form";


// --- ENTIDADES VISUAIS (VIEW MODELS) ---

// Resumo para a lista (Tabela)
export interface ClaimSummary {
  internalId: number;   // ID do banco
  mpClaimId: number;    // ID do Mercado Pago
  customerName?: string;
  resourceId?: string;
  type: string;
  status: string;       // String vinda do backend ou Enum
  dateCreated: string;
  isUrgent?: boolean;   // Lógica visual
}

// Detalhe completo com Chat
export interface ClaimDetail {
  internalId: number;
  mpClaimId: number;
  status: string;
  stage?: string;
  messages: ChatMessage[];
  canReply?: boolean;
}

// Mensagem individual do Chat
export interface ChatMessage {
  messageId: string;
  senderRole: string;   // 'complainant' | 'respondent' | 'mediator'
  content: string;
  dateCreated: string;
  attachments: string[]; // URLs
  isMe: boolean;        // Crucial para o layout (Direita/Esquerda)
}

// --- DTOs DE ENVIO (FORMULÁRIOS) ---

// Interface para o GenericForm (Hook Form)
export interface ReplyFormData extends FieldValues {
  message: string;
  attachments?: FileList; // GenericForm retorna FileList
}

// Payload JSON esperado pelo Backend (Admin)
export interface ReplyClaimPayload {
  internalId: number;
  message: string;
  // Nota: Backend atual espera JSON. Upload de arquivo requer endpoint separado ou FormData.
}

// Payload JSON esperado pelo Backend (User)
export interface ReplyUserPayload {
  message: string;
}