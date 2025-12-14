import { Outlet } from 'react-router-dom';
import styles from './MainLayout.module.scss'; // Apenas o layout wrapper
import { Navbar } from './components/header/Navbar';
import { Footer } from './components/footer/Footer';

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