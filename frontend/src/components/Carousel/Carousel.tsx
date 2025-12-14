import { type ReactNode } from 'react';
import { Swiper, SwiperSlide } from 'swiper/react';
import { Navigation, Pagination, A11y, Autoplay } from 'swiper/modules';
import type { SwiperOptions } from 'swiper/types';

// Importação dos estilos padrão do Swiper
import 'swiper/css';
import 'swiper/css/navigation';
import 'swiper/css/pagination';
import './Carousel.scss';

// Interface Genérica
interface CarouselProps<T> {
  items: T[]; // Array de dados (ex: Course[], User[])
  
  // Função que diz como renderizar cada item
  renderItem: (item: T) => ReactNode; 
  
  // Função para pegar a chave única (id) de cada item
  keyExtractor: (item: T) => string | number;

  // Opções extras para sobrescrever o padrão
  options?: SwiperOptions;
  
  // Classe extra para o container
  className?: string;
}

// O <T,> diz ao React que este é um componente genérico
export const Carousel = <T,>({ 
  items, 
  renderItem, 
  keyExtractor, 
  options, 
  className = '' 
}: CarouselProps<T>) => {

  // Configuração Padrão (Mobile First)
  const defaultOptions: SwiperOptions = {
    modules: [Navigation, Pagination, A11y, Autoplay],
    spaceBetween: 20, // Espaço entre slides
    slidesPerView: 1, // Mobile: 1 item por vez
    navigation: true, // Setinhas
    pagination: { clickable: true }, // Bolinhas
    autoplay: {
      delay: 5000,
      disableOnInteraction: false,
    },
    // Breakpoints para responsividade
    breakpoints: {
      640: {
        slidesPerView: 2, // Tablet
        spaceBetween: 20,
      },
      992: { // Desktop (Baseado na sua variável)
        slidesPerView: 3,
        spaceBetween: 30,
      },
      1200: {
        slidesPerView: 4,
        spaceBetween: 30,
      }
    },
    ...options // Mescla com as opções passadas via prop (se houver)
  };

  if (!items || items.length === 0) return null;

  return (
    <div className={`generic-carousel-wrapper ${className}`}>
      <Swiper {...defaultOptions} className="custom-swiper">
        {items.map((item) => (
          <SwiperSlide key={keyExtractor(item)}>
            {/* Renderiza o conteúdo customizado aqui */}
            {renderItem(item)}
          </SwiperSlide>
        ))}
      </Swiper>
    </div>
  );
};