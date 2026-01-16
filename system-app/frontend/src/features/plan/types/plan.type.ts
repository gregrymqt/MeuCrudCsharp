// 1. O que vem da API (Espelho do C# PlanDto)
export interface PlanDto {
  id: number;
  name: string;
  description: string; // Geralmente vem como texto único ou separado por pipes/quebras
  transactionAmount: number; // O C# usa decimal/double [cite: 20]
  interval: string; // Ex: 'Monthly', 'Annual'
  isActive: boolean;
}

// 2. Estrutura de Paginação Genérica (Espelho do C# PagedResultDto) [cite: 6]
export interface PagedResultDto<T> {
  items: T[];           // [cite: 7]
  currentPage: number;  // [cite: 8]
  pageSize: number;     // [cite: 8]
  totalCount: number;   // [cite: 9]
  totalPages: number;   // [cite: 9]
  hasPreviousPage: boolean; // [cite: 10]
  hasNextPage: boolean;     // [cite: 11]
}

// 3. O que o seu Componente React consome (Já definido anteriormente)
export interface PlanUI {
  id: number;
  slug: string;
  name: string;
  
  // Visual
  priceMain: string;
  priceCents: string;
  frequencyLabel: string;
  
  features: string[];
  isRecommended: boolean;
  
  // Ação
  buttonAction: string;
  buttonText: string;
}