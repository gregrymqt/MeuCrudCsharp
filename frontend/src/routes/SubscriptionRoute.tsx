import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../features/auth/hooks/useAuth';
import { AppRoles } from '../types/models';

export const SubscriptionRoute = () => {
  const { user } = useAuth();

  // 1. Regra de Admin: Se for Admin, libera tudo (VIP)
  const isAdmin = user?.roles?.includes(AppRoles.Admin);

  // 2. Regra de Assinatura:
  // Verifica se existe subscription E se o status é válido
  // Baseado no seu código anterior, status 'active' ou 'authorized' liberam acesso
  const hasActiveSubscription = 
    user?.subscription && 
    (user.subscription.status === 'active' || user.subscription.status === 'authorized');

  // Lógica de Decisão
  if (isAdmin || hasActiveSubscription) {
    return <Outlet />;
  }

  // Se não tiver assinatura, manda para a tela de Planos (Upsell)
  return <Navigate to="/plans" replace />;
};