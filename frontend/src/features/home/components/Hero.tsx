import React from 'react';
import styles from './Home.module.scss';
import { Carousel } from '../../../components/Carousel/Carousel';
import { useScrollOpacity } from '../hooks/useScrollOpacity';
import type { HeroSlideData } from '../types/home.types';

interface HeroProps {
  slides: HeroSlideData[];
}

export const Hero: React.FC<HeroProps> = ({ slides }) => {
  const opacity = useScrollOpacity(80); // 80vh height

  if (!slides || slides.length === 0) return null;

  return (
    <section className={styles.heroSection} style={{ opacity }}>
      {/* CORREÇÃO 1: Passamos o Generic <HeroSlideData> para o componente saber o tipo dos itens.
         CORREÇÃO 2: Adicionamos keyExtractor (obrigatório).
         CORREÇÃO 3: Movemos slidesPerView para dentro de 'options'.
      */}
      <Carousel<HeroSlideData>
        items={slides}
        keyExtractor={(item) => item.id}
        options={{
          slidesPerView: 1
        }}
        renderItem={(slide) => (
          <div 
            className="swiper-slide" 
            style={{ 
              backgroundImage: `url(${slide.imageUrl})`,
              backgroundSize: 'cover',
              backgroundPosition: 'center',
              width: '100%',
              height: '100%'
            }}
          >
            <div className={styles.slideContent}>
              <h1>{slide.title}</h1>
              <p>{slide.subtitle}</p>
              <a href={slide.actionUrl} className="btn btn-primary">
                {slide.actionText}
              </a>
            </div>
          </div>
        )}
      />
    </section>
  );
};