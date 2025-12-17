import { useNavigate } from 'react-router-dom';
import type { SubmitHandler } from 'react-hook-form';
import type { TerminalFormData } from '../types/adminProfile.types';

export const useAdminTerminal = () => {
  const navigate = useNavigate();

  const handleTerminalCommand: SubmitHandler<TerminalFormData> = (data) => {
    // Lógica de roteamento baseada nos 3 seletores
    // Exemplo de URL gerada: /admin/course/list?filter=active
    const baseUrl = `/admin/${data.module}`;
    
    // Mapeamento simples para rotas (pode ser expandido)
    let finalPath = baseUrl;

    switch (data.module) {
        case 'course':
            finalPath = '/admin/courses'; 
            break;
        case 'video':
            finalPath = '/admin/videos';
            break;
        case 'mercadopago':
            finalPath = '/admin/finance';
            break;
    }

    // Adiciona a ação e filtro como query params ou sufixos
    // Isso torna o terminal poderoso para já cair na tela filtrada
    const query = `?action=${data.action}&status=${data.filter}`;
    
    console.log(`[Terminal] Executing command: ${finalPath}${query}`);
    navigate(`${finalPath}${query}`);
  };

  return {
    handleTerminalCommand
  };
};