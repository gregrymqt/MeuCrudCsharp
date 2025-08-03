using System;
using Azure.Core;
using Microsoft.Extensions.Configuration; // Adicione este using

// Adicione este using
namespace MeuCrudCsharp.Features.MercadoPago.Tokens
{
    public class TokenMercadoPago
    {
        public string? _access_Token { get; }
        public string? _webhook_Secret { get; }

        public TokenMercadoPago(IConfiguration configuration)
        {
            _access_Token = configuration["MERCADOPAGO_ACCESS_TOKEN"];
            _webhook_Secret = configuration["MERCADOPAGO_WEBHOOK_SECRET"];
            // É uma boa prática verificar se os tokens foram carregados para evitar erros.
            // Se o token não for encontrado na configuração, o programa irá parar na inicialização
            // com uma mensagem de erro clara, o que é muito melhor do que falhar depois.
            if (string.IsNullOrEmpty(_access_Token))
            {
                throw new ArgumentNullException(
                    nameof(_access_Token),
                    "O 'MERCADOPAGO_ACCESS_TOKEN' não foi encontrado na configuração."
                );
            }

            if (string.IsNullOrEmpty(_webhook_Secret))
            {
                throw new ArgumentNullException(
                    nameof(_webhook_Secret),
                    "O 'MERCADOPAGO_WEBHOOK_SECRET' não foi encontrado na configuração."
                );
            }
        }
    }
}
