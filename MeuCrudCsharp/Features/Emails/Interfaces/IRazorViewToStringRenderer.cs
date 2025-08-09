using System.Threading.Tasks;

namespace MeuCrudCsharp.Features.Emails.Interfaces
{
    /// <summary>
    /// Define um contrato para um serviço que renderiza uma view Razor para uma string.
    /// Útil para gerar corpos de e-mail em HTML a partir de templates Razor.
    /// </summary>
    public interface IRazorViewToStringRenderer
    {
        /// <summary>
        /// Renderiza uma view Razor especificada para uma string de forma assíncrona.
        /// </summary>
        /// <typeparam name="TModel">O tipo do modelo de dados a ser passado para a view.</typeparam>
        /// <param name="viewName">O nome ou caminho da view Razor a ser renderizada (ex: "/Views/Emails/Welcome.cshtml").</param>
        /// <param name="model">O objeto de modelo a ser usado pela view durante a renderização.</param>
        /// <returns>Uma <see cref="Task{TResult}"/> que representa a operação assíncrona, contendo a string renderizada da view.</returns>
        Task<string> RenderViewToStringAsync<TModel>(string viewName, TModel model);
    }
}
