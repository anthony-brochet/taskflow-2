using TaskFlow.Services.Results;

namespace TaskFlow.Services;

/// <summary>Logique métier des pièces jointes (validation, stockage disque + base).</summary>
public interface IAttachmentService
{
    /// <summary>Valide et stocke un fichier (disque + métadonnées en base).</summary>
    Task<UploadResult> UploadAsync(string? userId, int taskId, IFormFile? file);

    /// <summary>Prépare le téléchargement d'une pièce jointe de l'utilisateur (null si introuvable).</summary>
    Task<AttachmentFile?> GetForDownloadAsync(string? userId, int id);

    /// <summary>Supprime une pièce jointe (disque + base). Renvoie l'id de la tâche parente, ou null.</summary>
    Task<int?> DeleteAsync(string? userId, int id);
}
