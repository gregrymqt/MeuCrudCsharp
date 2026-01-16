import React from 'react';
import styles from './SupportAdminPage.module.scss';
import { SupportTicketList } from '../../features/support/components/SupportTicketList';

export const SupportAdminPage: React.FC = () => {
  return (
    <main className={styles.pageContainer}>
      {/* Cabeçalho da Página */}
      <header className={styles.pageHeader}>
        <div>
          <h1>
            <i className="fas fa-user-shield"></i> Painel de Suporte
          </h1>
          <p>Central de atendimento e gerenciamento de tickets.</p>
        </div>
        
        {/* Aqui você poderia colocar filtros globais ou breadcrumbs no futuro */}
      </header>

      {/* Área de Conteúdo Principal */}
      <section className={styles.contentArea}>
        <SupportTicketList />
      </section>
    </main>
  );
};