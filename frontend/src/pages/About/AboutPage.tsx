import React, { useEffect } from 'react';


// Estilos
import styles from './AboutPage.module.scss';
import { AboutHeroSection } from '../../features/about/components/AboutHeroSection';
import { AboutTeamSection } from '../../features/about/components/AboutTeamSection';
import { useAbout } from '../../features/about/hooks/useAbout';
import type { AboutSectionData, AboutTeamData } from '../../features/about/types/about.types';

export const AboutPage: React.FC = () => {
    const { sections, isLoading, fetchSections } = useAbout();

    // Busca os dados ao montar a página
    useEffect(() => {
        fetchSections();
    }, [fetchSections]);

    // Loading UI
    if (isLoading) {
        return (
            <div className={styles.loadingContainer}>
                <p>Carregando nossa história...</p>
            </div>
        );
    }

    // Empty UI
    if (!isLoading && sections.length === 0) {
        return (
            <div className={styles.emptyState}>
                <p>Nenhuma informação disponível no momento.</p>
            </div>
        );
    }

    return (
        <main className={styles.pageContainer}>
            {sections.map((section, index) => {
                // Wrapper para garantir estrutura e separação no HTML
                return (
                    <div key={section.id || index} className={styles.sectionWrapper}>

                        {/* RENDERIZAÇÃO CONDICIONAL DA SEÇÃO 1 (TEXTO + IMG) */}
                        {section.contentType === 'section1' && (
                            <AboutHeroSection data={section as AboutSectionData} />
                        )}

                        {/* RENDERIZAÇÃO CONDICIONAL DA SEÇÃO 2 (TIME / GRID) */}
                        {section.contentType === 'section2' && (
                            <AboutTeamSection data={section as AboutTeamData} />
                        )}

                    </div>
                );
            })}
        </main>
    );
};