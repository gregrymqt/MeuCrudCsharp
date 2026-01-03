// components/Home/Services.tsx
import React from 'react';
import styles from '../styles/Home.module.scss';
import type { ServiceData } from '../../types/home.types';

interface ServicesProps {
  data: ServiceData[];
}

export const Services: React.FC<ServicesProps> = ({ data }) => {
  return (
    <section className={styles.servicesSection}>
      <div className="container">
        <h2 className="section-title">Por que escolher nossa plataforma?</h2>
        <div className={styles.grid}>
          {data.map((item) => (
            <div key={item.id} className={styles.serviceCard}>
              <i className={item.iconClass}></i>
              <h3>{item.title}</h3>
              <p>{item.description}</p>
              <a href={item.actionUrl} className="btn btn-secondary">
                {item.actionText}
              </a>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
};