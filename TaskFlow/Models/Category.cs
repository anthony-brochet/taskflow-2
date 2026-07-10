using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Models;

// Une "entité" EF Core = l'équivalent d'une classe métier en WinForms/WPF,
// sauf qu'ici chaque instance correspond à une ligne de la table SQL "Categories".
// C'est Entity Framework (le DbContext) qui fait le pont objet <-> base de données,
// là où en WinForms on écrivait souvent le SQL à la main (SqlCommand / DataAdapter).
public class Category
{
    // Convention EF Core : une propriété nommée "Id" devient automatiquement
    // la clé primaire, auto-incrémentée par la base. Rien à configurer.
    public int Id { get; set; }

    [Required(ErrorMessage = "Le nom de la catégorie est requis.")]
    [StringLength(50)]
    [Display(Name = "Nom de la catégorie")]
    public string Name { get; set; } = string.Empty;

    // La couleur est réinjectée telle quelle dans le HTML des vues
    // (style="background-color: ...").
    // Sans validation, un utilisateur malveillant pourrait y glisser du code
    // et provoquer une faille XSS. Le [RegularExpression] impose un code
    // hexadécimal strict (#RGB ou #RRGGBB), ce qui ferme la porte à l'injection.
    [Required]
    [RegularExpression("^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})$",
        ErrorMessage = "La couleur doit être un code hexadécimal valide, ex : #3b5bdb.")]
    [Display(Name = "Couleur")]
    public string Color { get; set; } = "#3b5bdb";

    // Propriété de navigation : la liste des tâches liées à cette catégorie.
    // EF Core la remplit quand on fait un .Include(c => c.Tasks).
    // C'est l'équivalent objet d'une relation "1 catégorie -> N tâches".
    public ICollection<TodoTask> Tasks { get; set; } = [];
}
