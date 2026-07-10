using TaskFlow.Models;

namespace TaskFlow.Services;

/// <summary>Logique métier des commentaires.</summary>
public interface ICommentService
{
    /// <summary>
    /// Ajoute un commentaire à une tâche de l'utilisateur. Renvoie false si la
    /// tâche visée n'existe pas / n'appartient pas à l'utilisateur.
    /// </summary>
    Task<bool> AddAsync(string? userId, string? userName, Comment comment);

    /// <summary>
    /// Supprime un commentaire de l'utilisateur. Renvoie l'id de la tâche parente
    /// (pour la redirection), ou null si le commentaire est introuvable.
    /// </summary>
    Task<int?> DeleteAsync(string? userId, int commentId);
}
