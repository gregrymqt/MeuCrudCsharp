import React from 'react';
import { useAuth } from '../../../auth/hooks/useAuth'; // Ajuste o import conforme sua estrutura
import styles from '../styles/ProfileInfo.module.scss'; // Estilo local
import { Card } from '../../../../components/Card/Card';

export const ProfileInfo: React.FC = () => {
  const { user } = useAuth(); //  Pega dados do contexto

  if (!user) return null;

  return (
    <Card className={styles.profileCard}>
      <div className={styles.header}>
        {/* Foto ou Avatar padrão */}
        <div className={styles.avatarContainer}>
            {user.avatarUrl ? (
                <img src={user.avatarUrl} alt={user.name} />
            ) : (
                <div className={styles.placeholderAvatar}>
                    {user.name?.charAt(0).toUpperCase()}
                </div>
            )}
        </div>
        <div className={styles.titles}>
            <h3>{user.name || 'Administrador'}</h3>
            <span className={styles.roleBadge}>{user.roles || 'Admin'}</span>
        </div>
      </div>

      <div className={styles.details}>
        <div className={styles.item}>
            <label>Email:</label>
            <span>{user.email}</span>
        </div>
        <div className={styles.item}>
            <label>ID do Sistema:</label>
            <span className={styles.code}>{user.googleId || 'N/A'}</span>
        </div>
        <div className={styles.item}>
            <label>Membro desde:</label>
            <span>{user.createdAt ? new Date(user.createdAt).toLocaleDateString('pt-BR') : 'N/A'}</span>
        </div>
      </div>
    </Card>
  );
};