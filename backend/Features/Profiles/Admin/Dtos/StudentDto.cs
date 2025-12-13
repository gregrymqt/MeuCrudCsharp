using System;
using System.ComponentModel.DataAnnotations;

namespace MeuCrudCsharp.Features.Profiles.Admin.Dtos;

/// <summary>
/// Represents a student's profile data, intended for display in administrative interfaces.
/// </summary>
/// <param name="Id">The unique identifier for the student.</param>
/// <param name="Name">The full name of the student.</param>
/// <param name="Email">The email address of the student.</param>
/// <param name="SubscriptionStatus">The current status of the student's subscription (e.g., "active", "cancelled", "paused").</param>
/// <param name="PlanName">The name of the plan the student is subscribed to (e.g., "Premium Annual Plan").</param>
/// <param name="RegistrationDate">The date and time when the student registered.</param>
public record StudentDto(
    [Required]
    string? Id,

    [Required]
    [StringLength(100)]
    string? Name,

    [Required]
    [EmailAddress]
    string? Email,

    string? SubscriptionStatus,

    string? PlanName,
    
    [Required]
    DateTime? RegistrationDate,
    
    string? SubscriptionId
);

public class PaginatedResult<T>
{
    public List<T> Items { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public long TotalCount { get; set; }
}