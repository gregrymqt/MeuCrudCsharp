// types/home.types.ts

// 1. Enum para identificar o tipo de conteúdo vindo do banco
export const ContentType = {
  HERO: 'HERO',
  SERVICE: 'SERVICE', // ou FEATURES
  ABOUT: 'ABOUT'
} as const;

export type ContentType = typeof ContentType[keyof typeof ContentType];


// 2. Interface "Crua" (como os dados vêm do Backend C#/Java)
// Supomos que o backend retorne uma lista genérica de "ContentItems"
export interface RawContentItem {
  id: number;
  contentType: ContentType; // O discriminador
  title: string;
  subtitle?: string;     // Usado no Hero
  description?: string;  // Usado no Service/About
  imageUrl?: string;
  actionText?: string;   // Texto do botão
  actionUrl?: string;    // Link do botão
  iconClass?: string;    // Usado no Service
  metadata?: string;     // Campo extra (ex: lista de benefícios do About separados por vírgula)
}

// 3. Interfaces de UI (Já definidas anteriormente, mantidas aqui para referência)
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

export interface AboutData {
  id: number;
  imageUrl: string;
  title: string;
  description: string;
  benefits: string[];
}

// 4. O formato final que o Hook vai entregar para a View
export interface HomeContent {
  hero: HeroSlideData[];
  services: ServiceData[];
  about: AboutData | null;
}