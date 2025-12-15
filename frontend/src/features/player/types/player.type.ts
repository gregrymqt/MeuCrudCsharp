export interface PlayerUI {
  id: string;           // PublicId para buscar o stream
  title: string;
  description: string;
  thumbnailUrl?: string;
  durationFormatted: string; // Ex: "12:30"
  courseTitle?: string;      // "Curso de React > Módulo 1"
  streamUrl?: string;        // URL do .m3u8 (opcional por enquanto, foco visual)
}

export interface VideoDto {
  id: number;
  publicId: string;        // GUID usado para navegação
  title: string;
  description?: string;
  thumbnailUrl?: string;
  duration: string;        // TimeSpan vem como string do JSON
  storageIdentifier: string; // Essencial para montar a URL do .m3u8 [cite: 37]
  courseId: number;
  // Adicionei campos opcionais para UI
  courseTitle?: string; 
}