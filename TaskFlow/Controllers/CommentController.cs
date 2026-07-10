using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.Controllers;

// Contrôleur FIN : la logique (sécurité + persistance) est dans ICommentService.
// Il n'a pas de vue à lui : ses actions traitent les formulaires de Todo/Details
// puis redirigent vers cette même page (Post/Redirect/Get).
[Authorize]
public class CommentController(
    ICommentService commentService, UserManager<IdentityUser> userManager) : Controller
{
    private readonly ICommentService _commentService = commentService;
    private readonly UserManager<IdentityUser> _userManager = userManager;

    // POST /Comment/Add
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(Comment comment)
    {
        // Validation (commentaire vide) : préoccupation HTTP, gérée ici.
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Le commentaire ne peut-être vide !";
           
            return RedirectToAction("Details", "Todo", new { id = comment.TaskId });
        }

        var added = await _commentService.AddAsync(
            _userManager.GetUserId(User), _userManager.GetUserName(User), comment);

        // false = la tâche visée n'existe pas / n'appartient pas à l'utilisateur.
        if (!added) return NotFound();

        return RedirectToAction("Details", "Todo", new { id = comment.TaskId });
    }

    // POST /Comment/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var taskId = await _commentService.DeleteAsync(_userManager.GetUserId(User), id);
       
        if (taskId == null) return NotFound();

        return RedirectToAction("Details", "Todo", new { id = taskId });
    }
}
