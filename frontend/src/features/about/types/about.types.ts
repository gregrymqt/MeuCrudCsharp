// types/AboutTypes.ts

// Define os tipos de seção que sua API pode retornar
export type AboutContentType = 'section1' | 'section2';

export interface AboutSectionData {
    id: number; // ou string, dependendo do seu banco
    contentType: AboutContentType;
    title: string;
    description: string; // Pode ser HTML ou Markdown se vier formatado
    imageUrl: string;
    imageAlt: string;
}

export interface TeamMember {
    id: number | string;
    name: string;
    role: string;
    photoUrl: string;
    linkedinUrl?: string; // Opcional: Link para perfil
    githubUrl?: string;   // Opcional: Link para portfolio
}

// O dado que a API vai retornar para a Seção 2
export interface AboutTeamData {
    id: number | string; // <--- ADICIONE ISSO AQUI PARA CORRIGIR O ERRO
    contentType: 'section2';
    title: string;
    description?: string; // Um subtítulo opcional para a seção
    members: TeamMember[];
}

// Union Type atualizado
export type AboutSectionContent = AboutSectionData | AboutTeamData;