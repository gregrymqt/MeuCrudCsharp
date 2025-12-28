import { Routes, Route, Navigate } from "react-router-dom";
import { ProtectedRoute } from "./ProtectedRoute";
import { SubscriptionRoute } from "./SubscriptionRoute"; // Importe o novo componente
import { AppRoles } from "../types/models";
import { MainLayout } from "../components/layout/MainLayout";
import { AccessDenied } from "../pages/AccessDenied/AccessDenied";
import { GoogleCallbackPage } from "../features/auth/components/GoogleCallbackPage";
import { Home } from "../pages/Home/Home";
import { AdminCourseManager } from "../features/admin/Courses/components/AdminCourseManager";
import { AdminProfile } from "../pages/Admin/AdminProfile";
import { GoogleLoginButton } from "../features/auth/pages/GoogleLoginButton";
import { CourseFeed } from "../features/course/pages/CourseFeed";
import { PlansFeed } from "../features/plan/pages/PlansFeed";
import { PlayerScreen } from "../features/player/pages/PlayerScreen";
import { AdminClaimsPage } from "../features/Claim/components/AdminClaimsPage";
import { UserClaimsPage } from "../features/Claim/components/UserClaimsPage";
import { ChargebackManager } from "../pages/ChargeBack/ChargebackManager";
import { WalletPage } from "../pages/Wallet/WalletPage";
import { TransactionsPage } from "../pages/Transactions/TransactionsPage";
import { SubscriptionPage } from "../pages/Subscription/SubscriptionPage";
import { ProfileDashboard } from "../pages/Profile/ProfileDashboard";
import { CreateSupportPage } from "../pages/Support/CreateSupportPage";
import { SupportAdminPage } from "../pages/Support/SupportAdminPage";

export const AppRoutes = () => {
  return (
    <Routes>
      {/* === ROTAS PÚBLICAS === */}
      <Route path="/login" element={<GoogleLoginButton />} />
      {/* === LAYOUT PRINCIPAL === */}
      <Route element={<MainLayout />}>
        <Route path="/" element={<Home />} />

        {/* Nível 1: Apenas Autenticado (Logado) */}
        <Route element={<ProtectedRoute />}>
          {/* Rotas que todo logado pode ver (Perfil, Callback, Comprar Planos) */}
          <Route path="/perfil" element={<ProfileDashboard />} />
          <Route path="/plans" element={<PlansFeed />} />
          <Route path="/login/callback" element={<GoogleCallbackPage />} />
          <Route path="reclamacoes" element={<UserClaimsPage />} />

          {/* === Nível 1.5: Requer Assinatura Ativa OU Admin === */}
          {/*  */}
          <Route element={<SubscriptionRoute />}>
            <Route path="/cursos" element={<CourseFeed />} /> {/* [cite: 4] */}
            <Route path="/player/:videoId" element={<PlayerScreen />} />
            <Route path="/carteira" element={<WalletPage />} />
            <Route path="/transacoes" element={<TransactionsPage />} />
            <Route path="/assinaturas" element={<SubscriptionPage />} />
          </Route>
        </Route>

        <Route path="/acesso-negado" element={<AccessDenied />} />
        <Route path="/suporte/novo" element={<CreateSupportPage />} />

        {/* Nível 2: Apenas ADMIN */}
        <Route element={<ProtectedRoute allowedRoles={[AppRoles.Admin]} />}>
          <Route path="/admin" element={<AdminProfile />} />
          <Route path="/admin/cursos" element={<AdminCourseManager />} />
          <Route path="/admin/reclamacoes" element={<AdminClaimsPage />} />
          <Route path="/admin/contestacoes" element={<ChargebackManager />} />
          <Route path="/admin/suporte" element={<SupportAdminPage />} />
        </Route>

        {/* Nível 3: Manager OU Admin */}
        <Route
          element={
            <ProtectedRoute allowedRoles={[AppRoles.Admin, AppRoles.Manager]} />
          }
        >
          <Route path="/relatorios" element={<h1>Relatórios Financeiros</h1>} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} /> {/* [cite: 7] */}
    </Routes>
  );
}; // [cite: 8]
