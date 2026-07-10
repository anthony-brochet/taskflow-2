using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Models;

// Entité "Attachment" : une pièce jointe rattachée à une tâche.
// Principe important : le fichier binaire N'est PAS stocké en base. On garde
// en base uniquement les métadonnées (nom, taille, type) ; l'octet réel vit
// dans wwwroot/uploads sur le disque (voir AttachmentController).
public class Attachment
{
    public int Id { get; set; }

    // Nom d'origine côté utilisateur (ex. "rapport.pdf"), affiché et réutilisé
    // au téléchargement.
    [Required]
    public string OriginalName { get; set; } = string.Empty;

    // Nom réel sur le disque : un GUID + extension. On ne réutilise jamais le
    // nom d'origine comme nom de fichier (deux "rapport.pdf" s'écraseraient, et
    // un nom piégé pourrait servir une attaque par traversée de répertoire).
    [Required]
    public string StoredName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long Size { get; set; }

    // Stockage en UTC (voir explication dans Comment.cs).
    public DateTime UploadAt { get; set; } = DateTime.UtcNow;

    // Clé étrangère + navigation vers la tâche parente (relation N pièces -> 1 tâche).
    public int TaskId { get; set; }

    [ScaffoldColumn(false)]
    public TodoTask? Task { get; set; }

    [ScaffoldColumn(false)]
    public string? UserId { get; set; }
}
