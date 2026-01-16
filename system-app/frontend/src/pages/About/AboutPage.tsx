import React, { useEffect } from "react";

// Estilos
import styles from "./styles/AboutPage.module.scss";

// Componentes
import { AboutHeroSection } from "../../features/about/components/Section/AboutHeroSection";
import { AboutTeamSection } from "../../features/about/components/Members/AboutTeamSection";

// Hook (CORREÇÃO: Usar o hook de leitura criado anteriormente)
import { useAboutData } from "../../features/about/hooks/useAboutData";

// Tipos (CORREÇÃO: Importar, não redefinir)
import type { AboutTeamData } from "../../features/about/types/about.types";

export const AboutPage: React.FC = () => {
  // CORREÇÃO: Usando o hook correto que retorna sections e teamMembers separados
  const { sections, teamMembers, isLoading, refreshData } = useAboutData();

  // Busca os dados ao montar a página
  useEffect(() => {
    refreshData();
  }, [refreshData]);

  // Loading UI
  if (isLoading) {
    return (
      <div className={styles.loadingContainer}>
        <p>Carregando nossa história...</p>
      </div>
    );
  }

  // Empty UI (Verifica se ambos estão vazios)
  if (!isLoading && sections.length === 0 && teamMembers.length === 0) {
    return (
      <div className={styles.emptyState}>
        <p>Nenhuma informação disponível no momento.</p>
      </div>
    );
  }

  // Preparar os dados para o componente de Team
  // Como o hook retorna apenas a lista de membros, montamos o objeto que o componente espera
  const teamSectionData: AboutTeamData = {
    id: "team-section",
    contentType: "section2",
    title: "Nossa Equipe", // Título padrão ou vindo de config
    description: "Conheça as pessoas que fazem a diferença.",
    members: teamMembers,
  };

  return (
    <main className={styles.pageContainer}>
      {/* 1. RENDERIZA AS SEÇÕES DE CONTEÚDO (Hero/Texto) */}
      {sections.map((section) => (
        <div key={section.id} className={styles.sectionWrapper}>
          <AboutHeroSection data={section} />
        </div>
      ))}

      {/* 2. RENDERIZA A SEÇÃO DA EQUIPE (Se houver membros) */}
      {teamMembers.length > 0 && (
        <div className={styles.sectionWrapper}>
          <AboutTeamSection data={teamSectionData} />
        </div>
      )}
    </main>
  );
};
