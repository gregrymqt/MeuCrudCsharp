using MeuCrudCsharp.Features.Emails;

namespace MeuCrudCsharp.Features.MercadoPago.Payments.Interfaces
{
    public interface IRazorViewToStringRenderer
    {
        Task<string> RenderViewToStringAsync<TModel>(string viewName, TModel model);
    }
}
