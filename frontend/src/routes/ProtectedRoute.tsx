import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { AppRoles } from '../types/models'; // Seus Enums

interface ProtectedRouteProps {
  allowedRoles?: AppRoles[]; // Array de roles permitidas (Opcional)
}

export const ProtectedRoute = ({ allowedRoles }: ProtectedRouteProps) => {
  const { user, isAuthenticated } = useAuth();
  const location = useLocation();

  // 1. Verificação Básica: Está logado?
  if (!isAuthenticated) {
    // Redireciona para login, mas salva onde ele queria ir (state)
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  // 2. Verificação de Role (Se a rota exigir roles específicas)
  if (allowedRoles && allowedRoles.length > 0) {
    // Verifica se o usuário tem ALGUMA das roles permitidas
    const hasPermission = user?.roles.some(role => allowedRoles.includes(role));

    if (!hasPermission) {
      return <Navigate to="/acesso-negado" replace />;
    }
  }

  // 3. Tudo certo? Renderiza o conteúdo (Outlet)
  return <Outlet />;
};