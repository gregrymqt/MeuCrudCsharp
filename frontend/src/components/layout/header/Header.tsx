import { useState, useRef, useEffect } from 'react';
import { NavLink, Link } from 'react-router-dom';
import './Header.scss';
import { mockUser } from '../../../features/auth/types/auth.types';

export const Header = () => {
  // Lógica de Estado (substitui o JS puro)
  const [isMobileMenuOpen, setMobileMenuOpen] = useState(false);
  const [isUserDropdownOpen, setUserDropdownOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  
  // Simulação de usuário logado (depois virá do useAuth)
  const user = mockUser; 
  // const user = null; // Teste deslogado descomentando aqui

  // Fecha o dropdown se clicar fora (substitui window.addEventListener('click') do JS original)
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setUserDropdownOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  return (
    <header className="site-header">
      <nav className="main-nav container">
        {/* Logo */}
        <Link to="/" className="nav-logo">
          <i className="fas fa-graduation-cap"></i>
          <span>SeuCurso</span>
        </Link>

        <div className="nav-center-right">
          {/* Menu Principal (Sidebar no Mobile) */}
          <div className={`nav-menu ${isMobileMenuOpen ? 'show' : ''}`} id="nav-menu">
            <ul className="nav-links">
              {/* NavLink automaticamente coloca a classe 'active' [cite: 83] */}
              <li><NavLink to="/" className="nav-link" onClick={() => setMobileMenuOpen(false)}>Home</NavLink></li>
              <li><NavLink to="/cursos" className="nav-link" onClick={() => setMobileMenuOpen(false)}>Cursos</NavLink></li>
              <li><NavLink to="/sobre" className="nav-link" onClick={() => setMobileMenuOpen(false)}>Sobre</NavLink></li>
              <li><NavLink to="/contato" className="nav-link" onClick={() => setMobileMenuOpen(false)}>Contato</NavLink></li>
            </ul>

            {/* Área do Usuário (LoginPartial) */}
            <div className="nav-user">
              {user ? (
                <div className="user-dropdown" ref={dropdownRef}>
                  <button 
                    className="user-info-button" 
                    onClick={() => setUserDropdownOpen(!isUserDropdownOpen)}
                    aria-expanded={isUserDropdownOpen}
                  >
                    <img src={user.avatarUrl || '/default-avatar.png'} alt="Avatar" className="user-avatar" />
                    <span>{user.name}</span>
                    <i className={`fas fa-chevron-down dropdown-caret ${isUserDropdownOpen ? 'rotate' : ''}`}></i>
                  </button>

                  <div className={`dropdown-menu ${isUserDropdownOpen ? 'show' : ''}`}>
                    <Link to="/perfil" className="dropdown-item" onClick={() => setUserDropdownOpen(false)}>
                      <i className="fas fa-user-circle"></i> Meu Perfil
                    </Link>
                    <div className="dropdown-divider"></div>
                    <button className="dropdown-item logout-button" onClick={() => alert("Logout realizado!")}>
                      <i className="fas fa-sign-out-alt"></i> Sair
                    </button>
                  </div>
                </div>
              ) : (
                <Link to="/login" className="btn-primary">Entrar</Link>
              )}
            </div>
          </div>
        </div>

        {/* Toggle Mobile */}
        <button 
          className="nav-toggle" 
          onClick={() => setMobileMenuOpen(!isMobileMenuOpen)}
          aria-label="Toggle navigation"
        >
          <i className={`fas ${isMobileMenuOpen ? 'fa-times' : 'fa-bars'}`}></i>
        </button>
      </nav>
    </header>
  );
};