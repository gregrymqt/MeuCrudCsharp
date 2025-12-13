import { useEffect, useRef } from 'react';
import { useSearchParams, useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import './GoogleCallbackPage.scss';

export const GoogleCallbackPage = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { handleGoogleCallback } = useAuth();
  
  // UseRef para garantir que o efeito rode apenas uma vez (React 18 Strict Mode)
  const processedRef = useRef(false);

  useEffect(() => {
    if (processedRef.current) return;

    const token = searchParams.get('token');
    
    if (token) {
      processedRef.current = true;
      // Passa o token para o hook validar, salvar e baixar os dados do usuário
      handleGoogleCallback(token);
    } else {
      // Se não tiver token, algo deu errado, volta pro login
      console.error("Token não encontrado na URL de retorno");
      navigate('/login?error=no_token');
    }
  }, [searchParams, handleGoogleCallback, navigate]);

  return (
    <div className="google-callback-page">
      <div className="callback-content">
        <h2>Autenticando...</h2>
        <div className="spinner"></div>
      </div>
    </div>
  );
};