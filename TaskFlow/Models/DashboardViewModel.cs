// VIEW MODEL : une classe taillée pour UNE vue, pas pour la base.
//
// Distinction clé en MVC : une "entité" (TodoTask) reflète une table ; un
// "view model" (celui-ci) reflète ce dont un écran a besoin — ici des compteurs
// agrégés + un top 5. On ne passe pas l'entité brute à la vue quand celle-ci a
// besoin de données calculées/combinées : on lui passe un objet dédié.
// C'est l'équivalent propre du "remplir manuellement les Labels/DataGrid d'un
// formulaire de synthèse" en WinForms, mais typé et testable.
namespace TaskFlow.Models;

public class DashboardViewModel
{
    // Statistiques globales des tâches de l'utilisateur connecté.
    public int Total { get; set; }
    public int Completed { get; set; }
    public int InProgress { get; set; }
    public int Late { get; set; }

    // Propriété calculée (get sans set) : recalculée à chaque lecture.
    public int CompletedPercentage
        => Total > 0 ? (int)Math.Round(Completed * 100.0 / Total) : 0;

    // Les 5 tâches les plus urgentes affichées sur le tableau de bord.
    public List<TodoTask> TopTasks { get; set; } = [];
}
