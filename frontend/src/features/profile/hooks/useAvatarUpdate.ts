import { useState } from "react";
import { useAuth } from "../../auth/hooks/useAuth"; 
import { ProfileService } from "../services/profile.service";
import { ApiError } from "../../../shared/services/api.service";

interface AvatarFormData {
  file: FileList; // O React Hook Form retorna FileList
}

export const useAvatarUpdate = () => {
  const [isLoading, setIsLoading] = useState(false);
  const { user, refreshSession } = useAuth();

  const updateAvatar = async (data: AvatarFormData) => {
    if (!user) return;

    // Validação simples
    if (!data.file || data.file.length === 0) {
      alert("Por favor, selecione uma imagem.");
      return;
    }

    try {
      setIsLoading(true);

      // CORREÇÃO: Pegamos o arquivo físico diretamente (File)
      // Não criamos 'new FormData()' aqui, o ApiService fará isso.
      const fileToUpload = data.file[0];

      // Chamamos a service passando apenas o File
      const response = await ProfileService.updateAvatar(fileToUpload);

      // Atualiza a sessão (assumindo que o back retorna a nova URL)
      // Você pode passar a nova URL para o refreshSession se sua lógica permitir
      await refreshSession(); 

      alert(response.message || "Foto de perfil atualizada com sucesso!");
      
    } catch (error) {
      if (error instanceof ApiError) {
        console.error(error);
        alert(error.message || "Erro ao atualizar foto.");
      } else {
        alert("Erro inesperado.");
      }
    } finally {
      setIsLoading(false);
    }
  };

  return { updateAvatar, isLoading };
};