using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Models;

// Entité "Comment" : un commentaire rattaché à une tâche.
// Relation 1 tâche -> N commentaires (voir la propriété Task plus bas).
public class Comment
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Le commentaire ne doit pas être vide.")]
    [StringLength(1000)]
    [Display(Name = "Commenter")]
    public string Content { get; set; } = string.Empty;

    // Règle serveur : on stocke TOUJOURS en UTC (DateTime.UtcNow) et on convertit
    // en heure locale seulement à l'affichage (.ToLocalTime() dans la vue). Sans
    // ça, un changement de fuseau serveur fausse toutes les dates enregistrées.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Clé étrangère + navigation vers la tâche parente (relation N commentaires -> 1 tâche).
    public int TaskId { get; set; }

    [ScaffoldColumn(false)]
    public TodoTask? Task { get; set; }

    [ScaffoldColumn(false)]
    public string? UserId { get; set; }

    // INSTANTANÉ HISTORIQUE assumé : on capture le nom de l'auteur AU MOMENT du
    // commentaire, comme l'auteur d'un commit Git ou le nom client sur une facture.
    // Ce n'est PAS un cache pour éviter une jointure : si l'auteur change d'e-mail
    // ensuite, on veut justement garder le nom tel qu'il était à la rédaction.
    // (À distinguer de l'assignation d'une tâche, qui est un ÉTAT COURANT et est,
    // elle, résolue à la lecture via une navigation — voir TodoTask.AssignedToUser.)
    [ScaffoldColumn(false)]
    public string? UserName { get; set; }
}
