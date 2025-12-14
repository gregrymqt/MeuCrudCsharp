import { useState } from 'react';
import { useAuth } from '../../auth/hooks/useAuth'; // Seu hook de auth
import { ProfileService } from '../services/profile.service';
import { ApiError } from '../../../shared/services/api.service';
import type { UserSession } from '../../auth/types/auth.types';

interface AvatarFormData {
  file: FileList; // React Hook Form retorna FileList para inputs do tipo 'file'
}

export const useAvatarUpdate = () => {
  const [isLoading, setIsLoading] = useState(false);
  const { user ,updateUser } = useAuth(); // Pega a função que atualiza o cookie [cite: 1]

  const updateAvatar = async (data: AvatarFormData) => {
    if (!user) return;

    if (!data.file || data.file.length === 0) {
      alert("Por favor, selecione uma imagem.");
      return;
    }

    try {
      setIsLoading(true);

      // 1. Preparar o FormData
      const formData = new FormData();
      formData.append('avatar', data.file[0]); // Pega o primeiro arquivo

      // 2. Enviar para API
      const response = await ProfileService.updateAvatar(formData);

      // 3. Atualizar a Sessão Local (Cookie/Storage) com a nova URL retornada
      // A função updateUser mescla os dados, então passamos apenas o que mudou [cite: 2]
      const userData = { ...user, avatarUrl: response.avatarUrl };
      await updateUser(userData as UserSession);

      alert('Foto de perfil atualizada com sucesso!');
      
    } catch (error) {
      if(error instanceof ApiError)  {
      console.error(error);
      alert(error.message || "Erro ao atualizar foto.");
      }
    } finally {
      setIsLoading(false);
    }
  };

  return { updateAvatar, isLoading };
};