import { useAuth } from '../hooks/useAuth';
import styles from '../styles/GoogleLoginButton.module.scss';

export const GoogleLoginButton = () => {
  const { loginGoogle } = useAuth();

  return (
    <button 
      type="button" 
      className={styles['google-btn']} 
      onClick={loginGoogle}
      aria-label="Entrar com Google"
    >
      <div className={styles['google-icon-wrapper']}>
        <img 
          className={styles['google-icon']} 
          src="https://upload.wikimedia.org/wikipedia/commons/5/53/Google_%22G%22_Logo.svg" 
          alt="Google logo" 
        />
      </div>
      <span className={styles['btn-text']}>Continuar com o Google</span>
    </button>
  );
};