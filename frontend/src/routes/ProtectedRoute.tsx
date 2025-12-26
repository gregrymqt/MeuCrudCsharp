import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../features/auth/hooks/useAuth';
import { AppRoles } from '../types/models'; // Ajuste o caminho se necessário

// --- PROTECTED ROUTE (Baseada em Autenticação e Roles) ---
interface ProtectedRouteProps {
  allowedRoles?: AppRoles[];
}

export const ProtectedRoute = ({ allowedRoles }: ProtectedRouteProps) => {
  const { user, isAuthenticated } = useAuth();
  const location = useLocation();

  // 1. Verificação de Autenticação
  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  // 2. Verificação de Permissão (Roles)
  // Agora acessamos user.roles, que deve ser adicionado ao DTO
  if (allowedRoles && allowedRoles.length > 0) {
    // Se o usuário não tiver roles ou não tiver a role necessária
    const hasPermission = user?.roles?.some(role => allowedRoles.includes(role as AppRoles));
    
    if (!hasPermission) {
      return <Navigate to="/acesso-negado" replace />;
    }
  }

  return <Outlet />;
};