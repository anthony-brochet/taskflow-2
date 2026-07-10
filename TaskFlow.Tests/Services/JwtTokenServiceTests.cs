using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using TaskFlow.Services;

namespace TaskFlow.Tests.Services;

public class JwtTokenServiceTests
{
    // Clé >= 32 octets (256 bits), requis par HMAC-SHA256.
    private const string TestKey = "Cle-De-Test-Uniquement-Suffisamment-Longue-123456";

    private static IConfiguration BuildConfig(string? key) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SecretKey"] = key })
            .Build();

    [Fact]
    public void CreateToken_ProducesReadableTokenWithUserClaims()
    {
        var service = new JwtTokenService(BuildConfig(TestKey));
        var user = new IdentityUser { Id = "user-42", Email = "a@b.c", UserName = "a@b.c" };

        var result = service.CreateToken(user);

        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.True(result.ExpiresAtUtc > DateTime.UtcNow);

        // On relit le token pour vérifier qu'il embarque bien l'id de l'utilisateur.
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == "user-42");
    }

    [Fact]
    public void CreateToken_Throws_WhenSecretKeyMissing()
    {
        var service = new JwtTokenService(BuildConfig(null));
        var user = new IdentityUser { Id = "user-42" };

        Assert.Throws<InvalidOperationException>(() => service.CreateToken(user));
    }
}
