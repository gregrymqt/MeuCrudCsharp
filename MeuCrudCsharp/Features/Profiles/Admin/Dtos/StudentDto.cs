using System;
using System.ComponentModel.DataAnnotations;

namespace MeuCrudCsharp.Features.Profiles.Admin.Dtos
{
    /// <summary>
    /// Represents a student's profile data, intended for display in administrative interfaces.
    /// </summary>
    public class StudentDto
    {
        /// <summary>
        /// The unique identifier for the student.
        /// </summary>
        [Required]
        public Guid Id { get; set; }

        /// <summary>
        /// The full name of the student.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        /// <summary>
        /// The email address of the student.
        /// </summary>
        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        /// <summary>
        /// The current status of the student's subscription (e.g., "active", "cancelled", "paused").
        /// </summary>
        public string? SubscriptionStatus { get; set; }

        /// <summary>
        /// The name of the plan the student is subscribed to (e.g., "Premium Annual Plan").
        /// </summary>
        public string? PlanName { get; set; }

        /// <summary>
        /// The date and time when the student registered.
        /// </summary>
        [Required]
        public DateTime RegistrationDate { get; set; }
    }
}
