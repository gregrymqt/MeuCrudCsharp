using Microsoft.AspNetCore.Authorization;

namespace MeuCrudCsharp.Features.Authorization;

    public class ActiveSubscriptionRequirement : IAuthorizationRequirement
    {
        // Esta classe pode estar vazia ou conter parâmetros se a sua regra de negócio precisar.
        // Para este caso (apenas verificar se a assinatura está ativa), ela pode ser vazia.
    }
