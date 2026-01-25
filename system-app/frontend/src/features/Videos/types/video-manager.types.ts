import type { Video, Course } from 'src/types/models'; //

// As abas da Sidebar
export type VideoTab = 'list' | 'form' | 'player';

// Dados do Formulário (compatível com GenericForm)
export interface VideoFormData {
  title: string;
  description: string;
  courseId: string;
  videoUrl: string;     // URL externa (Youtube/Vimeo)
  duration: string;
  thumbnail?: FileList; // Capa (Opcional)
  
  // [CORREÇÃO] Adicionamos o campo videoFile aqui para remover o erro 'any'
  videoFile?: FileList; // Arquivo de vídeo para upload
}

// Props da Lista
export interface VideoListProps {
  videos: Video[];
  isLoading: boolean;
  onEdit: (video: Video) => void;
  onDelete: (publicId: string) => void;
  onWatch: (video: Video) => void; // Ação para ir à aba 'player'
  onNewClick: () => void;
  onRefresh : () => void;
}

// Props do Formulário
export interface VideoFormProps {
  initialData?: Video | null;
  courses: Course[]; // Lista de cursos para o Select
  isLoading: boolean;
  onSubmit: (data: VideoFormData) => Promise<void>;
  onCancel: () => void;
}

// Props do Player (Aba Visualizar Vídeo)
export interface VideoPlayerProps {
  video: Video | null;
  onBack: () => void;
}

export interface Videos {
  id: string; // GUID (PublicId no backend)
  title: string;
  description: string;
  storageIdentifier: string;
  thumbnailUrl: string;
  status: 'Processing' | 'Available' | 'Error'; // String conforme o DTO
  courseName?: string;
  uploadDate: string;
  duration?: string;
}

// Interface Genérica de Paginação (igual ao PaginatedResultDto do C#)
export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// Dados para CRIAR vídeo (Formulário com Arquivos)
// Baseado nos parâmetros do CreateVideo no Controller
export interface CreateVideoParams {
  title: string;
  description: string;
  courseId: string; // Necessário para vincular, mesmo que o snippet C# precise de ajuste
  videoFile: File;  // IFormFile obrigatório
  thumbnailFile?: File; // IFormFile opcional
}

// Dados para ATUALIZAR vídeo
// Baseado no UpdateVideoDto
export interface UpdateVideoParams {
  title: string;
  description: string;
  thumbnailFile?: File; // Opcional
}

// Filtros de listagem
export interface VideoFilters {
  page: number;
  pageSize: number;
}