import { Routes, Route, Navigate } from 'react-router-dom';
import { ProtectedRoute } from './ProtectedRoute';
import { AppRoles } from '../types/models';
import { MainLayout } from '../components/layout/MainLayout';
import { AccessDenied } from '../pages/AccessDenied/AccessDenied';
import { GoogleLoginButton } from '../features/auth/GoogleLoginButton';
import { GoogleCallbackPage } from '../features/auth/components/GoogleCallbackPage';
import { Home } from '../pages/Home/Home';
import { ProfileDashboard } from '../features/profile/ProfileMain';

// Importe suas páginas aqui (Exemplos)
// import { Dashboard } from '../pages/Dashboard';
// import { AdminPanel } from '../pages/AdminPanel';

export const AppRoutes = () => {
  return (
    <Routes>
      
      {/* === ROTAS PÚBLICAS (Qualquer um acessa) === */}
      <Route path="/login" element={
        <GoogleLoginButton/>
      } />
      
      {/* Rota de Acesso Negado (Pública para poder ser exibida) */}
      <Route path="/acesso-negado" element={
        <MainLayout> {/* Usando Layout para manter o Header [cite: 2] */}
          <AccessDenied />
        </MainLayout>
      } />


      {/* === ROTAS PROTEGIDAS (Precisa estar logado) === */}
      <Route element={<MainLayout />}> {/* Layout envolve tudo visualmente */}

      <Route path="/" element={
          <Home/>
      } /> 
        
        {/* Nível 1: Apenas Autenticado (Qualquer Role) */}
        <Route element={<ProtectedRoute />}>


          <Route path="/perfil" element={<ProfileDashboard/>} />
          <Route path="/cursos" element={<h1>Meus Cursos</h1>} /> {/* [cite: 4] */}
          <Route path='/login/callback' element={<GoogleCallbackPage/>}/>
        
        </Route>


        {/* Nível 2: Apenas ADMIN (Role Específica) */}
        <Route element={<ProtectedRoute allowedRoles={[AppRoles.Admin]} />}>
          
          <Route path="/admin" element={<h1>Painel Administrativo</h1>} />
          <Route path="/admin/usuarios" element={<h1>Gerenciar Usuários</h1>} />
        
        </Route>


        {/* Nível 3: Manager OU Admin */}
        <Route element={<ProtectedRoute allowedRoles={[AppRoles.Admin, AppRoles.Manager]} />}>
          
          <Route path="/relatorios" element={<h1>Relatórios Financeiros</h1>} />
        
        </Route>

      </Route>


      {/* === ROTA CATCH-ALL (404) === */}
      <Route path="*" element={<Navigate to="/" replace />} /> {/* [cite: 7] */}

    </Routes>
  );
}; // [cite: 8]