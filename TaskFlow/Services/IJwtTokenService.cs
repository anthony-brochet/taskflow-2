using Microsoft.AspNetCore.Identity;
using TaskFlow.Dtos;

namespace TaskFlow.Services;

// ===================== La couche de SERVICES =====================
//
// Rôle : sortir la LOGIQUE MÉTIER des contrôleurs. Un contrôleur ne devrait
// gérer que le HTTP (lire la requête, choisir la réponse) ; le « quoi faire »
// (requêtes base, règles, calculs, sécurité) vit dans un service.
//
// Pourquoi une INTERFACE (IJwtTokenService) et pas seulement la classe ?
//   - Découplage : le contrôleur dépend du CONTRAT (l'interface), pas de
//     l'implémentation. On peut changer l'implémentation sans toucher au
//     contrôleur (inversion de dépendance — le "D" de SOLID).
//   - Testabilité : dans un test, on injecte un faux (mock) de l'interface.
//   - Standard ASP.NET Core : on enregistre le couple interface→implémentation
//     dans le conteneur DI (Program.cs : AddScoped<IJwtTokenService, JwtTokenService>()).

/// <summary>Fabrique des jetons JWT pour authentifier les appels à l'API.</summary>
public interface IJwtTokenService
{
    /// <summary>Crée un JWT signé pour l'utilisateur donné.</summary>
    AuthTokenDto CreateToken(IdentityUser user);
}
