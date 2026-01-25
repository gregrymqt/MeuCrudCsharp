import React, { useState } from 'react';

import styles from './AdminProfile.module.scss';
import { Sidebar } from '../../components/SideBar/components/Sidebar';
import type { SidebarItem } from '../../components/SideBar/types/sidebar.types';
import { AdminTerminal } from '../../features/profile/Admin/components/AdminTerminal';
import { ProfileInfo } from '../../features/profile/User/components/ProfileInfo';
// 1. IMPORT NOVO: Importamos a lista de tickets
import { SupportTicketList } from '../../features/support/components/SupportTicketList'; 

export const AdminProfile: React.FC = () => {
    const [activeTab, setActiveTab] = useState<string>('profile');

    // 2. CONFIGURAÇÃO: Adicionado o item de Suporte na Sidebar
    const sidebarItems: SidebarItem[] = [
        {
            id: 'profile',
            label: 'Meus Dados',
            icon: 'fas fa-user-circle' 
        },
        {
            id: 'terminal',
            label: 'Terminal',
            icon: 'fas fa-terminal'
        },
        {
            id: 'support', // Novo ID
            label: 'Suporte',
            icon: 'fas fa-headset' // Ícone de Headset apropriado
        },
    ];

    // Helper para o Título do Header (para não poluir o JSX)
    const getHeaderContent = () => {
        switch (activeTab) {
            case 'profile':
                return { title: 'Meu Perfil', sub: 'Gerencie suas informações pessoais.' };
            case 'terminal':
                return { title: 'Terminal Admin', sub: 'Central de comando para redirecionamento.' };
            case 'support':
                return { title: 'Central de Suporte', sub: 'Visualize e responda chamados dos usuários.' };
            default:
                return { title: '', sub: '' };
        }
    };

    const headerData = getHeaderContent();

    return (
        <div className={styles.dashboardLayout}>
            <Sidebar
                items={sidebarItems}
                activeItemId={activeTab}
                onItemClick={(item) => setActiveTab(item.id.toString())}
                logo={<h2 className={styles.logoText}>Greg Co.</h2>}
            >
                <div className={styles.sidebarFooter}>
                    <span className={styles.version}>v2.0.0</span>
                </div>
            </Sidebar>

            <main className={styles.mainContent}>
                <header className={styles.pageHeader}>
                    <h1>{headerData.title}</h1>
                    <p>{headerData.sub}</p>
                </header>

                <div className={styles.contentArea}>
                    {activeTab === 'profile' && (
                        <div className={styles.fadeEntry}>
                            <ProfileInfo />
                        </div>
                    )}

                    {activeTab === 'terminal' && (
                        <div className={styles.fadeEntry}>
                            <AdminTerminal />
                        </div>
                    )}

                    {/* 3. RENDERIZAÇÃO: Área do Suporte */}
                    {activeTab === 'support' && (
                        <div className={styles.fadeEntry}>
                            {/* Renderizamos apenas a Lista, pois o Layout (Sidebar/Header) já é deste componente */}
                            <SupportTicketList />
                        </div>
                    )}
                </div>
            </main>
        </div>
    );
};