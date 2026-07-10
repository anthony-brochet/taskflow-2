using System.ComponentModel.DataAnnotations;

namespace TaskFlow.Models;

// [Display(Name=...)] sur chaque membre = le texte affiché par
// Html.GetEnumSelectList<PriorityLevel>() et @Html.DisplayFor(... Priority).
// Les valeurs numériques explicites (0,1,2) fixent le stockage en base : ne jamais
// les réordonner sous peine de corrompre les données existantes.
public enum PriorityLevel
{
    [Display(Name = "Basse")]
    Low = 0,
    [Display(Name = "Moyenne")]
    Medium = 1,
    [Display(Name = "Élevée")]
    High = 2,
}
