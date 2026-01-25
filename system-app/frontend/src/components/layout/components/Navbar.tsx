// components/Header/Navbar.tsx
import { useState } from 'react';
import { NavLink, Link } from 'react-router-dom';
import styles from '../styles/Navbar.module.scss';
import { UserArea } from './UserArea';

export const Navbar = () => {
  const [isSidebarOpen, setSidebarOpen] = useState(false);

  // Lista de Links Central
  const navLinks = [
    { path: '/', label: 'Home', icon: 'fas fa-home' },
    { path: '/cursos', label: 'Cursos', icon: 'fas fa-graduation-cap' },
    { path: '/sobre', label: 'Sobre', icon: 'fas fa-info-circle' },
    { path: '/contato', label: 'Contato', icon: 'fas fa-envelope' },
  ];

  return (
    <>
      <nav className={styles.navbar}>
        <div className={styles.container}>
          
          {/* 1. ESQUERDA: Logo */}
          <Link to="/" className={styles.logo}>
            <i className="fas fa-graduation-cap"></i>
            <span>SeuCurso</span>
          </Link>

          {/* 2. CENTRO: Links (Apenas Desktop) */}
          <div className={styles.desktopNav}>
            {navLinks.map((link) => (
              <NavLink 
                key={link.path} 
                to={link.path}
                className={({ isActive }) => isActive ? styles.active : ''}
              >
                {link.label}
              </NavLink>
            ))}
          </div>

          {/* 3. DIREITA: User Area (Desktop) ou Toggle (Mobile) */}
          <div className="d-flex align-items-center gap-3">
            {/* Desktop User */}
            <div className={styles.desktopUserArea}>
              <UserArea isMobile={false} />
            </div>

            {/* Mobile Toggle Button */}
            <button 
              className={styles.mobileToggle} 
              onClick={() => setSidebarOpen(true)}
              aria-label="Abrir menu"
            >
              <i className="fas fa-bars"></i>
            </button>
          </div>
        </div>
      </nav>

      {/* --- MOBILE SIDEBAR COMPONENT --- */}
      {/* Overlay Escuro */}
      <div 
        className={`${styles.overlay} ${isSidebarOpen ? styles.open : ''}`}
        onClick={() => setSidebarOpen(false)}
      />

      {/* A Gaveta Lateral */}
      <aside className={`${styles.sidebar} ${isSidebarOpen ? styles.open : ''}`}>
        <div className={styles.sidebarHeader}>
          <button className="btn btn-link text-dark" onClick={() => setSidebarOpen(false)}>
            <i className="fas fa-times fa-lg"></i>
          </button>
        </div>

        <nav className={styles.sidebarNav}>
          {navLinks.map((link) => (
            <NavLink 
              key={link.path} 
              to={link.path} 
              onClick={() => setSidebarOpen(false)}
              className={({ isActive }) => isActive ? 'text-primary fw-bold' : ''}
            >
              <i className={`${link.icon} fa-fw`}></i>
              {link.label}
            </NavLink>
          ))}
        </nav>

        <div className={styles.sidebarFooter}>
          {/* User Area versão Mobile (Expandable) */}
          <UserArea isMobile={true} />
        </div>
      </aside>
    </>
  );
};