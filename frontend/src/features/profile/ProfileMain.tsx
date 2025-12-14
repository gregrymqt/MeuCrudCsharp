import React, { useState } from 'react';
import type { SidebarItem } from '../../components/SideBar/types/sidebar.types';
import { PaymentHistory } from './components/history/PaymentHistory';
import { ProfileInfo } from './components/info/ProfileInfo';
import { SubscriptionManager } from './components/subscription/SubscriptionManager';
import { PROFILE_SIDEBAR_ITEMS } from './config/sidebarConfig';

// 1. IMPORTAR O USEAUTH (Sua fonte de verdade agora)
import { useAuth } from '../../features/auth/hooks/useAuth'; // Ajuste o caminho
import type { SubscriptionDetails } from './types/profile.types';
import { Sidebar } from '../../components/SideBar/components/Sidebar';

export const ProfileDashboard: React.FC = () => {
  // ESTADO: Define qual aba está ativa.
  const [activeTab, setActiveTab] = useState<string>('info');

  // DADOS: Buscamos direto do Hook de Autenticação (Cookie/Storage)
  // updateUser serve para recarregar os dados no cookie se algo mudar (ex: cancelar plano)
  const { user } = useAuth(); 

  // --- ADAPTAÇÃO DE DADOS (MAPPING) ---
  // O objeto 'user' do models.ts  é a estrutura do banco.
  // Precisamos transformá-lo na estrutura visual 'SubscriptionDetails' que os componentes esperam.
  const subscriptionData: SubscriptionDetails | null = user?.subscription ? {
    id: String(user.subscription.id), // TransactionBase tem ID
    planName: user.subscription.plan?.name || 'Plano Ativo', // 
    // Assumindo que TransactionBase tem status, ou mapeamos manualmente
    status: user.subscription.status || 'active', 
    amount: user.subscription.amount, // [cite: 28] herdado de TransactionBase
    lastFourCardDigits: user.subscription.lastFourCardDigits, // 
    nextBillingDate: user.subscription.currentPeriodEndDate // 
  } : null;

  // FUNÇÃO: Renderiza o conteúdo baseado na aba escolhida
  const renderContent = () => {
    // Se não tiver user carregado ainda, mostra loading
    if (!user) return <div className="p-5 text-center">Carregando perfil...</div>;

    switch (activeTab) {
      case 'info':
        return (
          <ProfileInfo 
            // Passamos o dado já tratado vindo do cookie
            subscription={subscriptionData} 
          />
        );

      case 'payments':
        // O PaymentHistory continua buscando seus dados (lista grande não costuma ficar em cookie) [cite: 9]
        return <PaymentHistory />;

      case 'subscription':
        return subscriptionData ? (
          <SubscriptionManager 
            data={subscriptionData} 
            // Ao alterar algo, chamamos updateUser para renovar o cookie/sessão
            onRefresh={() => {
                // Aqui você pode chamar uma função que força a renovação do token/user
                // Exemplo: api.get('/auth/me').then(u => updateUser(u));
                console.log("Atualizar sessão do usuário...");
            }} 
          />
        ) : (
          <div className="alert alert-info">
            Você não possui uma assinatura ativa no momento.
          </div>
        );

      default:
        return null;
    }
  };

  // LAYOUT
  return (
    <div style={{ display: 'flex', minHeight: '100vh', backgroundColor: '#f4f6f8' }}>
      
      {/* SIDEBAR */}
      <Sidebar 
        items={PROFILE_SIDEBAR_ITEMS}
        activeItemId={activeTab}
        onItemClick={(item: SidebarItem) => setActiveTab(String(item.id))}
        logo={<h4>Minha Conta</h4>}
      />

      {/* ÁREA DE CONTEÚDO */}
      <main style={{ flex: 1, padding: '2rem', overflowY: 'auto' }}>
        <div className="container" style={{ maxWidth: '900px' }}>
          <h2 className="mb-4">
            {PROFILE_SIDEBAR_ITEMS.find(i => i.id === activeTab)?.label}
          </h2>
          
          {renderContent()}
        </div>
      </main>

    </div>
  );
};