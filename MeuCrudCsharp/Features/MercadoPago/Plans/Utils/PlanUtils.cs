
using MeuCrudCsharp.Features.MercadoPago.Plans.DTOs;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.MercadoPago.Plans.Utils
{
    public static class PlanUtils
    {
        public static string GetPlanTypeDescription(int interval, PlanFrequencyType frequencyType)
        {
            if (frequencyType == PlanFrequencyType.Months)
            {
                switch (interval)
                {
                    case 1: return "Mensal";
                    case 3: return "Trimestral";
                    case 6: return "Semestral";
                    case 12: return "Anual";
                    default: return $"Pacote de {interval} meses";
                }
            }
            if (frequencyType == PlanFrequencyType.Days)
            {
                return interval == 1 ? "Diário" : $"Pacote de {interval} dias";
            }
            return "Plano Padrão";
        }

        public static List<string> GetDefaultFeatures() => new()
        {
            "Acesso a todos os cursos",
            "Vídeos novos toda semana",
            "Suporte via comunidade",
            "Cancele quando quiser",
        };

        public static string FormatPriceDisplay(decimal amount, int frequency)
        {
            if (frequency == 12)
            {
                var monthlyPrice = amount / 12;
                return $"R$ {monthlyPrice:F2}".Replace('.', ',');
            }
            return $"R$ {amount:F2}".Replace('.', ',');
        }

        public static string FormatBillingInfo(decimal amount, int frequency)
        {
            if (frequency == 12)
            {
                return $"Cobrado R$ {amount:F2} anualmente".Replace('.', ',');
            }
            return "&nbsp;";
        }
        public static void ApplyUpdateDtoToPlan(Plan localPlan, UpdatePlanDto updateDto)
        {
            if (updateDto.Reason != null) localPlan.Name = updateDto.Reason;
            if (updateDto.TransactionAmount.HasValue) localPlan.TransactionAmount = updateDto.TransactionAmount.Value;
            if (updateDto.Frequency.HasValue)
                localPlan.FrequencyInterval = updateDto.Frequency.Value;
            if (updateDto.FrequencyType != null)
            {
                if (!Enum.TryParse<PlanFrequencyType>(updateDto.FrequencyType, ignoreCase: true,
                        out var frequencyTypeEnum))
                {
                    throw new ArgumentException(
                        $"O valor '{updateDto.FrequencyType}' é inválido para o tipo de frequência. Use 'Days' ou 'Months'.");
                }

                localPlan.FrequencyType = frequencyTypeEnum;
            }
          
        }
    }
}