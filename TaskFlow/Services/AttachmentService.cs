using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.Models;
using TaskFlow.Services.Results;

namespace TaskFlow.Services;

/// <summary>Implémentation de <see cref="IAttachmentService"/>.</summary>
/// <remarks>
/// Injecte <see cref="IWebHostEnvironment"/> pour connaître le chemin physique de
/// wwwroot (jamais de chemin codé en dur), et <see cref="ITaskService"/> pour
/// vérifier l'appartenance de la tâche cible.
/// </remarks>
public class AttachmentService(
    AppDbContext context, IWebHostEnvironment env, ITaskService taskService) : IAttachmentService
{
    private readonly AppDbContext _context = context;
    private readonly IWebHostEnvironment _env = env;
    private readonly ITaskService _taskService = taskService;

    private const long MaxFileSize = 10 * 1024 * 1024; // 10 Mo

    // Liste blanche : on n'autorise QUE ces types (plutôt que d'essayer
    // d'interdire les dangereux, qu'on ne peut pas tous prévoir).
    private static readonly string[] AllowedExtensions =
        [".pdf", ".doc", ".docx", ".xlsx", ".xls", ".png", ".jpg", ".jpeg", ".txt"];

    public async Task<UploadResult> UploadAsync(string? userId, int taskId, IFormFile? file)
    {
        // Sécurité : la tâche cible doit exister ET appartenir à l'utilisateur.
        if (!await _taskService.ExistsForUserAsync(userId, taskId))
            return new UploadResult(false, "Tâche introuvable.", taskId);

        // Validations d'entrée : on ne fait confiance à rien de ce qui vient du client.
        if (file == null || file.Length == 0)
            return new UploadResult(false, "Veuillez choisir un fichier", taskId);

        if (file.Length > MaxFileSize)
            return new UploadResult(false, "Le fichier est trop volumineux. Maximum 10 mb.", taskId);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
       
        if (!AllowedExtensions.Contains(extension))
            return new UploadResult(false, "Type de fichier non autorisé.", taskId);

        // Nom de stockage = GUID + extension. On n'utilise jamais le nom fourni
        // par l'utilisateur comme nom sur disque (collisions + traversée de chemin).
        var storedName = $"{Guid.NewGuid()}{extension}";
        var uploadFolder = Path.Combine(_env.WebRootPath, "uploads");
        var filePath = Path.Combine(uploadFolder, storedName);

        Directory.CreateDirectory(uploadFolder); // no-op si le dossier existe déjà

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // En base : seulement les métadonnées, pas les octets.
        _context.Attachments.Add(new Attachment
        {
            OriginalName = file.FileName,
            StoredName = storedName,
            ContentType = file.ContentType,
            Size = file.Length,
            UploadAt = DateTime.UtcNow,
            TaskId = taskId,
            UserId = userId
        });
       
        await _context.SaveChangesAsync();

        return new UploadResult(true, null, taskId);
    }

    public async Task<AttachmentFile?> GetForDownloadAsync(string? userId, int id)
    {
        var attachment = await _context.Attachments
            .Include(a => a.Task)
            .FirstOrDefaultAsync(a => a.Id == id && a.Task!.UserId == userId);

        if (attachment == null) return null;

        var filePath = Path.Combine(_env.WebRootPath, "uploads", attachment.StoredName);
        
        if (!File.Exists(filePath)) return null;

        return new AttachmentFile(filePath, attachment.ContentType, attachment.OriginalName);
    }

    public async Task<int?> DeleteAsync(string? userId, int id)
    {
        var attachment = await _context.Attachments
            .Include(a => a.Task)
            .FirstOrDefaultAsync(a => a.Id == id && a.Task!.UserId == userId);

        if (attachment == null) return null;

        var taskId = attachment.TaskId;
        var filePath = Path.Combine(_env.WebRootPath, "uploads", attachment.StoredName);

        // On supprime les DEUX : le fichier sur disque ET la ligne en base.
        if (File.Exists(filePath))
            File.Delete(filePath);

        _context.Attachments.Remove(attachment);
        
        await _context.SaveChangesAsync();
       
        return taskId;
    }
}
