namespace MeuCrudCsharp.Features.MercadoPago.Base;

public interface IMercadoPagoServiceBase
{
    Task<string> SendMercadoPagoRequestAsync<T>(HttpMethod method,
        string endpoint,
        T? payload);
}