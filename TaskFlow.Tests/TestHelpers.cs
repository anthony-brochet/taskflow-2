using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using TaskFlow.Data;

namespace TaskFlow.Tests;

// Fabriques partagées par tous les tests : centralisées ici pour éviter de
// recopier la même plomberie (base InMemory, mock UserManager, contexte HTTP)
// dans chaque fichier de test.
internal static class TestHelpers
{
    // Une base EF Core "InMemory" JETABLE : un nom unique par appel garantit
    // que deux tests ne partagent jamais leurs données (isolation).
    public static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        return new AppDbContext(options);
    }

    // UserManager est une classe concrète au constructeur lourd : on la SIMULE
    // avec Moq. On configure juste GetUserId(...) pour renvoyer l'id voulu.
    public static UserManager<IdentityUser> CreateMockUserManager(string userId)
    {
        var store = new Mock<IUserStore<IdentityUser>>();

        var userManager = new Mock<UserManager<IdentityUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        userManager.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(userId);
        userManager.Setup(u => u.GetUserName(It.IsAny<ClaimsPrincipal>())).Returns($"{userId}@test.local");

        return userManager.Object;
    }

    // Attache un utilisateur authentifié + un TempData au contrôleur, pour que
    // User / TempData ne soient pas null pendant le test.
    public static void SetUser(Controller controller, string userId)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId)], "TestAuth");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        controller.TempData = new TempDataDictionary(
            controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());
    }

    // Variante pour un ControllerBase d'API (pas de TempData).
    public static void SetUser(ControllerBase controller, string userId)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId)], "TestAuth");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }
}
