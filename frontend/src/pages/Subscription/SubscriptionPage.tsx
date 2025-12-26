import React, { useState } from 'react';
// Imports dos seus componentes
import { SubscriptionActions } from '../../components/MercadoPago/Subscription/components/SubscriptionActions'; // [cite: 1]
import { SubscriptionInfo } from '../../components/MercadoPago/Subscription/components/SubscriptionInfo'; // [cite: 1]
import { useSubscription } from '../../components/MercadoPago/Subscription/hooks/useUserSubscription'; // 
// Imports da Sidebar Genérica

// Estilos
import styles from './SubscriptionPage.module.scss';
import { Sidebar } from '../../components/SideBar/components/Sidebar';
import type { SidebarItem } from '../../components/SideBar/types/sidebar.types';

export const SubscriptionPage: React.FC = () => {
    // 1. Estado para controlar a aba ativa da Sidebar
    const [activeTab, setActiveTab] = useState<string | number>('subscription');

    // 2. Definição dos itens da Sidebar [cite: 27]
    const sidebarItems: SidebarItem[] = [
        {
            id: 'subscription',
            label: 'Minha Assinatura',
            icon: 'fas fa-file-invoice-dollar'
        },
        // Adicione outros itens conforme necessário para compor o menu
        {
            id: 'history',
            label: 'Histórico de Pagamentos',
            icon: 'fas fa-history'
        }
    ];

    // 3. Renderização do conteúdo dinâmico
    const renderContent = () => {
        switch (activeTab) {
            case 'subscription':
                return <SubscriptionContent />;
            case 'history':
                return <div className={styles.infoAlert}>Histórico (Em breve)</div>;
            default:
                return null;
        }
    };

    return (
        <div className={styles.pageContainer}> {/*  */}
            <h2 className={styles.headerTitle}>Minha Conta</h2> {/* [cite: 9] */}

            <div className={styles.layoutGrid}> {/* [cite: 10] */}

                {/* --- Integração da Sidebar Genérica --- */}
                <Sidebar
                    items={sidebarItems}
                    activeItemId={activeTab}
                    onItemClick={(item) => setActiveTab(item.id)} // [cite: 17]
                    logo={<h4>Portal do Cliente</h4>} // [cite: 28]
                >
                    {/* Footer da Sidebar (Logout) [cite: 24, 29] */}
                    <button className={styles.logoutBtn}>
                        <i className="fas fa-sign-out-alt"></i> Sair
                    </button>
                </Sidebar>

                {/* --- Área Principal de Conteúdo --- */}
                <main className={styles.contentArea}> {/* [cite: 12] */}
                    {renderContent()}
                </main>
            </div>
        </div>
    );
};

// --- Sub-componente Interno: Lógica da Assinatura ---
// (Isolado aqui para manter o código limpo e só carregar dados quando necessário)
const SubscriptionContent: React.FC = () => {
    const {
        subscription,
        isLoading,
        isProcessing,
        actions
    } = useSubscription(); // 

    // Loading
    if (isLoading) {
        return (
            <div className={styles.loadingContainer}>
                <div className={styles.spinner} role="status">
                    <span className={styles.visuallyHidden}>Carregando...</span>
                </div>
            </div>
        ); // [cite: 3]
    }

    // Estado vazio
    if (!subscription) {
        return (
            <div className={styles.infoAlert} role="alert">
                <h4>Nenhuma assinatura encontrada!</h4>
                <p>Você ainda não possui um plano ativo. Acesse nossa página de planos para começar.</p>
            </div>
        ); // [cite: 4]
    }

    // Renderização Info + Ações
    return (
        <section className={styles.fadeIn}>
            {/* Visualização [cite: 6] */}
            <SubscriptionInfo data={subscription} />

            {/* Ações (Separadas no card abaixo pelo próprio componente interno) [cite: 7] */}
            <div className={styles.actionsContainer}>
                <SubscriptionActions
                    status={subscription.status}
                    onPause={actions.pause}
                    onReactivate={actions.reactivate}
                    onCancel={actions.cancel}
                    isProcessing={isProcessing}
                />
            </div>
        </section>
    ); // [cite: 5]
};