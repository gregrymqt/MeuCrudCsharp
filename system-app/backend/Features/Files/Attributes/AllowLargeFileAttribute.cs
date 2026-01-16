using System;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MeuCrudCsharp.Features.Files.Attributes;

/// <summary>
/// Atributo para liberar upload de arquivos grandes (Vídeos/Imagens).
/// Configura automaticamente o RequestSizeLimit e o MultipartBodyLengthLimit.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AllowLargeFileAttribute : RequestFormLimitsAttribute, IResourceFilter
{
    private readonly long _maxSizeBytes;

    /// <param name="maxSizeInMb">Tamanho máximo em Megabytes (Padrão: 500MB)</param>
    public AllowLargeFileAttribute(int maxSizeInMb = 500)
    {
        _maxSizeBytes = (long)maxSizeInMb * 1024 * 1024;

        // Configurações do Form (Herdado de RequestFormLimitsAttribute)
        this.MultipartBodyLengthLimit = _maxSizeBytes;
        this.ValueLengthLimit = int.MaxValue;
        this.ValueCountLimit = int.MaxValue; // Permite muitos campos no formulário
    }

    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        // Configurações do Servidor Kestrel/IIS
        var features = context.HttpContext.Features;
        var maxBodySizeFeature = features.Get<IHttpMaxRequestBodySizeFeature>();

        if (maxBodySizeFeature != null && !maxBodySizeFeature.IsReadOnly)
        {
            maxBodySizeFeature.MaxRequestBodySize = _maxSizeBytes;
        }
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
        // Não é necessário implementar nada aqui
    }
}
