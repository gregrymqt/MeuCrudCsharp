import { type Course, type Video } from '../../../../types/models'; // Importando do seu source [cite: 1]

// Define as abas de navegação da Sidebar interna
export type AdminTab = 'list' | 'form';

// Interface específica para o Formulário (Omitindo ID pois é gerado no back ou na edição)
// Extende Record<string, any> para satisfazer o GenericForm [cite: 12]
export interface CourseFormData {
  name: string;
  description: string;
  // Se houver upload de imagem de capa futuramente
  thumbnail?: FileList; 
}

// Props para os Sub-componentes
export interface CourseListProps {
  courses: Course[];
  isLoading: boolean;
  onEdit: (course: Course) => void;
  onDelete: (id: string) => void;
  onNewClick: () => void; // Para ir para a aba de formulário
}

export interface CourseFormProps {
  initialData?: Course | null; // Null se for criar, Objeto se for editar
  isLoading: boolean;
  onSubmit: (data: CourseFormData) => Promise<void>;
  onCancel: () => void;
}

// src/features/admin/courses/types/course.types.ts

// Interface baseada no 'CourseDto' do C# [cite: 73]
export interface CourseDto {
  publicId: string; // Guid no backend
  name: string;
  description: string;
  // Videos são opcionais na listagem, mas vêm no detalhe
  videos?: Video[]; 
}

// Interface para Paginação baseada no 'PaginatedResultDto' [cite: 48, 85]
export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

// Baseado no 'CreateCourseDto' [cite: 71]
export interface CreateCourseData {
  name: string;
  description?: string;
}

// Baseado no 'UpdateCourseDto' [cite: 69]
export interface UpdateCourseData {
  name: string;
  description?: string;
}

// Filtros para a listagem
export interface CourseFilters {
  pageNumber: number;
  pageSize: number;
  name?: string; // Para a busca [cite: 56]
}