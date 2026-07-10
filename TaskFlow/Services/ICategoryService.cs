using TaskFlow.Models;

namespace TaskFlow.Services;

/// <summary>Logique métier des catégories (CRUD).</summary>
public interface ICategoryService
{
    /// <summary>Catégories triées, avec leurs tâches chargées (pour compter côté vue).</summary>
    Task<List<Category>> GetAllWithTasksAsync();

    /// <summary>Catégories triées (pour alimenter les menus déroulants).</summary>
    Task<List<Category>> GetAllAsync();

    Task<Category?> GetByIdAsync(int id);

    /// <summary>Variante avec les tâches chargées (page de confirmation de suppression).</summary>
    Task<Category?> GetByIdWithTasksAsync(int id);

    Task CreateAsync(Category category);

    /// <summary>Met à jour une catégorie. Renvoie false si elle n'existe pas.</summary>
    Task<bool> UpdateAsync(int id, Category input);

    Task DeleteAsync(int id);
}
