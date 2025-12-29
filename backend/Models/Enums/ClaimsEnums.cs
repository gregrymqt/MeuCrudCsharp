using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MeuCrudCsharp.Models.Enums
{
    // Baseado em 
    public enum ClaimResource
    {
        [Display(Name = "Pagamento")]
        Payment,    // "payment"
        
        [Display(Name = "Pedido")]
        Order,      // "order"
        
        [Display(Name = "Envio")]
        Shipment,   // "shipment"
        
        [Display(Name = "Compra")]
        Purchase    // "purchase"
    }

    // Baseado em 
    public enum ClaimStage
    {
        [Display(Name = "Reclamação")]
        Claim,      // "claim" - Fase inicial
        
        [Display(Name = "Disputa")]
        Dispute,    // "dispute" - Em desacordo
        
        [Display(Name = "Recontato")]
        Recontact,  // "recontact" - Após encerramento
        
        [Display(Name = "Nenhuma")]
        None        // "none"
    }

    // Baseado em 
    public enum ClaimType
    {
        [Display(Name = "Mediação")]
        Mediations,      // "mediations"
        
        [Display(Name = "Cancelamento de Compra")]
        CancelPurchase,  // "cancel_purchase"
        
        [Display(Name = "Devolução")]
        Return,          // "return"
        
        [Display(Name = "Cancelamento de Venda")]
        CancelSale,      // "cancel_sale"
        
        [Display(Name = "Alteração")]
        Change           // "change"
    }

    // Baseado em 
    // Status original do Mercado Pago (diferente do seu status interno)
    public enum MpClaimStatus
    {
        [Display(Name = "Aberto")]
        Opened, // "opened"
        
        [Display(Name = "Fechado")]
        Closed  // "closed"
    }
}