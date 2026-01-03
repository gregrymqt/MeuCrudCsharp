using System.Collections.Generic;
using System.Threading.Tasks;
using MeuCrudCsharp.Features.Profiles.UserAccount.DTOs;
using MeuCrudCsharp.Models;

namespace MeuCrudCsharp.Features.Profiles.UserAccount.Interfaces
{
    /// <summary>
    /// Defines the contract for a service that manages a user's account.
    /// This includes retrieving profile and subscription information, as well as performing subscription actions.
    /// </summary>
    public interface IUserAccountService
    {
        Task<AvatarUpdateResponse> UpdateProfilePictureAsync(IFormFile file);
    }
}
