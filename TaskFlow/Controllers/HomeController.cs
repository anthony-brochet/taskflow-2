using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.Controllers;

// Pas de [Authorize] ici : la page d'accueil doit rester visible aux visiteurs
// non connectés (elle affiche alors un écran d'invitation à se connecter).
public class HomeController(ITaskService taskService, UserManager<IdentityUser> userManager) : Controller
{
    private readonly ITaskService _taskService = taskService;
    private readonly UserManager<IdentityUser> _userManager = userManager;

    // GET / (route par défaut) — tableau de bord.
    public async Task<IActionResult> Index()
    {
        // Visiteur non connecté : modèle vide -> la vue affiche l'écran "connectez-vous".
        if (!User.Identity!.IsAuthenticated)
        {
            return View(new DashboardViewModel());
        }

        // Toute la logique d'agrégation (compteurs + top 5) est dans le service.
        var vm = await _taskService.GetDashboardAsync(_userManager.GetUserId(User));
       
        return View(vm);
    }

    // GET /Home/Error — page d'erreur affichée par app.UseExceptionHandler("/Home/Error")
    // en production. [ResponseCache] à zéro : on ne met jamais une page d'erreur en cache.
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
