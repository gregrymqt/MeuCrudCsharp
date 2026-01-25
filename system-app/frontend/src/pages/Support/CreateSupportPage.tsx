import React from 'react';
import { useNavigate } from 'react-router-dom';
import styles from './styles/CreateSupportPage.module.scss';
import { SupportCreateForm } from '../../features/support/components/SupportCreateForm';

export const CreateSupportPage: React.FC = () => {
  const navigate = useNavigate();

  return (
    <main className={styles.pageContainer}>
      <div className={styles.contentWrapper}>
        {/* Botão de Voltar: Essencial para Mobile */}
        <button 
          onClick={() => navigate(-1)} 
          className={styles.backBtn}
          aria-label="Voltar para a página anterior"
        >
          <i className="fas fa-arrow-left"></i> Voltar
        </button>

        {/* O Formulário */}
        <SupportCreateForm />
      </div>
    </main>
  );
};