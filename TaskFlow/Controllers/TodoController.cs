using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.Controllers;

// Rappel WinForms -> ASP.NET :
//   Un Controller MVC joue le rôle du code-behind d'un formulaire (Form1.cs).
//   Chaque méthode publique (une "action") ressemble à un gestionnaire
//   d'événement (button_Click) : elle reçoit une requête, fait le travail,
//   puis renvoie une réponse (une vue HTML, une redirection, un 404...).
//
// ⚠️ NOUVELLE ARCHITECTURE : ce contrôleur est désormais FIN. Il ne contient
// plus de requêtes EF Core ni de logique métier : tout ça vit dans ITaskService.
// Le contrôleur ne fait que du HTTP -> il traduit une requête en appel de service,
// puis le résultat du service en réponse (View / Redirect / NotFound).
// Il dépend de l'INTERFACE ITaskService, pas d'une classe concrète : c'est
// l'inversion de dépendance (couplage faible, testable en isolant le service).
//
// [Authorize] sur la classe = toutes les actions exigent d'être connecté.
[Authorize]
public class TodoController(
    ITaskService taskService,
    ICategoryService categoryService,
    UserManager<IdentityUser> userManager) : Controller
{
    private readonly ITaskService _taskService = taskService;
    private readonly ICategoryService _categoryService = categoryService;
    private readonly UserManager<IdentityUser> _userManager = userManager;

    private const int PageSize = 10;

    // GET: /Todo/
    public async Task<IActionResult> Index(
        int? categoryId, string? filter, string? search, int pageNumber = 1)
    {
        string? userId = _userManager.GetUserId(User);

        var paginatedTasks = await _taskService.GetPagedAsync(
            userId, categoryId, filter, search, pageNumber, PageSize);

        // ViewBag = données secondaires (menu déroulant + état des filtres) que
        // la vue réutilise pour conserver la sélection courante.
        ViewBag.Categories = new SelectList(await _categoryService.GetAllAsync(), "Id", "Name", categoryId);
        ViewBag.CategoryId = categoryId;
        ViewBag.Filter = filter;
        ViewBag.Search = search;

        return View(paginatedTasks);
    }

    // GET: /Todo/Create
    public async Task<IActionResult> Create()
    {
        await PopulateDropdownsAsync();
        
        return View();
    }

    // POST: /Todo/Create
    // [Bind] = liste blanche des champs que le formulaire a le droit de remplir.
    // On exclut Id, UserId, IsCompleted : contrôlés par le serveur. Parade contre
    // l'"over-posting".
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Title,Description,Priority,CategoryId,DueDate,AssignedToUserId")] TodoTask task)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            
            return View(task);
        }

        await _taskService.CreateAsync(_userManager.GetUserId(User), task);

        TempData["SuccessMessage"] = "Tâche créée avec succès !";
        
        return RedirectToAction(nameof(Index));
    }

    // GET: /Todo/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        TodoTask? task = await _taskService.GetForEditAsync(_userManager.GetUserId(User), id);
        
        if (task == null) return NotFound();

        await PopulateDropdownsAsync(task.CategoryId);
        
        return View(task);
    }

    // POST: /Todo/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TodoTask taskModified)
    {
        if (id != taskModified.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(taskModified.CategoryId);
            
            return View(taskModified);
        }

        bool updated = await _taskService.UpdateAsync(_userManager.GetUserId(User), id, taskModified);
        
        if (!updated) return NotFound();

        TempData["SuccessMessage"] = "Tâche mise à jour avec succès !";
        
        return RedirectToAction(nameof(Index));
    }

    // GET: /Todo/Delete/5 (page de confirmation)
    public async Task<IActionResult> Delete(int id)
    {
        TodoTask? task = await _taskService.GetForEditAsync(_userManager.GetUserId(User), id);
        
        if (task == null) return NotFound();

        return View(task);
    }

    // POST: /Todo/DeleteConfirmed/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _taskService.DeleteAsync(_userManager.GetUserId(User), id);

        TempData["SuccessMessage"] = "Tâche supprimée avec succès !";
        
        return RedirectToAction(nameof(Index));
    }

    // POST: /Todo/ToggleCompletion/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCompletion(int id)
    {
        bool? newState = await _taskService.ToggleAsync(_userManager.GetUserId(User), id);
       
        if (newState == null) return NotFound();

        return RedirectToAction(nameof(Index));
    }

    // GET: /Todo/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var task = await _taskService.GetDetailsAsync(_userManager.GetUserId(User), id);
        
        if (task == null) return NotFound();

        return View(task);
    }

    // --- Méthode privée utilitaire : remplit les menus déroulants des formulaires. ---
    // Construire une SelectList est une préoccupation d'AFFICHAGE : elle reste
    // donc dans le contrôleur, alimentée par les données que fournit le service.
    private async Task PopulateDropdownsAsync(int? selectedCategoryId = null)
    {
        ViewBag.Categories = new SelectList(
            await _categoryService.GetAllAsync(), "Id", "Name", selectedCategoryId);

        var users = await _userManager.Users
            .OrderBy(u => u.Email)
            .Select(u => new { u.Id, u.Email })
            .ToListAsync();

        ViewBag.Users = new SelectList(users, "Id", "Email");
    }
}
