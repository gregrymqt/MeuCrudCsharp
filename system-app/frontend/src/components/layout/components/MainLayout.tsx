import { Outlet } from 'react-router-dom';
import styles from '../styles/MainLayout.module.scss'; // Apenas o layout wrapper
import { Footer } from './Footer';
import { Navbar } from './Navbar';


/**
 * Componente que renderiza o layout principal da aplicação.
 * 
 * Responsável por renderizar o Navbar (Top Menu no Desktop e Sidebar Toggle no Mobile),
 * o conteúdo principal (páginas com suas próprias sidebars) e o Footer.
 * 
 * @returns {React.ReactElement} 
 */

export const MainLayout = () => {
  return (
    <div className={styles.layoutWrapper}>
      
      {/* Navbar cuida de: Top Menu (Desktop) e Sidebar Toggle (Mobile) */}
      <Navbar />

      <main className={styles.mainContent}>
        {/* Aqui renderiza as páginas (Home, Cursos, Features com suas próprias sidebars) */}
        <Outlet />
      </main>

      <Footer />
    </div>
  );
};