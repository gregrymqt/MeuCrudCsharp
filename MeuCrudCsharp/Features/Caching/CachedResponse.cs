namespace MeuCrudCsharp.Features.Caching
{
    /// <summary>
    /// Representa uma resposta serializável para armazenamento em cache,
    /// incluindo o corpo e o código de status HTTP associado.
    /// </summary>
    /// <param name="Body">Conteúdo serializável da resposta.</param>
    /// <param name="StatusCode">Código de status HTTP relacionado à resposta.</param>
    public record CachedResponse(object Body, int StatusCode);
}
