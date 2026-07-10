using Microsoft.EntityFrameworkCore;
using TaskFlow.Data;
using TaskFlow.Models;

namespace TaskFlow.Services;

/// <summary>Implémentation de <see cref="ICategoryService"/>.</summary>
public class CategoryService(AppDbContext context) : ICategoryService
{
    private readonly AppDbContext _context = context;

    public async Task<List<Category>> GetAllWithTasksAsync() =>
        await _context.Categories
            .Include(c => c.Tasks)
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();

    public async Task<List<Category>> GetAllAsync() =>
        await _context.Categories
            .OrderBy(c => c.Name)
            .ToListAsync();

    public async Task<Category?> GetByIdAsync(int id) =>
        await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Category?> GetByIdWithTasksAsync(int id) =>
        await _context.Categories
            .Include(c => c.Tasks)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task CreateAsync(Category category)
    {
        _context.Add(category);
        
        await _context.SaveChangesAsync();
    }

    public async Task<bool> UpdateAsync(int id, Category input)
    {
        Category? category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
        
        if (category == null) return false;

        category.Name = input.Name;
        category.Color = input.Color;

        await _context.SaveChangesAsync();
       
        return true;
    }

    public async Task DeleteAsync(int id)
    {
        Category? category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
       
        if (category != null)
        {
            // Rappel : SetNull (AppDbContext) -> les tâches liées deviennent
            // "sans catégorie", elles ne sont PAS supprimées.
            _context.Categories.Remove(category);
           
            await _context.SaveChangesAsync();
        }
    }
}
