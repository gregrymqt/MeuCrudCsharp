import { useNavigate } from 'react-router-dom';
import '../styles/AccessDenied.scss'; // Criaremos abaixo

export const AccessDenied = () => {
  const navigate = useNavigate();

  return (
    <div className="access-denied-container">
      <div className="denied-content">
        <div className="icon-wrapper">
          <i className="fas fa-user-lock"></i>
        </div>
        <h1>Acesso Restrito</h1>
        <p>
          Ops! Parece que você não tem permissão para acessar esta área.
          Se você acha que isso é um erro, contate seu administrador.
        </p>
        
        <div className="actions">
          <button onClick={() => navigate(-1)} className="btn-secondary">
            <i className="fas fa-arrow-left"></i> Voltar
          </button>
          <button onClick={() => navigate('/')} className="btn-primary">
            <i className="fas fa-home"></i> Ir para o Início
          </button>
        </div>
      </div>
    </div>
  );
};