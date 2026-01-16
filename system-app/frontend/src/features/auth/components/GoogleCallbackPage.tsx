import { useEffect, useRef } from "react";
import { useSearchParams, useNavigate } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";
import styles from "../styles/GoogleCallbackPage.module.scss";

export const GoogleCallbackPage = () => {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const { handleGoogleCallback } = useAuth();

  const processedRef = useRef(false);

  useEffect(() => {
    if (processedRef.current) return;

    const token = searchParams.get("token");

    if (token) {
      processedRef.current = true;
      handleGoogleCallback(token);
    } else {
      console.error("Token não encontrado na URL de retorno");
      navigate("/login?error=no_token");
    }
  }, [searchParams, handleGoogleCallback, navigate]);

  return (
    <div className={styles["google-callback-page"]}>
      <div className={styles["callback-content"]}>
        <h2>Autenticando...</h2>
        <div className={styles["spinner"]}></div>
      </div>  
    </div>
  );
};
