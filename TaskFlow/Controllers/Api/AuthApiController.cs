using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Dtos;
using TaskFlow.Services;

namespace TaskFlow.Controllers.Api;

// Contrôleur d'authentification de l'API. PAS de [Authorize] : le login doit
// rester accessible sans être déjà connecté (sinon on ne pourrait jamais
// obtenir de token). La fabrication du JWT est déléguée à IJwtTokenService.
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthApiController(
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
    IJwtTokenService jwtTokenService) : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager = userManager;
    private readonly SignInManager<IdentityUser> _signInManager = signInManager;
    private readonly IJwtTokenService _jwtTokenService = jwtTokenService;

    /// <summary>Authentifie un utilisateur et renvoie un jeton JWT.</summary>
    /// <remarks>
    /// Renvoie un jeton **Bearer** à placer ensuite dans l'en-tête
    /// <c>Authorization: Bearer &lt;token&gt;</c> de chaque appel protégé.
    /// **Aucune authentification préalable requise** pour cet endpoint.
    /// </remarks>
    /// <param name="request">Identifiants de connexion : <c>Email</c> et <c>Password</c>.</param>
    /// <response code="200">Authentification réussie : jeton, e-mail, identifiant et date d'expiration UTC.</response>
    /// <response code="401">Identifiants incorrects ou compte verrouillé.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        
        if (user == null) return Unauthorized(new { message = "Identifiants incorrect." });

        // lockoutOnFailure: true -> verrouille le compte après trop d'échecs (anti brute-force).
        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
       
        if (!result.Succeeded) return Unauthorized(new { message = "Mot de passe incorrect." });

        var token = _jwtTokenService.CreateToken(user);

        return Ok(new LoginResponseDto(token.Token, user.Email, user.Id, token.ExpiresAtUtc));
    }
}
