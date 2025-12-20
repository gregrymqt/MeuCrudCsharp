import React, { useState, useMemo } from 'react';

import styles from './WalletPage.module.scss';
import { AddCardForm } from '../../components/MercadoPago/Wallet/components/AddCardForm';
import { WalletList } from '../../components/MercadoPago/Wallet/components/WalletList';
import { Sidebar } from '../../components/SideBar/components/Sidebar';
import type { SidebarItem } from '../../components/SideBar/types/sidebar.types';

// IDs das Views
type ViewState = 'list' | 'add';

export const WalletPage: React.FC = () => {
  const [activeView, setActiveView] = useState<ViewState>('list');

  // Configuração dos itens do Menu
  const menuItems: SidebarItem[] = useMemo(() => [
    { 
      id: 'list', 
      label: 'Minha Carteira', 
      icon: 'fas fa-wallet' 
    },
    { 
      id: 'add', 
      label: 'Novo Cartão', 
      icon: 'fas fa-plus-circle' 
    },
    // Exemplo de item desabilitado ou futuro
    { 
      id: 'history', 
      label: 'Histórico', 
      icon: 'fas fa-history' 
    }
  ], []);

  // Handler de navegação da Sidebar
  const handleNavigation = (item: SidebarItem) => {
    // Se for 'history' ou outro ID, você pode redirecionar ou ignorar
    if (item.id === 'list' || item.id === 'add') {
      setActiveView(item.id as ViewState);
    }
  };

  // Renderização Condicional do Conteúdo
  const renderContent = () => {
    switch (activeView) {
      case 'add':
        return (
          <div className={styles.contentWrapper}>
            {/* Título da Seção */}
            <div style={{ marginBottom: '1.5rem' }}>
              <h2>Adicionar Método de Pagamento</h2>
              <p style={{ color: '#6c757d' }}>Insira os dados do seu cartão para assinaturas futuras.</p>
            </div>
            
            <AddCardForm 
              onSuccess={() => setActiveView('list')} // UX: Volta pra lista ao salvar
              onCancel={() => setActiveView('list')}  // UX: Volta pra lista ao cancelar
            />
          </div>
        );
      
      case 'list':
      default:
        return (
          <div className={styles.contentWrapper}>
             {/* Título da Seção */}
             <div style={{ marginBottom: '1.5rem' }}>
              <h2>Gerenciar Carteira</h2>
              <p style={{ color: '#6c757d' }}>Visualize e gerencie seus cartões salvos.</p>
            </div>

            <WalletList />
          </div>
        );
    }
  };

  // Componente de Logo para passar para a Sidebar
  const LogoComponent = (
    <div className={styles.brandLogo}>
      <i className="fas fa-layer-group"></i>
      <span>MinhaApp</span>
    </div>
  );

  return (
    <div className={styles.pageLayout}>
      {/* SIDEBAR: Responsável pelo Header Mobile e Menu Lateral */}
      <Sidebar
        logo={LogoComponent}
        items={menuItems}
        activeItemId={activeView}
        onItemClick={handleNavigation}
      >
        {/* Children da Sidebar = Footer (Logout) */}
        <button className={styles.logoutBtn} onClick={() => console.log('Logout')}>
          <i className="fas fa-sign-out-alt"></i>
          <span>Sair</span>
        </button>
      </Sidebar>

      {/* ÁREA DE CONTEÚDO PRINCIPAL */}
      <main className={styles.mainContent}>
        {renderContent()}
      </main>
    </div>
  );
};