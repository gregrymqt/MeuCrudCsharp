import React, { useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import styles from '../styles/CourseFeed.module.scss';
import { CourseRow } from '../components/CourseRow/CourseRow';
import { CourseSkeleton } from '../components/CourseSkeleton/CourseSkeleton';
import { useCourses } from '../hooks/useCourses';
import type { VideoCardUI } from '../types/course.type';



export const CourseFeed: React.FC = () => {
  const navigate = useNavigate();

  // 1. Consumindo o Hook:
  // O componente não sabe o que é paginação ou observer, 
  // ele só recebe os dados prontos e a referência 'sentinelRef' para o scroll.
  const { 
    courses, 
    isLoading, 
    error, 
    refresh,
    sentinelRef 
  } = useCourses(); 

  // 2. A única responsabilidade sobre o vídeo aqui é o redirecionamento
  const handleVideoClick = useCallback((video: VideoCardUI) => {
    // Apenas muda a rota. A feature 'Player' assumirá a partir daqui.
    navigate(`/player/${video.id}`);
  }, [navigate]);

  // Tratamento de Erro de Carga
  if (error && courses.length === 0) {
    return (
      <div className={styles.errorContainer}>
        <p>{error}</p>
        <button onClick={refresh} className={styles.retryBtn}>Tentar Novamente</button>
      </div>
    );
  }

  return (
    <main className={styles.feedContainer}>
      <h1 className={styles.pageTitle}>Catálogo de Cursos</h1>

      {/* Estado de Loading Inicial (Skeleton) */}
      {isLoading && courses.length === 0 ? (
        <CourseSkeleton />
      ) : (
        <div className={styles.rowsList}>
          {/* Renderiza as Categorias (Rows) */}
          {courses.map((category) => (
            <CourseRow 
              key={category.id} 
              data={category} 
              onVideoClick={handleVideoClick} 
            />
          ))}
        </div>
      )}

      {/* Sentinela do Infinite Scroll 
          Quando essa div invisível entra na tela, o Hook carrega mais. */}
      <div ref={sentinelRef} className={styles.infiniteScrollTrigger}>
        {isLoading && courses.length > 0 && (
           <span className={styles.loadingMore}>Carregando mais cursos...</span>
        )}
      </div>
    </main>
  );
};