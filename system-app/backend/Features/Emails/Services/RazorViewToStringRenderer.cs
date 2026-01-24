using System;
using System.IO;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Emails.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace MeuCrudCsharp.Features.Emails.Services
{
    /// <summary>
    /// Implementa <see cref="IRazorViewToStringRenderer"/> para fornecer um mecanismo para renderizar views Razor para uma string.
    /// Isso é particularmente útil para gerar corpos de e-mail em HTML a partir de templates .cshtml fora do contexto de uma requisição MVC padrão.
    /// </summary>
    public class RazorViewToStringRenderer : IRazorViewToStringRenderer
    {
        private readonly IRazorViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Inicializa uma nova instância da classe <see cref="RazorViewToStringRenderer"/>.
        /// </summary>
        /// <param name="viewEngine">O motor de views Razor para encontrar e renderizar as views.</param>
        /// <param name="tempDataProvider">O provedor de dados temporários necessário para o contexto da view.</param>
        /// <param name="serviceProvider">O provedor de serviços para criar um contexto de requisição.</param>
        public RazorViewToStringRenderer(
            IRazorViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider
        )
        {
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Renderiza uma view Razor para uma string.
        /// </summary>
        /// <typeparam name="TModel">O tipo do modelo a ser passado para a view.</typeparam>
        /// <param name="viewName">O nome da view a ser renderizada.</param>
        /// <param name="model">O modelo a ser passado para a view.</param>
        /// <returns>Uma <see cref="Task{TResult}"/> que representa a operação assíncrona, contendo a string renderizada da view.</returns>
        public async Task<string> RenderViewToStringAsync<TModel>(string viewName, TModel model)
        {
            if (string.IsNullOrWhiteSpace(viewName))
                throw new ArgumentException("O nome da view não pode ser vazio.", nameof(viewName));

            var actionContext = GetActionContext();
            var view = FindView(actionContext, viewName);

            await using var output = new StringWriter();

            var viewContext = new ViewContext(
                actionContext,
                view,
                new ViewDataDictionary<TModel>(
                    metadataProvider: new EmptyModelMetadataProvider(),
                    modelState: new ModelStateDictionary()
                )
                {
                    Model = model,
                },
                new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
                output,
                new HtmlHelperOptions()
            );

            await view.RenderAsync(viewContext);

            return output.ToString();
        }

        /// <summary>
        /// Encontra a view Razor especificada usando o motor de views.
        /// </summary>
        /// <param name="actionContext">O contexto da ação atual.</param>
        /// <param name="viewName">O nome da view a ser encontrada.</param>
        /// <returns>A instância de <see cref="IView"/> se encontrada.</returns>
        /// <exception cref="InvalidOperationException">Lançada se a view não puder ser localizada.</exception>
        private IView FindView(ActionContext actionContext, string viewName)
        {
            var getViewResult = _viewEngine.GetView(
                executingFilePath: null,
                viewPath: viewName,
                isMainPage: true
            );
            if (getViewResult.Success)
            {
                return getViewResult.View;
            }

            var findViewResult = _viewEngine.FindView(actionContext, viewName, isMainPage: true);
            if (findViewResult.Success)
            {
                return findViewResult.View;
            }

            throw new InvalidOperationException(
                $"Não foi possível encontrar a view '{viewName}'. Os seguintes locais foram pesquisados: {string.Join(", ", findViewResult.SearchedLocations)}"
            );
        }

        /// <summary>
        /// Cria um <see cref="ActionContext"/> padrão, necessário para que o motor de views renderize uma view fora de um ciclo de requisição-resposta HTTP.
        /// </summary>
        /// <returns>Uma nova instância de <see cref="ActionContext"/>.</returns>
        private ActionContext GetActionContext()
        {
            var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
            return new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        }
    }
}
