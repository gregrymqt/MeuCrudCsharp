using System.ComponentModel.DataAnnotations;

namespace MeuCrudCsharp.Features.Profiles.UserAccount.DTOs
{
    /// <summary>
    /// Represents the essential profile information for a user, suitable for display.
    /// </summary>
    public class UserProfileDto
    {
        /// <summary>
        /// The user's full name.
        /// </summary>
        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        /// <summary>
        /// The user's email address.
        /// </summary>
        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        /// <summary>
        /// The URL for the user's avatar or profile picture. Can be null.
        /// </summary>
        [Url]
        public string? AvatarUrl { get; set; }
    }
}
