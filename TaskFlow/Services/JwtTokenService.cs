using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Dtos;

namespace TaskFlow.Services;

/// <summary>
/// Implémentation de <see cref="IJwtTokenService"/> : construit un JWT signé
/// (HMAC-SHA256) avec les claims d'identité de l'utilisateur.
/// </summary>
public class JwtTokenService(IConfiguration config) : IJwtTokenService
{
    private readonly IConfiguration _config = config;

    // Durée de validité centralisée ici (une seule source de vérité), au lieu
    // d'être recopiée à plusieurs endroits comme avant dans le contrôleur.
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    public AuthTokenDto CreateToken(IdentityUser user)
    {
        // La clé DOIT être identique à celle qui valide le token (Program.cs).
        // On échoue vite si elle manque : jamais de secret codé en dur.
        var secretKey = _config["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("Configuration 'Jwt:SecretKey' manquante.");

        // Claims = les infos d'identité embarquées (et signées) dans le token.
        // Ne JAMAIS y mettre de données sensibles : le contenu est lisible.
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),   // l'id (GUID) de l'utilisateur
            new(ClaimTypes.Email, user.Email ?? ""),
            new(ClaimTypes.Name, user.UserName ?? "")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var expiresAt = DateTime.UtcNow.Add(TokenLifetime);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new AuthTokenDto(tokenString, expiresAt);
    }
}
