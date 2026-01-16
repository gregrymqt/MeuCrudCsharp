import React from 'react';
import styles from './CourseRow.module.scss';
import { VideoCard } from '../VideoCard/VideoCard';
import { Carousel } from '../../../../components/Carousel/Carousel';
import type { CourseRowUI, VideoCardUI } from '../../types/course.type';

interface CourseRowProps {
  data: CourseRowUI;
  onVideoClick: (video: VideoCardUI) => void;
}

export const CourseRow: React.FC<CourseRowProps> = ({ data, onVideoClick }) => {
  
  // Se não houver vídeos, não renderiza a fileira
  if (!data.videos || data.videos.length === 0) return null;

  return (
    <section className={styles.rowContainer}>
      <h3 className={styles.rowTitle}>{data.categoryName}</h3>
      
      <div className={styles.carouselWrapper}>
        <Carousel<VideoCardUI>
          items={data.videos}
          keyExtractor={(item) => item.id}
          // Renderização customizada usando nosso VideoCard específico
          renderItem={(video) => (
            <VideoCard 
              data={video} 
              onClick={onVideoClick} 
            />
          )}
          // Podemos sobrescrever opções do Swiper se necessário (opcional)
          options={{
             spaceBetween: 15,
             // Ajuste fino para o estilo Netflix (slides visíveis parcialmente)
             centeredSlides: false,
          }}
          className={styles.customCarousel}
        />
      </div>
    </section>
  );
};