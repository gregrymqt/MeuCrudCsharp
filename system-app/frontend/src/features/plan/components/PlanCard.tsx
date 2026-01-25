import React from 'react';

import styles from '../styles/PlanCard.module.scss';
import  { Card } from '../../../components/Card/Card';
import type { PlanUI } from '../types/plan.type';

interface PlanCardProps {
  plan: PlanUI;
  onSelect: (plan: PlanUI) => void;
}

export const PlanCard: React.FC<PlanCardProps> = ({ plan, onSelect }) => {
  // Define classes condicionais
  const cardClasses = `${styles.planCardWrapper} ${plan.isRecommended ? styles.recommended : ''}`;

  return (
    // Usamos o Card Genérico tipado com PlanUI [cite: 5]
    <Card<PlanUI> 
      data={plan} 
      onClick={onSelect} // O Card genérico já trata o clique [cite: 6]
      className={cardClasses}
    >
      {/* Badge Flutuante (Customizado fora do fluxo padrão do GenericCard) */}
      {plan.isRecommended && (
        <div className={styles.floatingBadge}>MAIS POPULAR</div>
      )}

      {/* Corpo do Card usando o sub-componente do seu sistema  */}
      <Card.Body title={plan.name}>
        
        {/* Bloco de Preço (Layout Específico) */}
        <div className={styles.priceBlock}>
          <span className={styles.currency}>R$</span>
          <span className={styles.mainValue}>{plan.priceMain}</span>
          <div className={styles.columnMeta}>
            <span className={styles.cents}>{plan.priceCents}</span>
            <span className={styles.frequency}>{plan.frequencyLabel}</span>
          </div>
        </div>

        {/* Lista de Features */}
        <ul className={styles.featuresList}>
          {plan.features.map((feature, index) => (
            <li key={index}>
              <span className={styles.checkIcon}>✓</span>
              {feature}
            </li>
          ))}
        </ul>
      </Card.Body>

      {/* Ações usando o sub-componente do seu sistema  */}
      <Card.Actions>
        <button className={styles.ctaButton}>
          {plan.buttonText}
        </button>
      </Card.Actions>
    </Card>
  );
};