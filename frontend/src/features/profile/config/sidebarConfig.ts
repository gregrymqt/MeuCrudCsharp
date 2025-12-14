import type { SidebarItem } from "../../../components/SideBar/types/sidebar.types";

export const PROFILE_SIDEBAR_ITEMS: SidebarItem[] = [
  { 
    id: 'info', // Antes era '/perfil'
    label: 'Meu Perfil', 
    icon: 'fas fa-user' 
  },
  { 
    id: 'payments', // Antes era '/perfil/pagamentos'
    label: 'Histórico de Pagamentos', 
    icon: 'fas fa-file-invoice-dollar' 
  },
  { 
    id: 'subscription', // Antes era '/perfil/assinatura'
    label: 'Gerenciar Assinatura', 
    icon: 'fas fa-credit-card' 
  }
];