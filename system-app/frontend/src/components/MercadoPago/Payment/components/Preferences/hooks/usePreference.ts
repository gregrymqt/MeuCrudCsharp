// src/pages/Payment/hooks/usePreference.ts
import { useState, useEffect } from "react";
import { PreferenceService } from "../services/preference.service";

export const usePreference = (amount: number, shouldFetch: boolean = true) => {
  const [preferenceId, setPreferenceId] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!shouldFetch || !amount) return;

    const fetchPreference = async () => {
      setLoading(true);
      try {
        const id = await PreferenceService.createPreference(amount, "Acesso ao curso");
        setPreferenceId(id);
      } catch (err) {
        console.error("Erro ao criar preferência MP", err);
        setError("Não foi possível iniciar a sessão de pagamento.");
      } finally {
        setLoading(false);
      }
    };

    fetchPreference();
  }, [amount, shouldFetch]);

  return { preferenceId, loading, error };
};