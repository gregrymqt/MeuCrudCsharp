import React, { useCallback } from 'react';
import styles from './PlansFeed.module.scss';

// Sub-components
import { PlanCard } from './components/Card/PlanCard';
import { PaginationControls } from './components/PaginationControl/PaginationControls';

// Hook e Types
import { usePlans } from './hooks/usePlans';
import type { PlanUI } from './types/plan.type';

export const PlansFeed: React.FC = () => {
  // 1. Consumindo o Hook
  // Definimos 6 itens por página (ajuste conforme seu layout desktop)
  const { 
    plans, 
    isLoading, 
    error,
    pagination, 
    refresh 
  } = usePlans(6); 

  // 2. Lógica de Seleção
  // Como definimos 'buttonAction' no Service com a URL correta (/checkout...), 
  // aqui apenas redirecionamos.
  const handlePlanSelect = useCallback((plan: PlanUI) => {
    if (plan.buttonAction) {
      // Se for SPA (Single Page App) use navigate(plan.buttonAction)
      // Se for legado misturado, window.location funciona bem:
      window.location.href = plan.buttonAction;
    }
  }, []);

  // 3. Estado de Erro (Visual simples)
  if (error) {
    return (
      <div className={styles.errorContainer}>
        <p>{error}</p>
        <button onClick={refresh} className={styles.retryBtn}>
          Tentar Novamente
        </button>
      </div>
    );
  }

  return (
    <section className={styles.feedContainer}>
      <div className={styles.header}>
        <h2>Escolha seu Plano</h2>
        <p>Desbloqueie todo o potencial da plataforma.</p>
      </div>

      {isLoading ? (
        <div className={styles.loadingContainer}>
           {/* Se tiver um componente de Spinner, use aqui */}
           <div className={styles.spinner}></div>
           <span>Carregando planos...</span>
        </div>
      ) : (
        <>
          <div className={styles.plansGrid}>
            {plans.map((plan) => (
              <PlanCard 
                key={plan.id} 
                plan={plan} 
                onSelect={handlePlanSelect} 
              />
            ))}

            {plans.length === 0 && (
              <p className={styles.emptyState}>Nenhum plano disponível no momento.</p>
            )}
          </div>
          
          <PaginationControls 
            currentPage={pagination.currentPage}
            totalPages={pagination.totalPages}
            onNext={pagination.nextPage}
            onPrev={pagination.prevPage}
          />
        </>
      )}
    </section>
  );
};