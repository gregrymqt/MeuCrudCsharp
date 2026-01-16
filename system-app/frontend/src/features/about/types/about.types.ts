export type AboutContentType = 'section1' | 'section2';

export interface AboutSectionData {
    id: number;
    contentType: AboutContentType;
    title: string;
    description: string;
    imageUrl: string;
    imageAlt: string;
}

export interface TeamMember {
    id: number | string;
    name: string;
    role: string;
    photoUrl: string;
    linkedinUrl?: string;
    githubUrl?: string;
}

export interface AboutTeamData {
    id: number | string;
    contentType: 'section2';
    title: string;
    description?: string;
    members: TeamMember[];
}

// NOVO: Espelho do C# AboutPageContentDto
export interface AboutPageResponse {
    sections: AboutSectionData[];
    teamSection: AboutTeamData;
}

export type AboutSectionContent = AboutSectionData | AboutTeamData;

// --- TIPOS ESPECÍFICOS PARA OS FORMULÁRIOS ---

export interface AboutSectionFormValues extends Omit<AboutSectionData, 'id' | 'contentType' | 'imageUrl'> {
  newImage?: FileList; 
}

export interface TeamMemberFormValues extends Omit<TeamMember, 'id' | 'photoUrl'> {
  newPhoto?: FileList;
}