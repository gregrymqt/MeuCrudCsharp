import { useState, useEffect, useRef, useCallback } from 'react';
import { courseService } from '../services/courseService';
import type { CourseRowUI } from '../types/course.type';
import { ApiError } from '../../../shared/services/api.service';

export const useCourses = (pageSize: number = 5) => {
  const [courses, setCourses] = useState<CourseRowUI[]>([]);
  const [page, setPage] = useState(1);
  const [isLoading, setIsLoading] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Ref para o elemento HTML que dispara o scroll infinito
  const sentinelRef = useRef<HTMLDivElement>(null);

  // Função que busca dados (Memoizada)
  const fetchCourses = useCallback(async (pageNum: number, isRefresh = false) => {
    setIsLoading(true);
    setError(null);
    try {
      const { data, totalPages } = await courseService.getAllPaginated(pageNum, pageSize);
      
      setCourses(prev => isRefresh ? data : [...prev, ...data]);
      setHasMore(pageNum < totalPages);
    } catch (err) {
      if(err instanceof ApiError){
      setError('Erro ao carregar conteúdo.');
      }
    } finally {
      setIsLoading(false);
    }
  }, [pageSize]);

  // 1. Carga Inicial
  useEffect(() => {
    fetchCourses(1, true);
  }, [fetchCourses]);

  // 2. Lógica do Observer (Infinite Scroll)
  useEffect(() => {
    const element = sentinelRef.current;
    if (!element) return;

    const observer = new IntersectionObserver((entries) => {
      // Se a div sentinela apareceu E tem mais páginas E não está carregando...
      if (entries[0].isIntersecting && hasMore && !isLoading) {
        const nextPage = page + 1;
        setPage(nextPage);
        fetchCourses(nextPage);
      }
    }, { threshold: 0.5 }); // Dispara quando 50% da sentinela aparecer

    observer.observe(element);
    return () => observer.disconnect();
  }, [hasMore, isLoading, page, fetchCourses]);

  // Expondo apenas o necessário para a UI
  return {
    courses,      // Os dados para as linhas
    isLoading,    // Para mostrar skeletons/spinners
    error,        // Para mostrar mensagem de erro
    refresh: () => fetchCourses(1, true), // Para o botão "Tentar Novamente"
    sentinelRef   // Para ligar na DIV do final da página
  };
};