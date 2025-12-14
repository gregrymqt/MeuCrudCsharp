import { type User, AppRoles } from "../../../types/models";

/**
 * UserSession: O "Documento Completo"
 * Estende a interface User (do models.ts) para herdar TODA a estrutura do banco.
 * Adiciona apenas os campos técnicos de sessão (tokens e roles).
 */
export interface UserSession extends User {
  // Controle de Acesso e Sessão (Não estão na tabela Users, mas vêm no Login)
  token: string;          // JWT
  refreshToken?: string;  // Para renovar sem logar de novo
  roles: AppRoles[];      // Lista de permissões (Admin, User, etc.)
  expiration?: string;    // Data de validade do token
}