import React from 'react';
import { About } from '../../features/home/components/About';
import { Hero } from '../../features/home/components/Hero';
import { Services } from '../../features/home/components/Services';
import { useHomeData } from '../../features/home/hooks/useHomeData';


export const Home: React.FC = () => {
  // O Hook cuida de toda a "sujeira" (chamada de API, separação de tipos, loading)
  const { data, loading, error } = useHomeData();

  if (loading) {
    return (
      <div className="loading-container">
        {/* Aqui você pode por um Spinner ou Skeleton Screen */}
        <p>Carregando experiências...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="error-container">
        <p>Ops! {error}</p>
        <button onClick={() => window.location.reload()}>Tentar Novamente</button>
      </div>
    );
  }

  if (!data) return null;

  return (
    <main>
      {/* Renderiza Hero apenas se tiver slides */}
      {data.hero.length > 0 && (
        <Hero slides={data.hero} />
      )}

      {/* Renderiza Services apenas se tiver itens */}
      {data.services.length > 0 && (
        <Services data={data.services} />
      )}

      {/* Renderiza About apenas se o objeto existir */}
      {data.about && (
        <About data={data.about} />
      )}
    </main>
  );
};