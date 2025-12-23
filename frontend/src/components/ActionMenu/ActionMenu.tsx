import React, { useState, useRef, useEffect } from 'react';
import './ActionMenu.scss';

interface ActionMenuProps {
  onEdit?: () => void;      // Agora é opcional (?)
  onDelete?: () => void;    // Agora é opcional (?)
  disabled?: boolean;
  children?: React.ReactNode; // Aceita botões extras (Estornar, Detalhes, etc)
}

export const ActionMenu: React.FC<ActionMenuProps> = ({ 
  onEdit, 
  onDelete, 
  disabled = false,
  children
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  // Fecha ao clicar fora
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside);
    }
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isOpen]);

  return (
    <div className="action-menu" ref={menuRef}>
      {/* Gatilho */}
      <button 
        className={`action-menu__trigger ${isOpen ? 'active' : ''}`} 
        onClick={() => setIsOpen(!isOpen)}
        disabled={disabled}
        type="button"
      >
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <circle cx="12" cy="12" r="1"></circle>
          <circle cx="12" cy="5" r="1"></circle>
          <circle cx="12" cy="19" r="1"></circle>
        </svg>
      </button>

      {/* Dropdown Genérico */}
      {isOpen && (
        <div 
            className="action-menu__dropdown"
            // Garante que qualquer clique dentro do menu (edit, delete ou custom) feche o menu
            onClick={() => setIsOpen(false)} 
        >
          {/* 1. Renderiza botão de Editar se a prop existir */}
          {onEdit && (
            <button className="action-menu__item" onClick={onEdit}>
              <span className="icon edit-icon">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path>
                    <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path>
                </svg>
              </span>
              Atualizar
            </button>
          )}

          {/* 2. Renderiza botão de Deletar se a prop existir */}
          {onDelete && (
            <button className="action-menu__item delete" onClick={onDelete}>
              <span className="icon delete-icon">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <polyline points="3 6 5 6 21 6"></polyline>
                    <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
                </svg>
              </span>
              Deletar
            </button>
          )}

          {/* Divisor se houver botões padrões E customizados */}
          {(onEdit || onDelete) && children && <div className="action-menu__divider"></div>}

          {/* 3. Renderiza conteúdo customizado (seus botões de Estornar/Detalhes) */}
          {children}
        </div>
      )}
    </div>
  );
};