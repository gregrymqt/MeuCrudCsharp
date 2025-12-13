import { useAuth } from './hooks/useAuth';
import './GoogleLoginButton.scss';

export const GoogleLoginButton = () => {
  const { loginGoogle } = useAuth();

  return (
    <button 
      type="button" 
      className="google-btn" 
      onClick={loginGoogle}
      aria-label="Entrar com Google"
    >
      <div className="google-icon-wrapper">
        <img 
          className="google-icon" 
          src="https://upload.wikimedia.org/wikipedia/commons/5/53/Google_%22G%22_Logo.svg" 
          alt="Google logo" 
        />
      </div>
      <span className="btn-text">Continuar com o Google</span>
    </button>
  );
};