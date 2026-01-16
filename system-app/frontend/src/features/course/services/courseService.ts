import { ApiService } from '../../../shared/services/api.service'; // [cite: 1]
import type { PaginatedResponse, CourseDto, CourseRowUI, VideoCardUI } from '../types/course.type';


// Helper simples para formatar duração (pode mover para utils)
const formatDuration = (totalSeconds: number): string => {
  if (!totalSeconds) return '';
  const minutes = Math.floor(totalSeconds / 60);
  const hours = Math.floor(minutes / 60);
  if (hours > 0) return `${hours}h ${minutes % 60}m`;
  return `${minutes}m`;
};

export const courseService = {
  /**
   * Busca cursos paginados e já retorna formatado para a UI.
   * Endpoint: api/public/courses/paginated 
   */
  getAllPaginated: async (pageNumber: number, pageSize: number = 5) => {
    // A ApiService já tem a BASE_URL, passamos apenas o endpoint relativo
    const endpoint = `/public/courses/paginated?pageNumber=${pageNumber}&pageSize=${pageSize}`;

    // Chamada à API usando o método genérico 'get' 
    const response = await ApiService.get<PaginatedResponse<CourseDto>>(endpoint);

    // --- MAPPING (Adapter Pattern) ---
    // Transforma DTO (Backend) em UI Model (Frontend)
    const uiData: CourseRowUI[] = response.items.map((course) => ({
      id: course.id,
      categoryName: course.name,
      videos: course.videos.map((video): VideoCardUI => ({
        id: video.publicId,
        title: video.title,
        thumbnailUrl: video.thumbnailUrl || '/assets/placeholder.jpg',
        durationFormatted: formatDuration(video.duration?.totalSeconds || 0),
        courseId: course.id,
        isNew: false // Lógica futura se houver campo 'UploadDate' recente
      }))
    }));

    return {
      data: uiData,
      totalCount: response.totalCount,
      currentPage: response.page,
      totalPages: Math.ceil(response.totalCount / response.pageSize)
    };
  }
};