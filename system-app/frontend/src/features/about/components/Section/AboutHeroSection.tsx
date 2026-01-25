import React from 'react';
import styles from '../../styles/AboutHeroSection.module.scss';
import type { AboutSectionData } from '../../types/about.types';

interface AboutHeroSectionProps {
    data: AboutSectionData;
}

export const AboutHeroSection: React.FC<AboutHeroSectionProps> = ({ data }) => {
    // UI/UX: Verifica se há dados antes de renderizar para evitar quebras
    if (!data) return null;

    return (
        <section className={styles.container}>
            <div className={styles.contentWrapper}>
                {/* Lado Esquerdo: Texto */}
                <div className={styles.textContent}>
                    <h2 className={styles.title}>
                        {data.title}
                    </h2>
                    <div className={styles.description}>
                        <p>{data.description}</p>
                    </div>
                    
                    {/* Botão de ação opcional (CTA) - Bom para UX */}
                    <button className={styles.ctaButton}>
                        Saiba mais
                    </button>
                </div>

                {/* Lado Direito: Imagem */}
                <div className={styles.imageWrapper}>
                    <img 
                        src={data.imageUrl} 
                        alt={data.imageAlt || data.title} 
                        className={styles.image}
                        loading="lazy" // Performance: Lazy load nativo
                    />
                </div>
            </div>
        </section>
    );
};