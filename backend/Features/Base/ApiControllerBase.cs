using MeuCrudCsharp.Features.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MeuCrudCsharp.Features.Base;

[ApiController] // Atributo padrão para APIs
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public abstract class ApiControllerBase : ControllerBase { }

[RateLimit(5, 60)] // Aplica em tudo que herdar daqui
public abstract class MercadoPagoApiControllerBase : ApiControllerBase 
{ 
    // Métodos comuns de pagamento...
}
