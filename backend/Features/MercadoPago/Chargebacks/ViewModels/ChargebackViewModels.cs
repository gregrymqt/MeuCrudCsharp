using System;

namespace MeuCrudCsharp.Features.MercadoPago.Chargebacks.ViewModels;

public class ChargeBackViewModels
{
    public class ChargebackDetailViewModel
    {
        public string ChargebackId { get; set; }
        public decimal Valor { get; set; }
        public string Moeda { get; set; }
        public string StatusDocumentacao { get; set; }
        public bool CoberturaAplicada { get; set; }
        public bool PrecisaDocumentacao { get; set; }
        public DateTime? DataLimiteDisputa { get; set; }
        public DateTime DataCriacao { get; set; }

        // Lista simplificada de arquivos para o front mostrar links
        public List<ChargebackFileViewModel> ArquivosEnviados { get; set; } = new();
    }

    public class ChargebackFileViewModel
    {
        public string Tipo { get; set; }
        public string Url { get; set; }
        public string NomeArquivo { get; set; }
    }

    public class ChargebacksIndexViewModel
    {
        public List<ChargebackSummaryViewModel> Chargebacks { get; set; } = new();

        // Filtros
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }

        // Paginação
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class ChargebackSummaryViewModel
    {
        public string Id { get; set; }
        public string Customer { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; }
        public string MercadoPagoUrl { get; set; }
    }
}
