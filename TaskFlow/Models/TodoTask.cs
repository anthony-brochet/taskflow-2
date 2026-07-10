using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace TaskFlow.Models;

// ENTITÉ EF Core — pont WinForms/WPF -> ASP.NET Core :
//   En WinForms, une classe métier ne "savait" rien de la base : tu la
//   remplissais toi-même via un DataReader/DataAdapter. Ici, cette classe EST
//   la table : EF Core génère la table "TodoTasks" à partir d'elle (voir les
//   migrations) et fait le mapping objet <-> lignes SQL automatiquement.
//
// Les attributs [Required], [StringLength]... (Data Annotations) jouent double
// rôle : ils configurent le schéma SQL ET servent de règles de validation
// côté serveur (ModelState.IsValid dans les contrôleurs) et côté navigateur.
// C'est l'équivalent centralisé de ce que tu codais à la main dans les
// événements Validating de tes contrôles WinForms.
public class TodoTask
{
    // Convention EF Core : une propriété "Id" (ou "TodoTaskId") devient la clé
    // primaire auto-incrémentée. Aucune configuration nécessaire.
    public int Id { get; set; }

    // Clé étrangère vers Categories. Nullable => une tâche PEUT ne pas avoir
    // de catégorie (voir la règle OnDelete SetNull dans AppDbContext).
    [Display(Name = "Catégorie")]
    public int? CategoryId { get; set; }

    // Propriété de navigation : EF la remplit sur demande via .Include(t => t.Category).
    // Non chargée par défaut (pas de lazy loading ici) -> null si on n'a pas Include.
    public Category? Category { get; set; }

    // Propriétaire de la tâche (Id de l'utilisateur Identity). [ScaffoldColumn(false)]
    // dit aux tag helpers de NE PAS générer de champ pour ça dans les formulaires :
    // c'est le serveur qui le remplit, jamais l'utilisateur.
    [ScaffoldColumn(false)]
    public string? UserId { get; set; }

    // [Display(Name=...)] est le libellé repris automatiquement par <label asp-for="Title">
    // dans les vues. Un seul endroit à changer pour renommer le champ partout.
    [Required(ErrorMessage = "Le titre est requis.")]
    [StringLength(200, ErrorMessage = "Le titre ne peut pas dépasser 200 caractères.")]
    [Display(Name = "Titre")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Priorité")]
    public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;

    [Display(Name = "Tâche terminée")]
    public bool IsCompleted { get; set; } = false;

    // [DataType(Date)] génère un <input type="date"> (sélecteur sans heure) côté vue.
    [Required(ErrorMessage = "La date d'échéance est requise.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date d'échéance")]
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(7);

    // Relations "1 tâche -> N ...". Initialisées à vide pour éviter les NullReference
    // quand la vue itère dessus avant tout Include.
    public ICollection<Comment> Comments { get; set; } = [];

    public ICollection<Attachment> Attachments { get; set; } = [];

    // Assignation à un autre utilisateur : on stocke UNIQUEMENT la clé étrangère
    // (l'Id, stable), pas l'e-mail (volatil). L'e-mail est résolu à la LECTURE via
    // la navigation ci-dessous — pas de donnée dupliquée qui pourrait devenir périmée
    // si l'utilisateur change d'adresse. La jointure se fait sur la clé primaire
    // d'AspNetUsers : c'est indexé, donc bon marché.
    [ScaffoldColumn(false)]
    [Display(Name = "Assignée à")]
    public string? AssignedToUserId { get; set; }

    // Propriété de navigation vers l'utilisateur assigné (IdentityUser = table
    // AspNetUsers). Remplie par EF via .Include(t => t.AssignedToUser) ; la relation
    // et sa règle de suppression sont configurées dans AppDbContext.
    [ScaffoldColumn(false)]
    public IdentityUser? AssignedToUser { get; set; }
}
