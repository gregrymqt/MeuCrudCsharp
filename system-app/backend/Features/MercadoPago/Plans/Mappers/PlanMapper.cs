using MeuCrudCsharp.Features.MercadoPago.Plans.DTOs;
using MeuCrudCsharp.Features.MercadoPago.Plans.Utils;
using MeuCrudCsharp.Models;

// Importe seus Utils

namespace MeuCrudCsharp.Features.MercadoPago.Plans.Mappers
{
    public static class PlanMapper
    {
        public static PlanDto MapDbPlanToDto(Plan dbPlan)
        {
            string planType = PlanUtils.GetPlanTypeDescription(
                dbPlan.FrequencyInterval,
                dbPlan.FrequencyType
            );
            bool isAnnual =
                dbPlan.FrequencyInterval == 12 && dbPlan.FrequencyType == PlanFrequencyType.Months;

            return new PlanDto(
                dbPlan.PublicId.ToString(),
                dbPlan.Name,
                planType,
                PlanUtils.FormatPriceDisplay(dbPlan.TransactionAmount, dbPlan.FrequencyInterval),
                PlanUtils.FormatBillingInfo(dbPlan.TransactionAmount, dbPlan.FrequencyInterval),
                PlanUtils.GetDefaultFeatures(),
                isAnnual,
                dbPlan.IsActive
            );
        }

        public static PlanDto MapApiPlanToDto(PlanResponseDto apiPlan, Plan localPlan)
        {
            var frequencyTypeEnum = string.Equals(
                apiPlan.AutoRecurring.FrequencyType,
                "days",
                StringComparison.OrdinalIgnoreCase
            )
                ? PlanFrequencyType.Days
                : PlanFrequencyType.Months;

            string planType = PlanUtils.GetPlanTypeDescription(
                apiPlan.AutoRecurring.Frequency,
                frequencyTypeEnum
            );
            bool isRecommended =
                apiPlan.AutoRecurring.Frequency == 12
                && frequencyTypeEnum == PlanFrequencyType.Months;
            var isActive = string.Equals(
                apiPlan.Status,
                "active",
                StringComparison.OrdinalIgnoreCase
            );

            return new PlanDto(
                localPlan.PublicId.ToString(),
                apiPlan.Reason,
                planType,
                PlanUtils.FormatPriceDisplay(
                    apiPlan.AutoRecurring.TransactionAmount,
                    apiPlan.AutoRecurring.Frequency
                ),
                PlanUtils.FormatBillingInfo(
                    apiPlan.AutoRecurring.TransactionAmount,
                    apiPlan.AutoRecurring.Frequency
                ),
                PlanUtils.GetDefaultFeatures(),
                isRecommended,
                isActive
            );
        }

        public static PlanEditDto MapPlanToEditDto(Plan dbPlan)
        {
            return new PlanEditDto(
                dbPlan.PublicId.ToString(),
                dbPlan.Name,
                dbPlan.TransactionAmount,
                dbPlan.FrequencyInterval,
                dbPlan.FrequencyType.ToString().ToLower()
            );
        }
    }
}
