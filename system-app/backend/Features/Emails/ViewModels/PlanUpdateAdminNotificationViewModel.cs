using System.Collections.Generic;

namespace MeuCrudCsharp.Features.Emails.ViewModels;

public record PlanUpdateAdminNotificationViewModel(
    string PlanName,
    string ExternalId,
    List<string> Changes,
    string DashboardUrl
);