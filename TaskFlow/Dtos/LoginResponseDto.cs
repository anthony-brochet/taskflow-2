namespace TaskFlow.Dtos;

/// <summary>Réponse de l'endpoint <c>POST /api/AuthApi/login</c>.</summary>
/// <param name="Token">Jeton **JWT** à placer dans l'en-tête <c>Authorization: Bearer &lt;token&gt;</c>.</param>
/// <param name="Email">Adresse e-mail de l'utilisateur authentifié.</param>
/// <param name="UserId">Identifiant unique de l'utilisateur.</param>
/// <param name="ExpiresAtUtc">Date et heure d'expiration du jeton, en **UTC**.</param>
public record LoginResponseDto(
    string Token,
    string? Email,
    string UserId,
    DateTime ExpiresAtUtc);
