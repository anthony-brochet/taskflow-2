using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.Helpers;
using TaskFlow.Models;

namespace TaskFlow.Services;

/// <summary>
/// Implémentation de <see cref="ITaskService"/>. C'est ici que vit désormais
/// la logique auparavant dispersée dans TodoController et HomeController :
/// requêtes EF Core, filtres, tri, pagination, règles de sécurité.
/// </summary>
public class TaskService(AppDbContext context) : ITaskService
{
    private readonly AppDbContext _context = context;

    public async Task<PaginatedList<TodoTask>> GetPagedAsync(
        string? userId, int? categoryId, string? filter, string? search,
        int pageNumber, int pageSize)
    {
        // IQueryable = requête différée : on empile les filtres, EF n'exécute
        // le SQL qu'au CreateAsync final (Count + Skip/Take).
        IQueryable<TodoTask> query = _context.TodoTasks
            .Where(t => t.UserId == userId)     // sécurité : uniquement SES tâches
            .Include(t => t.Category)           // pastille de couleur
            .Include(t => t.Comments)           // compteur de commentaires
            .Include(t => t.AssignedToUser)     // e-mail de l'assigné (résolu à la lecture)
            .AsNoTracking();                    // lecture seule -> plus rapide

        if (categoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == categoryId.Value);
        }

        query = filter switch
        {
            "inprogress" => query.Where(t => !t.IsCompleted),
            "completed" => query.Where(t => t.IsCompleted),
            "high" => query.Where(t => t.Priority == PriorityLevel.High),
            _ => query
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t =>
                t.Title.Contains(search) ||
                (t.Description != null && t.Description.Contains(search)));
        }

        // Tri appliqué AVANT la pagination : sinon Skip/Take est non déterministe.
        query = query
            .OrderBy(t => t.IsCompleted)
            .ThenBy(t => t.DueDate);

        return await PaginatedList<TodoTask>.CreateAsync(query, pageNumber, pageSize);
    }

    public async Task<TodoTask?> GetForEditAsync(string? userId, int id) =>
        await _context.TodoTasks
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Id == id);

    public async Task<TodoTask?> GetDetailsAsync(string? userId, int id) =>
        await _context.TodoTasks
            .Include(t => t.Category)
            .Include(t => t.Comments.OrderByDescending(c => c.CreatedAt))
            .Include(t => t.Attachments.OrderByDescending(a => a.UploadAt))
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

    public async Task<bool> ExistsForUserAsync(string? userId, int taskId) =>
        await _context.TodoTasks.AnyAsync(t => t.Id == taskId && t.UserId == userId);

    public async Task CreateAsync(string? userId, TodoTask task)
    {
        // Champ contrôlé par le serveur (jamais depuis le client).
        task.UserId = userId;

        _context.Add(task);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> UpdateAsync(string? userId, int id, TodoTask input)
    {
        // On recharge l'entité SUIVIE par EF en vérifiant l'appartenance (anti-IDOR),
        // puis on recopie uniquement les champs éditables.
        TodoTask? task = await _context.TodoTasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task == null) return false;

        task.Title = input.Title;
        task.Description = input.Description;
        task.Priority = input.Priority;
        task.IsCompleted = input.IsCompleted;
        task.DueDate = input.DueDate;
        task.CategoryId = input.CategoryId;
        task.AssignedToUserId = input.AssignedToUserId;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(string? userId, int id)
    {
        TodoTask? task = await _context.TodoTasks
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (task == null) return false;

        _context.TodoTasks.Remove(task);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool?> ToggleAsync(string? userId, int id)
    {
        TodoTask? task = await _context.TodoTasks
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Id == id);

        if (task == null) return null;

        task.IsCompleted = !task.IsCompleted;
        await _context.SaveChangesAsync();
        return task.IsCompleted;
    }

    public async Task<DashboardViewModel> GetDashboardAsync(string? userId)
    {
        // Un seul aller-retour SQL ; les compteurs et le top 5 sont calculés
        // en mémoire (suffisant sur un petit volume).
        List<TodoTask> tasks = await _context.TodoTasks
            .Where(t => t.UserId == userId)
            .AsNoTracking()
            .ToListAsync();

        return new DashboardViewModel
        {
            Total = tasks.Count,
            Completed = tasks.Count(t => t.IsCompleted),
            InProgress = tasks.Count(t => !t.IsCompleted && t.DueDate >= DateTime.Today),
            Late = tasks.Count(t => !t.IsCompleted && t.DueDate < DateTime.Today),
            TopTasks = tasks
                .Where(t => !t.IsCompleted)
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.DueDate)
                .Take(5)
                .ToList()
        };
    }
}
