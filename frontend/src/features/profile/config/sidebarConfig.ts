import type { SidebarItem } from "../../../components/SideBar/types/sidebar.types";
import type { UserSession } from "../../auth/types/auth.types";


export const getProfileSidebarItems = (user: UserSession | null): SidebarItem[] => {
  const items: SidebarItem[] = [
    { 
      id: 'info', 
      label: 'Meu Perfil', 
      icon: 'fas fa-user' 
    }
  ];

  if (!user) return items;

  // Só adiciona o item se tiver histórico de pagamento
  if (user.hasPaymentHistory) {
    items.push({ 
      id: 'payments', 
      label: 'Histórico de Pagamentos', 
      icon: 'fas fa-file-invoice-dollar' 
    });
  }

  // Só adiciona o item se tiver assinatura ativa
  if (user.hasActiveSubscription) {
    items.push({ 
      id: 'subscription', 
      label: 'Minha Assinatura', 
      icon: 'fas fa-credit-card' 
    });
  }

  return items;
};