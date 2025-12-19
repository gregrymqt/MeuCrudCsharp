import React, { useEffect, useRef } from 'react';
import ReactDOM from 'react-dom';
import styles from './Modal.module.scss';

// Tipos de tamanho para flexibilidade
export type ModalSize = 'small' | 'medium' | 'large' | 'full';

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title?: string;
  children: React.ReactNode;
  footer?: React.ReactNode; // Botões de ação opcionais
  size?: ModalSize;
  closeOnOverlayClick?: boolean;
}

export const Modal: React.FC<ModalProps> = ({
  isOpen,
  onClose,
  title,
  children,
  footer,
  size = 'medium',
  closeOnOverlayClick = true
}) => {
  const overlayRef = useRef<HTMLDivElement>(null);

  // Fecha ao pressionar ESC
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose();
      }
    };

    if (isOpen) {
      document.addEventListener('keydown', handleKeyDown);
      // Evita scroll no body atrás do modal
      document.body.style.overflow = 'hidden';
    }

    return () => {
      document.removeEventListener('keydown', handleKeyDown);
      document.body.style.overflow = 'unset';
    };
  }, [isOpen, onClose]);

  // Fecha ao clicar no overlay (fora do modal)
  const handleOverlayClick = (e: React.MouseEvent) => {
    if (closeOnOverlayClick && e.target === overlayRef.current) {
      onClose();
    }
  };

  if (!isOpen) return null;

  // Renderiza via Portal no body (melhor para acessibilidade e z-index)
  return ReactDOM.createPortal(
    <div 
      className={styles.overlay} 
      ref={overlayRef} 
      onClick={handleOverlayClick}
      role="dialog"
      aria-modal="true"
    >
      <div className={`${styles.modal} ${styles[size]}`}>
        {/* Cabeçalho */}
        <div className={styles.header}>
          <h3>{title}</h3>
          <button 
            className={styles.closeBtn} 
            onClick={onClose}
            aria-label="Fechar modal"
          >
            {/* Ícone X (SVG Inline) */}
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <line x1="18" y1="6" x2="6" y2="18"></line>
              <line x1="6" y1="6" x2="18" y2="18"></line>
            </svg>
          </button>
        </div>

        {/* Conteúdo */}
        <div className={styles.body}>
          {children}
        </div>

        {/* Rodapé (Opcional) */}
        {footer && (
          <div className={styles.footer}>
            {footer}
          </div>
        )}
      </div>
    </div>,
    document.body
  );
};