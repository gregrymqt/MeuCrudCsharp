import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "../features/auth/hooks/useAuth";
import { AppRoles } from "../types/models";

export const SubscriptionRoute = () => {
  const { user, isAuthenticated } = useAuth();

  // Segurança extra: se não estiver logado, manda pro login
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  // 1. Admin sempre passa (Regra VIP)
  // Precisamos das roles no DTO para isso funcionar
  const isAdmin = user?.roles?.includes(AppRoles.Admin);

  // 2. Verificação Otimizada
  // Em vez de checar user.subscription.status, usamos apenas o booleano 
  const hasAccess = user?.hasActiveSubscription;

  // Lógica: Admin OU Assinante Ativo
  if (isAdmin || hasAccess) {
    return <Outlet />;
  }

  // Se falhar, manda para a vitrine de planos (Upsell)
  return <Navigate to="/plans" replace />;
};