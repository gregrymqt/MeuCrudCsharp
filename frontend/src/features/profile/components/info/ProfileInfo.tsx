import React, { useState } from 'react';
import styles from './ProfileInfo.module.scss';
import { Card } from '../../../../components/Card/Card';
import { useAuth } from '../../../auth/hooks/useAuth';
import type { SubscriptionDetails } from '../../types/profile.types';
import { AvatarUploadForm } from './AvatarUploadForm'; // Componente criado acima

interface ProfileInfoProps {
  subscription?: SubscriptionDetails | null;
}

type TabOption = 'details' | 'avatar';

export const ProfileInfo: React.FC<ProfileInfoProps> = ({ subscription }) => {
  const { user } = useAuth();
  const [activeTab, setActiveTab] = useState<TabOption>('details');

  if (!user) return null;

  const planName = subscription?.planName || 'Gratuito';
  const isPlanActive = subscription?.status === 'active' || subscription?.status === 'authorized';
  const statusLabel = isPlanActive ? 'Ativo' : 'Inativo';

  return (
    <Card className={styles.card}>
      {/* Abas de Navegação */}
      <div className={styles.tabs}>
        <button 
          className={`${styles.tabBtn} ${activeTab === 'details' ? styles.active : ''}`}
          onClick={() => setActiveTab('details')}
        >
          <i className="fas fa-id-card me-2"></i> Dados
        </button>
        <button 
          className={`${styles.tabBtn} ${activeTab === 'avatar' ? styles.active : ''}`}
          onClick={() => setActiveTab('avatar')}
        >
          <i className="fas fa-camera me-2"></i> Foto
        </button>
      </div>

      <Card.Body title="">
        {/* CONTEÚDO DA ABA: DADOS (Visualização Original) */}
        {activeTab === 'details' && (
          <div className="text-center fade-in">
            <img 
              src={user.avatarUrl || '/default-user.png'} 
              alt={`Foto de ${user.name}`} 
              className={styles.avatar} 
            />
            
            <h2>{user.name}</h2>
            <p className="text-muted">{user.email}</p>

            <div className={`${styles.badge} ${isPlanActive ? styles.active : styles.inactive}`}>
              Plano: {planName} ({statusLabel})
            </div>
            
            <div className="mt-3">
               <button className="btn btn-primary w-100">
                  <i className="fas fa-graduation-cap me-2"></i> Acessar Cursos
               </button>
            </div>
          </div>
        )}

        {/* CONTEÚDO DA ABA: FOTO (Formulário Novo) */}
        {activeTab === 'avatar' && (
          <div className="fade-in">
            <div className="text-center mb-3">
              <img 
                src={user.avatarUrl || '/default-user.png'} 
                alt="Preview" 
                className={`${styles.avatar} ${styles.avatarSmall}`} 
              />
            </div>
            <AvatarUploadForm />
          </div>
        )}
      </Card.Body>
    </Card>
  );
};