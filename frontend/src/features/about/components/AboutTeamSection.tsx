import React from 'react';
import styles from '../styles/AboutTeamSection.module.css';
import { Card } from '../../../components/Card/Card';
import type { AboutTeamData, TeamMember } from '../types/about.types';

interface AboutTeamSectionProps {
    data: AboutTeamData;
}

export const AboutTeamSection: React.FC<AboutTeamSectionProps> = ({ data }) => {

    // Tratamento de clique no card (ex: abrir modal ou ir para perfil)
    const handleCardClick = (member: TeamMember) => {
        console.log('Clicou no membro:', member.name);
    };

    if (!data || !data.members.length) return null;

    return (
        <section className={styles.container}>
            <div className={styles.header}>
                <h2 className={styles.title}>{data.title}</h2>
                {data.description && <p className={styles.subtitle}>{data.description}</p>}
            </div>

            <div className={styles.gridWrapper}>
                {data.members.map((member) => (
                    <Card<TeamMember>
                        key={member.id}
                        data={member}
                        className={styles.teamCard} // Classe customizada para sobrescrever estilos se precisar
                        onClick={handleCardClick}
                    >
                        {/* 1. Imagem do Card */}
                        <Card.Image
                            src={member.photoUrl}
                            alt={`Foto de ${member.name}`}
                        />

                        {/* 2. Corpo do Card */}
                        <Card.Body title={member.name}>
                            <p className={styles.role}>{member.role}</p>
                        </Card.Body>

                        {/* 3. Ações (Links Sociais) */}
                        <Card.Actions>
                            <div className={styles.socialLinks}>
                                {member.linkedinUrl && (
                                    <a href={member.linkedinUrl} target="_blank" rel="noreferrer" onClick={(e) => e.stopPropagation()}>
                                        LinkedIn
                                    </a>
                                )}
                                {member.githubUrl && (
                                    <a href={member.githubUrl} target="_blank" rel="noreferrer" onClick={(e) => e.stopPropagation()}>
                                        GitHub
                                    </a>
                                )}
                            </div>
                        </Card.Actions>
                    </Card>
                ))}
            </div>
        </section>
    );
};