import React, { useState } from 'react';
import styles from './TransactionsPage.module.scss'; // CSS Modules
import { FailedPaymentsList } from '../../components/MercadoPago/Transactions/components/FailedPaymentsList';
import { Sidebar } from '../../components/SideBar/components/Sidebar';
import type { SidebarItem } from '../../components/SideBar/types/sidebar.types';
import {PaymentHistory} from "src/components/MercadoPago/Transactions/components/PaymentHistory.tsx";

// Definição dos itens do menu lateral
const MENU_ITEMS: SidebarItem[] = [
    {
        id: 'history',
        label: 'Histórico Geral',
        icon: 'fas fa-file-invoice-dollar' // [cite: 13]
    },
    {
        id: 'issues',
        label: 'Recusados e Estornos',
        icon: 'fas fa-exclamation-triangle'
    }
];

export const TransactionsPage: React.FC = () => {
    // Estado para controlar qual componente renderizar
    const [activeView, setActiveView] = useState<string>('history');

    // Handler para troca de menu
    const handleNavigation = (item: SidebarItem) => {
        setActiveView(item.id.toString());
    };

    // Função para renderizar o conteúdo dinâmico
    const renderContent = () => {
        switch (activeView) {
            case 'history':
                return (
                    <div className={styles.fadeEntry}>
                        <header>
                            <h2>Minhas Faturas</h2>
                            <p>Visualize e gerencie todos os seus pagamentos aprovados.</p>
                        </header>
                        <PaymentHistory />
                    </div>
                );

            case 'issues':
                return (
                    <div className={styles.fadeEntry}>
                        <header>
                            <h2>Problemas de Pagamento</h2>
                            <p>Verifique transações que não foram processadas ou solicite suporte.</p>
                        </header>
                        <FailedPaymentsList />
                    </div>
                );

            default:
                return null;
        }
    };

    return (
        <div className={styles.pageContainer}>
            {/* SIDEBAR:
         No mobile ela mostra o Header com o ícone de hambúrguer[cite: 6].
         No desktop ela fica fixa lateralmente.
      */}
            <Sidebar
                items={MENU_ITEMS}
                activeItemId={activeView}
                onItemClick={handleNavigation} // [cite: 4]
                logo={
                    <div style={{ fontSize: '1.2rem', fontWeight: 'bold', color: '#007bff' }}>
                        <i className="fas fa-wallet"></i> Financeiro
                    </div>
                }
            >
                {/* Rodapé da Sidebar (Opcional, ex: Saldo) [cite: 11] */}
                <div style={{ padding: '1rem', borderTop: '1px solid #dee2e6', fontSize: '0.85rem' }}>
                    <p>Precisa de ajuda?</p>
                    <small>suporte@gregcompany.com</small>
                </div>
            </Sidebar>

            {/* ÁREA DE CONTEÚDO PRINCIPAL */}
            <main className={styles.contentArea}>
                {renderContent()}
            </main>
        </div>
    );
};