using System.ComponentModel.DataAnnotations;
using TaskFlow.Models;

namespace TaskFlow.Dtos;

/// <summary>Données acceptées en entrée pour créer ou mettre à jour une tâche.</summary>
/// <remarks>
/// Volontairement DÉPOURVU de Id/UserId : c'est le serveur qui les fixe. C'est
/// l'équivalent, pour l'API, du <c>[Bind(...)]</c> des formulaires MVC : la parade
/// à l'« over-posting » (un client qui écrirait des champs sensibles).
///
/// ⚠️ Sur un `record`, les attributs de validation se posent SUR LE PARAMÈTRE du
/// constructeur (écriture <c>[Required]</c>), PAS sur la propriété générée
/// (<c>[property: Required]</c>). Sinon MVC lève une exception au binding :
/// « validation metadata must be associated with the constructor parameter ».
/// </remarks>
/// <param name="Title">Titre de la tâche. **Requis**, 200 caractères maximum.</param>
/// <param name="Description">Description libre. Optionnelle, 1000 caractères maximum.</param>
/// <param name="Priority">Niveau de priorité : <c>Low</c>, <c>Medium</c> ou <c>High</c>.</param>
/// <param name="IsCompleted"><c>true</c> si la tâche est déjà terminée à la création.</param>
/// <param name="DueDate">Date d'échéance (format ISO 8601, ex. <c>2026-07-09T00:00:00Z</c>).</param>
/// <param name="CategoryId">Identifiant de la catégorie rattachée. <c>null</c> = aucune catégorie.</param>
public record SaveTaskDto(
    [Required(ErrorMessage = "Le titre est requis.")]
    [StringLength(200)]
    string Title,
    [StringLength(1000)]
    string? Description,
    PriorityLevel Priority,
    bool IsCompleted,
    DateTime DueDate,
    int? CategoryId);
