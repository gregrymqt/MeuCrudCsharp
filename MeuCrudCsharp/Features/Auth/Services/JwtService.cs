namespace MeuCrudCsharp.Features.Auth.Services;

using Models; // Substitua pelo seu namespace
using Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

public class JwtService : IJwtService
{
    private readonly UserManager<Users> _userManager;
    private readonly JwtSettings _jwtSettings; // Sua classe de configurações
    private readonly ILogger<JwtService> _logger;

    public JwtService(
        UserManager<Users> userManager,
        IOptions<JwtSettings> jwtSettings,
        ILogger<JwtService> logger)
    {
        _userManager = userManager;
        _jwtSettings = jwtSettings.Value;
        _logger = logger;
    }

    // A lógica de geração de token foi movida para cá.
    public async Task<string> GenerateJwtTokenAsync(Users user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user), "Usuário não pode ser nulo.");

        var userRoles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, user.Name ?? string.Empty),
            new Claim("AvatarUrl", user.AvatarUrl ?? string.Empty),
        };

        foreach (var role in userRoles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        if (string.IsNullOrEmpty(_jwtSettings.Key))
            throw new InvalidOperationException("A chave JWT (JwtSettings.Key) não foi configurada.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(8);

        var token = new JwtSecurityToken(null, null, claims, null,expires, creds);

        _logger.LogInformation("Token JWT gerado para o usuário {UserId}", user.Id);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}