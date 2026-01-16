using System;

namespace MeuCrudCsharp.Models.Enums;

public enum SubscriptionStatus
{
    Pending,
    Authorized, // O equivalente a "active" no MP
    Paused,
    Cancelled,
    Unknown,
}

public static class SubscriptionStatusExtensions
{
    public static string ToMpString(this SubscriptionStatus status)
    {
        return status switch
        {
            SubscriptionStatus.Pending => "pending",
            SubscriptionStatus.Authorized => "authorized",
            SubscriptionStatus.Paused => "paused",
            SubscriptionStatus.Cancelled => "cancelled",
            _ => "unknown",
        };
    }

    public static SubscriptionStatus FromMpString(string status)
    {
        return status.ToLower() switch
        {
            "pending" => SubscriptionStatus.Pending,
            "authorized" => SubscriptionStatus.Authorized,
            "paused" => SubscriptionStatus.Paused,
            "cancelled" => SubscriptionStatus.Cancelled,
            _ => SubscriptionStatus.Unknown,
        };
    }
}
