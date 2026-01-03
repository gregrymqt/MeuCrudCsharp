import React from 'react';
import styles from '../../features/home/styles/Home.module.scss'; // Ajuste o caminho conforme sua estrutura

// Componentes
import { Services } from '../../features/home/components/Service/Services';
import { Hero } from '../../features/home/components/Hero/Hero';

// Hook
import { useHomeData } from '../../features/home/hooks/useHomeData';

export const Home: React.FC = () => {
  // CORREÇÃO: O hook retorna heroSlides, services e isLoading (não 'data' e 'loading')
  const { heroSlides, services, isLoading, error, refreshData } = useHomeData();

  // Loading State
  if (isLoading) {
    return (
      <div className={styles.loadingContainer}>
        <div className={styles.spinner}></div>
        <p>Carregando experiências...</p>
      </div>
    );
  }

  // Error State
  if (error) {
    return (
      <div className={styles.errorContainer}>
        <i className="fas fa-exclamation-triangle"></i>
        <p>Ops! {error}</p>
        <button onClick={() => refreshData()} className="btn btn-primary">
          Tentar Novamente
        </button>
      </div>
    );
  }

  // Empty State (opcional, caso não venha nada)
  if (!isLoading && heroSlides.length === 0 && services.length === 0) {
    return (
      <div className={styles.emptyState}>
        <p>O site está sendo atualizado. Volte em breve!</p>
      </div>
    );
  }

  return (
    <main className={styles.mainWrapper}>
      
      {/* Renderiza Hero apenas se tiver slides */}
      {heroSlides.length > 0 && (
        <Hero slides={heroSlides} />
      )}

      {/* Renderiza Services apenas se tiver itens */}
      {services.length > 0 && (
        <Services data={services} />
      )}

    </main>
  );
};