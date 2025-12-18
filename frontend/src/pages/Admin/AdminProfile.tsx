import React, { useState } from 'react';

import styles from './AdminProfile.module.scss';
import { Sidebar } from '../../components/SideBar/components/Sidebar';
import type { SidebarItem } from '../../components/SideBar/types/sidebar.types';
import { AdminTerminal } from '../../features/admin/Profile/components/AdminTerminal/AdminTerminal';
import { ProfileInfo } from '../../features/profile/components/info/ProfileInfo';


export const AdminProfile: React.FC = () => {
    // 1. Estado para controlar qual aba está ativa (padrão: 'profile')
    const [activeTab, setActiveTab] = useState<string>('profile');

    // 2. Definição dos itens da Sidebar conforme seu pedido
    const sidebarItems: SidebarItem[] = [
        {
            id: 'profile',
            label: 'Meus Dados',
            icon: 'fas fa-user-circle' // Exemplo de ícone FontAwesome
        },
        {
            id: 'terminal',
            label: 'Terminal',
            icon: 'fas fa-terminal'
        },
    ];

    return (
        <div className={styles.dashboardLayout}>
            {/* 3. Integração da Sidebar */}
            <Sidebar
                items={sidebarItems}
                activeItemId={activeTab}
                onItemClick={(item) => setActiveTab(item.id.toString())}
                logo={<h2 className={styles.logoText}>Greg Co.</h2>}
            >
                {/* Conteúdo Extra (Logout) no rodapé da Sidebar */}
                <div className={styles.sidebarFooter}>
                    <span className={styles.version}>v2.0.0</span>
                </div>
            </Sidebar>

            {/* 4. Área de Conteúdo Principal */}
            <main className={styles.mainContent}>
                <header className={styles.pageHeader}>
                    <h1>
                        {activeTab === 'profile' ? 'Meu Perfil' : 'Terminal Admin'}
                    </h1>
                    <p>
                        {activeTab === 'profile'
                            ? 'Gerencie suas informações pessoais.'
                            : 'Central de comando para redirecionamento.'}
                    </p>
                </header>

                <div className={styles.contentArea}>
                    {/* Renderização Condicional: Só mostra o que foi selecionado */}
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
                </div>
            </main>
        </div>
    );
};