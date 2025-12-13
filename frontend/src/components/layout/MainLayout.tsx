import { Outlet } from 'react-router-dom';


// Importa o css global aqui para garantir que carregue
import '../styles/global.scss'; 
import { Header } from './header/Header';
import { Footer } from './footer/Footer';

export const MainLayout = () => {
  return (
    <div className="app-wrapper">
      <Header />
      
      {/* Container principal com padding-top para compensar o Header fixo */}
      <div className="container main-container" style={{ paddingTop: '80px', minHeight: '80vh' }}>
        <main role="main" className="pb-3">
            {/* O Outlet é onde as rotas filhas (Home, Sobre) serão renderizadas */}
            <Outlet />
        </main>
      </div>

      <Footer />
    </div>
  );
};