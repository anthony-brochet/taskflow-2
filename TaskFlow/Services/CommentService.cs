using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.Models;

namespace TaskFlow.Services;

/// <summary>Implémentation de <see cref="ICommentService"/>.</summary>
/// <remarks>
/// Dépend de <see cref="ITaskService"/> pour vérifier l'appartenance de la tâche :
/// on réutilise <c>ExistsForUserAsync</c> au lieu de redupliquer la requête. C'est
/// la composition de services par injection de dépendances (un service peut en
/// consommer un autre).
/// </remarks>
public class CommentService(AppDbContext context, ITaskService taskService) : ICommentService
{
    private readonly AppDbContext _context = context;
    private readonly ITaskService _taskService = taskService;

    public async Task<bool> AddAsync(string? userId, string? userName, Comment comment)
    {
        // Sécurité : on ne commente que SA propre tâche.
        if (!await _taskService.ExistsForUserAsync(userId, comment.TaskId))
            return false;

        // Champs calculés côté serveur (jamais depuis le formulaire).
        comment.UserId = userId;
        comment.UserName = userName;
        comment.CreatedAt = DateTime.UtcNow; // stockage UTC, converti à l'affichage

        _context.Comments.Add(comment);
        
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<int?> DeleteAsync(string? userId, int commentId)
    {
        // Le filtre UserId retrouve le commentaire ET garantit qu'on ne supprime
        // que le sien.
        var comment = await _context.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.UserId == userId);

        if (comment == null) return null;

        var taskId = comment.TaskId;

        _context.Comments.Remove(comment);

        await _context.SaveChangesAsync();
        
        return taskId;
    }
}
