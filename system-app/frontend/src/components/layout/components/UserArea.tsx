// components/Header/UserArea.tsx
import React, { useState, useRef, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import styles from '../styles/Navbar.module.scss';
import { useAuth } from '../../../features/auth/hooks/useAuth';

interface UserAreaProps {
  isMobile?: boolean;
}

export const UserArea: React.FC<UserAreaProps> = ({ isMobile = false }) => {
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();
  const {user,logout} = useAuth(); // Trocar pelo useAuth() depois
 
  // Fechar ao clicar fora (Desktop)
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleLogout = () => {
    alert("Logout realizado");
    logout();
    navigate('/login');
  };

  if (!user) {
    return (
      <Link to="/login" className="btn btn-primary w-100">
        Entrar
      </Link>
    );
  }

  // --- VERSÃO MOBILE (Expandable no rodapé da sidebar) ---
  if (isMobile) {
    return (
      <div className={styles.mobileUserMenu}>
        <div className={styles.userInfo} onClick={() => setIsOpen(!isOpen)}>
          <img src={user.avatarUrl} alt={user.name} />
          <div>
            <div className="fw-bold">{user.name}</div>
            <small className="text-muted">{user.roles || 'Aluno'}</small>
          </div>
          <i className={`fas fa-chevron-${isOpen ? 'up' : 'down'} ms-auto`}></i>
        </div>
        
        <div className={`${styles.userOptions} ${isOpen ? styles.show : ''}`}>
          <Link to="/perfil"><i className="fas fa-user-circle me-2"></i> Meu Perfil</Link>
          {user.roles.some((role: string) => role.toUpperCase() === 'ADMIN') && (
             <Link to="/admin"><i className="fas fa-cog me-2"></i> Painel Admin</Link>
          )}
          <button onClick={handleLogout} className="text-danger">
            <i className="fas fa-sign-out-alt me-2"></i> Sair
          </button>
        </div>
      </div>
    );
  }

  // --- VERSÃO DESKTOP (Dropdown Flutuante) ---
  return (
    <div className={styles.userDropdown} ref={dropdownRef}>
      <button 
        className={styles.trigger} 
        onClick={() => setIsOpen(!isOpen)}
        aria-expanded={isOpen}
      >
        <img src={user.avatarUrl} alt="Avatar" />
        <span>{user.name}</span>
        <i className="fas fa-chevron-down"></i>
      </button>

      <div className={`${styles.menuDesktop} ${isOpen ? styles.show : ''}`}>
        <Link to="/perfil" onClick={() => setIsOpen(false)}>Meu Perfil</Link>
        {user.roles.some((role: string) => role.toUpperCase()== 'ADMIN' && (
             <Link to="/admin" onClick={() => setIsOpen(false)}>Painel Admin</Link>
        ))}
        <div className="dropdown-divider"></div>
        <button onClick={handleLogout} className="text-danger">Sair</button>
      </div>
    </div>
  );
};