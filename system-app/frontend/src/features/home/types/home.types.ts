// =================================================================
// 1. DADOS DE LEITURA (O que vem do C#)
// =================================================================

export interface HeroSlideData {
  id: number;
  imageUrl: string; 
  title: string;
  subtitle: string;
  actionText: string;
  actionUrl: string;
}

export interface ServiceData {
  id: number;
  iconClass: string;
  title: string;
  description: string;
  actionText: string;
  actionUrl: string;
}

// Resposta do endpoint GET /api/Home
export interface HomeContent {
  hero: HeroSlideData[];
  services: ServiceData[];
}

// =================================================================
// 2. DADOS DE ESCRITA (O que o Form envia)
// =================================================================

// HERO: Extende os dados base, remove ID e URL, adiciona FileList
export interface HeroFormValues extends Omit<HeroSlideData, 'id' | 'imageUrl'> {
  newImage?: FileList; // O input type="file" retorna isso
}

// SERVICE: Apenas remove o ID (não tem upload de arquivo)
export type ServiceFormValues = Omit<ServiceData, 'id'>