import { ApiService } from '../../../shared/services/api.service'; // 
import type { PlanDto, PlanUI, PagedResultDto } from '../types/plan.type';

const ENDPOINT = '/public/plans'; // [cite: 1]

export const planService = {
  /**
   * Busca os planos paginados e mapeia para o formato de UI
   */
  getAllPaginated: async (page: number, pageSize: number): Promise<PagedResultDto<PlanUI>> => {
    // 1. Chamada à API usando seu Service Genérico
    // Passamos os parâmetros via Query String conforme o Controller C# espera [cite: 1]
    const response = await ApiService.get<PagedResultDto<PlanDto>>(
      `${ENDPOINT}?page=${page}&pageSize=${pageSize}`
    );

    // 2. Mapeamento de DTO -> UI
    const mappedItems = response.items.map(mapDtoToUi);

    // 3. Retorna estrutura mantendo os metadados de paginação [cite: 15]
    return {
      ...response,
      items: mappedItems
    };
  }
};

/**
 * Função Pura de Mapeamento (Regra de Negócio Visual)
 */
function mapDtoToUi(dto: PlanDto): PlanUI {
  const isAnnual = dto.name.toLowerCase().includes('anual') || dto.interval === 'Annual';
  
  // Formatação de Preço (Ex: 49.90 -> "49" e ",90")
  const priceParts = dto.transactionAmount.toFixed(2).split('.');
  
  return {
    id: dto.id,
    slug: dto.name.toLowerCase().replace(/\s+/g, '-'), // Ex: "Plano Anual" -> "plano-anual"
    name: dto.name,
    
    // Formatação Visual
    priceMain: priceParts[0],
    priceCents: `,${priceParts[1]}`,
    frequencyLabel: isAnnual ? '/ano' : '/mês',
    
    // Tratamento da Descrição (Assumindo que no banco vem separado por quebra de linha ou pipe)
    features: dto.description ? dto.description.split('\n').filter(f => f) : [],
    
    // Lógica de Recomendado (Geralmente o Anual é o destaque)
    isRecommended: isAnnual,

    // Lógica de Redirecionamento (Baseada no seu legado)
    // Anual -> Direto pro Cartão | Mensal -> Escolher Pagamento
    buttonAction: isAnnual 
      ? `/checkout/credit-card?planId=${dto.id}` 
      : `/checkout/method?planId=${dto.id}`,
      
    buttonText: isAnnual ? 'Assinar Agora' : 'Escolher Pagamento'
  };
}