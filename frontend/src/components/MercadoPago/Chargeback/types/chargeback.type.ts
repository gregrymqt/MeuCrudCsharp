// ==========================================
// Enums (Espelho do C# ChargebackStatus)
// ==========================================
export const ChargebackStatusEnum = {
  Novo: 0,
  AguardandoEvidencias: 1,
  EvidenciasEnviadas: 2,
  Ganhamos: 3,
  Perdemos: 4
} as const;

export type ChargebackStatusType = typeof ChargebackStatusEnum[keyof typeof ChargebackStatusEnum];

// Helper para traduzir o status numérico para texto no front (opcional, se o back não mandar a string)
export const getChargebackStatusLabel = (status: number) => {
  switch (status) {
    case ChargebackStatusEnum.Novo: return "Novo";
    case ChargebackStatusEnum.AguardandoEvidencias: return "Aguardando Evidências";
    case ChargebackStatusEnum.EvidenciasEnviadas: return "Evidências Enviadas";
    case ChargebackStatusEnum.Ganhamos: return "Ganhamos";
    case ChargebackStatusEnum.Perdemos: return "Perdemos";
    default: return "Desconhecido";
  }
};

// ==========================================
// Interfaces de Lista (ChargebacksIndexViewModel)
// ==========================================

export interface ChargebackSummary {
  id: string;             // Mapeia ViewModel.Id
  customer: string;       // Mapeia ViewModel.Customer
  amount: number;         // Mapeia ViewModel.Amount
  date: string;           // Mapeia ViewModel.Date (ISO Date String)
  status: string;         // Mapeia ViewModel.Status (Texto formatado)
  mercadoPagoUrl: string; // Mapeia ViewModel.MercadoPagoUrl
}

export interface ChargebackPaginatedResponse {
  chargebacks: ChargebackSummary[];
  searchTerm?: string;
  statusFilter?: string;
  currentPage: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

// ==========================================
// Interfaces de Detalhes (ChargebackDetailViewModel)
// ==========================================

export interface ChargebackFile {
  tipo: string;        // Mapeia ChargebackFileViewModel.Tipo
  url: string;         // Mapeia ChargebackFileViewModel.Url
  nomeArquivo: string; // Mapeia ChargebackFileViewModel.NomeArquivo
}

export interface ChargebackDetail {
  chargebackId: string;        // Mapeia ChargebackDetailViewModel.ChargebackId
  valor: number;               // Mapeia ChargebackDetailViewModel.Valor
  moeda: string;               // Mapeia ChargebackDetailViewModel.Moeda
  statusDocumentacao: string;  // Mapeia ChargebackDetailViewModel.StatusDocumentacao
  coberturaAplicada: boolean;  // Mapeia ChargebackDetailViewModel.CoberturaAplicada
  precisaDocumentacao: boolean;// Mapeia ChargebackDetailViewModel.PrecisaDocumentacao
  dataLimiteDisputa: string | null; // Mapeia DateTime? (pode ser null)
  dataCriacao: string;         // Mapeia DateTime
  arquivosEnviados: ChargebackFile[]; // Mapeia List<ChargebackFileViewModel>
}