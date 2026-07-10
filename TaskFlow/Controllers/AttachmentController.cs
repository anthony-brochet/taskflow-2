using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using TaskFlow.Services;

namespace TaskFlow.Controllers;

// Contrôleur FIN : toute la logique (validation, écriture disque, base) est dans
// IAttachmentService. Le contrôleur ne fait que relayer requête -> service ->
// réponse HTTP (redirection, fichier, 404).
[Authorize]
public class AttachmentController(
    IAttachmentService attachmentService, UserManager<IdentityUser> userManager) : Controller
{
    private readonly IAttachmentService _attachmentService = attachmentService;
    private readonly UserManager<IdentityUser> _userManager = userManager;

    // POST /Attachment/Upload
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file, int taskId)
    {
        var result = await _attachmentService.UploadAsync(_userManager.GetUserId(User), taskId, file);

        TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] =
            result.Success ? "Le fichier a bien été envoyé." : result.ErrorMessage;

        return RedirectToAction("Details", "Todo", new { id = taskId });
    }

    // GET /Attachment/Download/5
    public async Task<IActionResult> Download(int id)
    {
        var file = await _attachmentService.GetForDownloadAsync(_userManager.GetUserId(User), id);
       
        if (file == null) return NotFound();

        // PhysicalFile = renvoie un fichier du disque comme réponse HTTP, en
        // fixant le type MIME et le nom de téléchargement proposé.
        return PhysicalFile(file.PhysicalPath, file.ContentType, file.DownloadName);
    }

    // POST /Attachment/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var taskId = await _attachmentService.DeleteAsync(_userManager.GetUserId(User), id);
        
        if (taskId == null) return NotFound();

        return RedirectToAction("Details", "Todo", new { id = taskId });
    }
}
