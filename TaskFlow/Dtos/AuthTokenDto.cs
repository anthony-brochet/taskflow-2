namespace TaskFlow.Dtos;

/// <summary>Jeton JWT produit par le service d'authentification.</summary>
/// <param name="Token">Chaîne du jeton **JWT** signé.</param>
/// <param name="ExpiresAtUtc">Date et heure d'expiration du jeton, en **UTC**.</param>
public record AuthTokenDto(string Token, DateTime ExpiresAtUtc);
