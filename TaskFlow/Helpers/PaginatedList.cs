using Microsoft.EntityFrameworkCore;

namespace TaskFlow.Helpers;

// Liste générique paginée : elle hérite de List<T> (les éléments de LA page
// courante) et ajoute les métadonnées de pagination (page actuelle, nombre
// total de pages...). Générique <T> = réutilisable pour n'importe quelle
// entité (tâches, catégories...), comme un contrôle réutilisable en WinForms.
public class PaginatedList<T> : List<T>
{
    public int PageIndex { get; private set; }

    public int TotalPages { get; private set; }

    public int TotalCount { get; private set; }

    private PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        TotalCount = count;

        TotalPages = (int)Math.Ceiling(count / (double)pageSize);

        AddRange(items); // Ajoute les éléments de cette page dans la liste héritée.
    }

    public bool HasPreviousPage => PageIndex > 1; // true si on n'est pas sur la 1er page.

    public bool HasNextPage => PageIndex < TotalPages; // true si on n'est pas sur la dernière page.

    // Fabrique asynchrone. On prend un IQueryable (requête PAS encore exécutée,
    // construite dans le contrôleur) et on déclenche seulement ICI les 2 seuls
    // allers-retours SQL nécessaires :
    //   1) CountAsync -> total de lignes (pour calculer le nombre de pages) ;
    //   2) Skip/Take + ToListAsync -> uniquement les N lignes de la page voulue.
    // On ne charge donc JAMAIS toute la table en mémoire pour paginer.
    public static async Task<PaginatedList<T>> CreateAsync(
        IQueryable<T> source, int pageIndex, int pageSize)
    {
        var count = await source.CountAsync();

        var items = await source
            .Skip((pageIndex - 1) * pageSize)   // saute les pages précédentes
            .Take(pageSize)                     // prend une page
            .ToListAsync();

        return new PaginatedList<T>(items, count, pageIndex, pageSize);
    }

    // Fabrique SYNCHRONE à partir d'éléments déjà en mémoire. Utile quand la
    // pagination n'a pas à toucher la base (ex. dans les tests unitaires, pour
    // simuler un retour de service sans requête EF).
    public static PaginatedList<T> Create(
        List<T> items, int count, int pageIndex, int pageSize) =>
        new(items, count, pageIndex, pageSize);
}
