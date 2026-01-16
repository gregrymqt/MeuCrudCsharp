import { useEffect } from 'react';
import { BrowserRouter } from 'react-router-dom';
import { AppRoutes } from './routes/AppRoutes';
import { useAuth } from './features/auth/hooks/useAuth';
import { socketService } from './shared/services/socket.service';
import { AppHubs } from './shared/enums/hub.enums';
import './App.css';

function App() {
  const { isAuthenticated } = useAuth();

  // --- SEU MÉTODO DE INICIALIZAÇÃO DE SOCKETS ---
  const initGlobalSockets = async () => {
    // Aqui você decide: conecta em todos ou só nos essenciais?
    // Ex: Pagamento é crítico, mantemos sempre conectado.
    await socketService.connect(AppHubs.Payment);
    
    // Video e Refund talvez possam ser conectados sob demanda,
    // mas se quiser notificações globais, conecte tudo aqui:
    await socketService.connect(AppHubs.Video);
    await socketService.connect(AppHubs.Refund);
  };

  useEffect(() => {
    if (isAuthenticated) {
      initGlobalSockets();
    } else {
      // Se deslogar, mata todas as conexões para segurança
      socketService.disconnect();
    }
  }, [isAuthenticated]);

  return (
    <BrowserRouter>
      <div className="app-container">
        <AppRoutes />
      </div>
    </BrowserRouter>
  );
}

export default App;