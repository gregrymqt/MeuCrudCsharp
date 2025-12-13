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

/**
 * MOCK COMPLETO
 * Simula exatamente o que o seu Backend vai retornar com:
 * .Include(u => u.Subscription).ThenInclude(s => s.Plan)
 */
export const mockUser: UserSession = {
  // 1. Dados Básicos (Identity + User)
  id: "user-guid-123456",
  publicId: "8a6e0804-2bd0-4672-b79d-d97027f9071a",
  userName: "lucas.vicente",
  name: "Lucas Vicente De Souza",
  email: "lucasvicentedesouza021@gmail.com",
  emailConfirmed: true,
  phoneNumber: "(13) 99624-3198",
  phoneNumberConfirmed: true,
  twoFactorEnabled: false,
  avatarUrl: "https://github.com/lucasvicente.png",
  createdAt: "2024-01-15T10:00:00Z", // Data ISO vinda do C#
  
  // 2. Dados de Sessão (Gerados no Login)
  token: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...", // Token JWT falso
  roles: [AppRoles.Admin, AppRoles.Manager], // O cara é patrão
  
  // 3. Assinatura (O "Include" do Backend)
  subscription: {
    id: "sub-guid-987654",
    userId: "user-guid-123456",
    status: "active", // String ou Enum dependendo de como serializar
    createdAt: "2024-02-01T14:00:00Z",
    currentPeriodStartDate: "2025-12-01T00:00:00Z",
    currentPeriodEndDate: "2026-01-01T00:00:00Z",
    planId: 101,
    planPublicId: "plan-gold-001",
    lastFourCardDigits: "4242",
    
    // 4. Plano (Include aninhado: Subscription -> Plan)
    plan: {
      id: 101,
      publicId: "plan-gold-001",
      externalPlanId: "mp-plan-gold",
      name: "Plano Ouro - Greg Company",
      description: "Acesso total a todos os cursos de C# e ASP.NET",
      transactionAmount: 49.90,
      currencyId: "BRL",
      frequencyInterval: 1,
      frequencyType: 1, // Months
      isActive: true
    }
  },

  // 5. Histórico de Pagamentos (Cuidado com o tamanho do Array aqui!)
  payments: [
    {
      id: "pay-001",
      userId: "user-guid-123456",
      amount: 49.90,
      status: "approved",
      createdAt: "2025-11-01T10:00:00Z",
      subscriptionId: "sub-guid-987654",
      installments: 1
    },
    {
      id: "pay-002",
      userId: "user-guid-123456",
      amount: 49.90,
      status: "approved",
      createdAt: "2025-12-01T10:00:00Z",
      subscriptionId: "sub-guid-987654",
      installments: 1
    }
  ]
};