// features/Course/types.ts

// Modelo visual único para o Card de Vídeo
export interface VideoCardUI {
  id: string;              // Mapeado do publicId
  title: string;
  thumbnailUrl: string;
  durationFormatted: string; // Já formatado (ex: "12m") para não ter lógica no view
  courseId: number;
  isNew?: boolean;         // Opcional: para badge "Novo"
}

// Modelo visual para a Fileira (Categoria)
export interface CourseRowUI {
  id: number;
  categoryName: string;    // Nome do curso ou categoria
  videos: VideoCardUI[];
}

// Representação exata do que o Backend manda (Data Transfer Object)
export interface VideoDto {
  id: number;
  publicId: string;
  title: string;
  thumbnailUrl?: string;
  duration?: {
    totalSeconds: number;
  };
  courseId: number;
}

export interface CourseDto {
  id: number;
  name: string; // Mapeado de c.Name [cite: 18]
  videos: VideoDto[];
}

// Baseado no seu DTO de paginação 
export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}