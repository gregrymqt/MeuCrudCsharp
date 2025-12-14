// components/Home/About.tsx
import React from 'react';
import styles from './Home.module.scss';
import type { AboutData } from '../types/home.types';

interface AboutProps {
  data: AboutData;
}

export const About: React.FC<AboutProps> = ({ data }) => {
  return (
    <section className={styles.aboutSection}>
      <div className={`container ${styles.grid}`}>
        <div className={styles.aboutImage}>
          <img src={data.imageUrl} alt={data.title} />
        </div>
        <div className={styles.aboutText}>
          <h2 className="section-title">{data.title}</h2>
          <p>{data.description}</p>
          <ul>
            {data.benefits.map((benefit, index) => (
              <li key={index}>
                <i className="fas fa-check-circle"></i> {benefit}
              </li>
            ))}
          </ul>
        </div>
      </div>
    </section>
  );
};