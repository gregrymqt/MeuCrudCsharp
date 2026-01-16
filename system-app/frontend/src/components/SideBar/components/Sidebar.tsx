import React, { useState } from 'react';
import styles from './Sidebar.module.scss';
import type { SidebarProps } from '../types/sidebar.types';

export const Sidebar: React.FC<SidebarProps> = ({ 
  items, 
  activeItemId, 
  onItemClick, 
  logo, 
  children 
}) => {
  const [isOpen, setIsOpen] = useState(false);

  const toggleSidebar = () => setIsOpen(!isOpen);
  const closeSidebar = () => setIsOpen(false);

  const handleItemClick = (item: SidebarProps['items'][number]) => {
    if (onItemClick) onItemClick(item);
    closeSidebar(); // UX: Fecha a sidebar ao clicar em um item no mobile
  };

  return (
    <>
      {/* 1. Header Mobile (Só aparece em telas pequenas via CSS) */}
      <header className={styles.mobileHeader}>
        <div className={styles.logoArea}>{logo}</div>
        <button onClick={toggleSidebar} aria-label="Abrir Menu">
          <i className="fas fa-bars"></i>
        </button>
      </header>

      {/* 2. Overlay (Fundo escuro ao abrir no mobile) */}
      <div 
        className={`${styles.overlay} ${isOpen ? styles.open : ''}`} 
        onClick={closeSidebar}
      />

      {/* 3. A Sidebar em si */}
      <aside className={`${styles.sidebar} ${isOpen ? styles.open : ''}`}>
        <div className={styles.header}>
          <div className={styles.brand}>
            {logo}
          </div>
          {/* Ícone de fechar (Só aparece no mobile via CSS) */}
          <button className={styles.closeBtn} onClick={closeSidebar}>
            <i className="fas fa-times"></i>
          </button>
        </div>

        <nav className={styles.nav}>
          {items.map((item) => (
            <div
              key={item.id}
              className={`${styles.navItem} ${activeItemId === item.id ? styles.active : ''}`}
              onClick={() => handleItemClick(item)}
              role="button"
              tabIndex={0}
            >
              {item.icon && <i className={item.icon}></i>}
              <span>{item.label}</span>
            </div>
          ))}
        </nav>

        {/* Área para Logout ou Info do Usuário no rodapé */}
        {children && (
          <div className={styles.footer}>
            {children}
          </div>
        )}
      </aside>
    </>
  );
};