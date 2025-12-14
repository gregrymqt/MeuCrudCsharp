import {type ReactNode } from 'react';

export interface SidebarItem {
  id: string | number;
  label: string;
  icon?: string; // Classe do ícone (ex: 'fas fa-home') ou URL
  path?: string; // Opcional, caso use roteamento
}

export interface SidebarProps {
  items: SidebarItem[];
  activeItemId?: string | number;
  onItemClick?: (item: SidebarItem) => void;
  logo?: ReactNode; // Pode ser uma imagem ou texto
  children?: ReactNode; // Conteúdo extra no rodapé da sidebar (ex: logout)
}