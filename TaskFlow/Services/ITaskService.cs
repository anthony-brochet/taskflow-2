using TaskFlow.Helpers;
using TaskFlow.Models;

namespace TaskFlow.Services;

/// <summary>
/// Logique métier des tâches. Toutes les méthodes prennent le <c>userId</c>
/// courant en paramètre et l'appliquent en filtre : c'est ICI qu'est
/// centralisé le cloisonnement par utilisateur (anti-IDOR), plus dans chaque
/// contrôleur. Un contrôleur ne peut donc pas « oublier » ce filtre.
/// </summary>
public interface ITaskService
{
    /// <summary>Liste paginée des tâches de l'utilisateur, avec filtres et recherche.</summary>
    Task<PaginatedList<TodoTask>> GetPagedAsync(
        string? userId, int? categoryId, string? filter, string? search,
        int pageNumber, int pageSize);

    /// <summary>Récupère une tâche de l'utilisateur pour édition (sans ses relations).</summary>
    Task<TodoTask?> GetForEditAsync(string? userId, int id);

    /// <summary>Récupère une tâche avec ses commentaires et pièces jointes (page Détails).</summary>
    Task<TodoTask?> GetDetailsAsync(string? userId, int id);

    /// <summary>Vérifie qu'une tâche existe ET appartient à l'utilisateur (réutilisé par les autres services).</summary>
    Task<bool> ExistsForUserAsync(string? userId, int taskId);

    /// <summary>Crée une tâche (fixe le propriétaire et résout l'utilisateur assigné).</summary>
    Task CreateAsync(string? userId, TodoTask task);

    /// <summary>Met à jour une tâche de l'utilisateur. Renvoie false si elle n'existe pas / n'est pas la sienne.</summary>
    Task<bool> UpdateAsync(string? userId, int id, TodoTask input);

    /// <summary>Supprime une tâche de l'utilisateur (ses commentaires/pièces jointes partent en cascade).</summary>
    Task<bool> DeleteAsync(string? userId, int id);

    /// <summary>Bascule terminé/en cours. Renvoie le nouvel état, ou null si la tâche est introuvable.</summary>
    Task<bool?> ToggleAsync(string? userId, int id);

    /// <summary>Construit le tableau de bord (compteurs + top 5) de l'utilisateur.</summary>
    Task<DashboardViewModel> GetDashboardAsync(string? userId);
}
