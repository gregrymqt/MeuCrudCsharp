import { Routes, Route, Navigate } from "react-router-dom";
import { ProtectedRoute } from "./ProtectedRoute";
import { SubscriptionRoute } from "./SubscriptionRoute"; // Importe o novo componente
import { AppRoles } from "../types/models";
import { MainLayout } from "../components/layout/MainLayout";
import { AccessDenied } from "../pages/AccessDenied/AccessDenied";
import { GoogleLoginButton } from "../features/auth/GoogleLoginButton";
import { GoogleCallbackPage } from "../features/auth/components/GoogleCallbackPage";
import { Home } from "../pages/Home/Home";
import { ProfileDashboard } from "../features/profile/ProfileDashboard";
import { CourseFeed } from "../features/course/CourseFeed";
import { PlayerScreen } from "../features/player/PlayerScreen";
import { PlansFeed } from "../features/plan/PlansFeed";
import { AdminCourseManager } from "../features/admin/Courses/components/AdminCourseManager";
import { AdminProfile } from "../pages/Admin/AdminProfile";

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

          {/* === Nível 1.5: Requer Assinatura Ativa OU Admin === */}
          {/*  */}
          <Route element={<SubscriptionRoute />}>
             <Route path="/cursos" element={<CourseFeed />} /> {/* [cite: 4] */}
             <Route path="/player/:videoId" element={<PlayerScreen />} />
          </Route>
        </Route>

        <Route path="/acesso-negado" element={<AccessDenied />} />

        {/* Nível 2: Apenas ADMIN */}
        <Route element={<ProtectedRoute allowedRoles={[AppRoles.Admin]} />}>
          <Route path="/admin" element={<AdminProfile/>} />
          <Route path="/admin/cursos" element={<AdminCourseManager/>} />
        </Route>

        {/* Nível 3: Manager OU Admin */}
        <Route element={<ProtectedRoute allowedRoles={[AppRoles.Admin, AppRoles.Manager]} />}>
          <Route path="/relatorios" element={<h1>Relatórios Financeiros</h1>} />
        </Route>
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} /> {/* [cite: 7] */}
    </Routes>
  );
}; // [cite: 8]