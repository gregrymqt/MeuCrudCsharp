// types/plans.ts

// Interface genérica para paginação (Match com PagedResultDto do C#)
export interface PagedResult<T> {
  items: T[];          // 
  currentPage: number; // [cite: 42]
  pageSize: number;    // [cite: 43]
  totalCount: number;  // [cite: 44]
  totalPages: number;
  hasPreviousPage: boolean; // [cite: 45]
  hasNextPage: boolean;     // [cite: 46]
}

// --- DTOs de LEITURA (O que vem do backend para exibir) ---

// Usado na listagem principal (Grid)
export interface PlanSummary {
  publicId: string;
  name: string;
  slug: string;
  priceDisplay: string;
  billingInfo: string;
  features: string[];
  isRecommended: boolean;
  isActive: boolean;
  // Baseado no PlanDto do C# [cite: 36]
}

// Usado para preencher o formulário de Edição
export interface PlanEditDetail {
  publicId: string;
  name: string;
  transactionAmount: number;
  frequency: number;
  frequencyType: string;
  description?: string; 
}

// --- DTOs de ESCRITA (O que enviamos para o backend) ---

// Sub-objeto para recorrência (usado no Create/Update)
export interface AutoRecurringRequest {
  frequency: number;            // [cite: 34]
  frequency_type: string;       // Nota: snake_case pois o DTO C# usa JsonPropertyName [cite: 34]
  transaction_amount: number;   // [cite: 34]
  currency_id: string;          // [cite: 34]
}

// Payload para CRIAR um plano
export interface CreatePlanRequest {
  reason: string;               // [cite: 35]
  auto_recurring: AutoRecurringRequest; // [cite: 35]
  description: string;          // [cite: 35]
}

// Payload para ATUALIZAR um plano
export interface UpdatePlanRequest {
  reason?: string;              // [cite: 40]
  auto_recurring?: AutoRecurringRequest; // [cite: 40]
}