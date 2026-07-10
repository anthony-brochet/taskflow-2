using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.Controllers;

// CRUD classique en MVC, désormais FIN : la logique est dans ICategoryService.
// Le motif récurrent à mémoriser :
//   - chaque écran d'édition = DEUX actions du même nom :
//       * la version GET AFFICHE le formulaire ;
//       * la version POST TRAITE les données envoyées.
//   - après un POST qui modifie la base, on REDIRIGE (Post/Redirect/Get) : ça
//     évite qu'un F5 ne renvoie deux fois le formulaire.
[Authorize]
public class CategoryController(ICategoryService categoryService) : Controller
{
    private readonly ICategoryService _categoryService = categoryService;

    // GET /Category
    public async Task<IActionResult> Index() =>
        View(await _categoryService.GetAllWithTasksAsync());

    // GET /Category/Create
    public IActionResult Create() => View();

    // POST /Category/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category category)
    {
        // ModelState.IsValid = résultat des Data Annotations de Category
        // ([Required], [RegularExpression] sur la couleur...).
        if (!ModelState.IsValid) return View(category);

        await _categoryService.CreateAsync(category);

        TempData["SuccessMessage"] = "Catégorie créée avec succès !";
        
        return RedirectToAction(nameof(Index));
    }

    // GET /Category/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        Category? category = await _categoryService.GetByIdAsync(id);
        
        if (category == null) return NotFound();

        return View(category);
    }

    // POST /Category/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Category categoryModified)
    {
        if (id != categoryModified.Id) return BadRequest();
       
        if (!ModelState.IsValid) return View(categoryModified);

        bool updated = await _categoryService.UpdateAsync(id, categoryModified);
        
        if (!updated) return NotFound();

        TempData["SuccessMessage"] = "Catégorie mise à jour avec succès !";
        
        return RedirectToAction(nameof(Index));
    }

    // GET /Category/Delete/5 — page de confirmation.
    public async Task<IActionResult> Delete(int id)
    {
        Category? category = await _categoryService.GetByIdWithTasksAsync(id);
        
        if (category == null) return NotFound();

        return View(category);
    }

    // POST /Category/DeleteConfirmed/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _categoryService.DeleteAsync(id);

        TempData["SuccessMessage"] = "Catégorie supprimée avec succès !";
       
        return RedirectToAction(nameof(Index));
    }
}
