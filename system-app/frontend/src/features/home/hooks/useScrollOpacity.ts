// hooks/useScrollOpacity.ts
import { useEffect, useState } from 'react';

export const useScrollOpacity = (maxHeightVh: number = 80) => {
  const [opacity, setOpacity] = useState(1);

  useEffect(() => {
    const handleScroll = () => {
      const scrollPosition = window.scrollY;
      const heroHeight = window.innerHeight * (maxHeightVh / 100);
      
      // Lógica original: fade out ao rolar 80% da altura do hero
      const newOpacity = 1 - (scrollPosition / (heroHeight * 0.8));
      setOpacity(Math.max(0, newOpacity));
    };

    window.addEventListener('scroll', handleScroll);
    return () => window.removeEventListener('scroll', handleScroll);
  }, [maxHeightVh]);

  return opacity;
};