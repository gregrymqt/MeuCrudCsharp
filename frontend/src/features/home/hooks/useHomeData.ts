import { useState, useEffect } from 'react';
import { HomeService } from '../services/home.service';
import { type HomeContent } from '../types/home.types';
import { ApiError } from '../../../shared/services/api.service';


export const useHomeData = () => {
  const [data, setData] = useState<HomeContent | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    const fetchData = async () => {
      try {
        setLoading(true);
        const result = await HomeService.getHomeData();
        
        if (isMounted) {
          setData(result);
          setError(null);
        }
      } catch (err) {
        if (isMounted) {
          // Usa a classe de erro personalizada que definimos no ApiService
          if (err instanceof ApiError) {
            setError(err.message);
          } else {
            setError('Falha ao carregar conteúdo da home.');
          }
          console.error(err);
        }
      } finally {
        if (isMounted) setLoading(false);
      }
    };

    fetchData();

    return () => {
      isMounted = false;
    };
  }, []);

  return { data, loading, error };
};