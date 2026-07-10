using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using TaskFlow.Models;

namespace TaskFlow.Data;

// Le DbContext est le "chef d'orchestre" d'Entity Framework Core.
//
// Rappel WinForms -> ASP.NET :
//   En WinForms on ouvrait souvent une SqlConnection + SqlCommand pour parler
//   à la base. Ici, le DbContext remplace tout ça : chaque DbSet<T> ci-dessous
//   est comme une "table typée" que l'on interroge en LINQ, et EF Core traduit
//   le LINQ en SQL pour nous.
//
// On hérite de IdentityDbContext<IdentityUser> (et non du simple DbContext)
// pour qu'EF crée aussi automatiquement les tables d'authentification
// (AspNetUsers, AspNetRoles, etc.) gérées par ASP.NET Core Identity.
public class AppDbContext : IdentityDbContext<IdentityUser>
{
    // Constructeur "normal" : utilisé quand on injecte directement AppDbContext.
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    // Constructeur protégé : indispensable pour les contextes DÉRIVÉS
    // (SqliteAppDbContext / PostgresAppDbContext). Chacun porte son propre jeu de
    // migrations, donc reçoit un DbContextOptions typé à SON nom. On accepte ici
    // le type non générique DbContextOptions pour laisser passer ces options
    // spécialisées vers la base. Le schéma reste identique : toute la config
    // (relations, seed) est définie une seule fois dans OnModelCreating ci-dessous.
    protected AppDbContext(DbContextOptions options)
        : base(options) { }

    // Chaque DbSet<T> = une table en base. On les interroge via _context.TodoTasks, etc.
    public DbSet<TodoTask> TodoTasks { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Attachment> Attachments { get; set; }

    // OnModelCreating = l'endroit où l'on configure finement le schéma
    // (relations, règles de suppression, données initiales).
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Indispensable : laisse Identity configurer ses propres tables avant nous.
        base.OnModelCreating(builder);

        // --- Règles de suppression (ON DELETE) -----------------------------
        // Sans configuration, EF interdit par défaut de supprimer une catégorie
        // encore utilisée par des tâches (violation de clé étrangère).
        // SetNull : supprimer une catégorie met simplement CategoryId à NULL
        // sur les tâches concernées (la tâche existe toujours, sans catégorie).
        builder.Entity<TodoTask>()
            .HasOne(t => t.Category)
            .WithMany(c => c.Tasks)
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // Relation vers l'utilisateur assigné (AspNetUsers). .WithMany() sans
        // argument = pas de collection inverse sur IdentityUser (inutile ici).
        // SetNull : si l'utilisateur assigné est supprimé, la tâche n'est pas
        // supprimée, elle est simplement désassignée.
        builder.Entity<TodoTask>()
            .HasOne(t => t.AssignedToUser)
            .WithMany()
            .HasForeignKey(t => t.AssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Cascade : supprimer une tâche supprime aussi ses commentaires
        // et ses pièces jointes (ils n'ont aucun sens sans leur tâche parente).
        builder.Entity<Comment>()
            .HasOne(c => c.Task)
            .WithMany(t => t.Comments)
            .HasForeignKey(c => c.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Attachment>()
            .HasOne(a => a.Task)
            .WithMany(t => t.Attachments)
            .HasForeignKey(a => a.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // --- Données initiales (seed) --------------------------------------
        // HasData insère ces lignes lors des migrations : pratique pour avoir
        // des catégories de démo dès le premier lancement.
        builder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Travail", Color = "#3b5bdb" },
            new Category { Id = 2, Name = "Perso", Color = "#16a34a" },
            new Category { Id = 3, Name = "Urgent", Color = "#dc2626" },
            new Category { Id = 4, Name = "Entraînement", Color = "#ea580c" }
        );

        // Dates en UTC OBLIGATOIRE : PostgreSQL mappe DateTime sur
        // "timestamp with time zone" et refuse une date de type Unspecified.
        // DateTimeKind.Utc rend le seed compatible avec les DEUX fournisseurs
        // (SQLite ignore le fuseau, PostgreSQL l'exige).
        builder.Entity<TodoTask>().HasData(
            new TodoTask
            {
                Id = 1,
                Title = "Tâche 1",
                Description = "Description de la tâche 1",
                Priority = PriorityLevel.High,
                IsCompleted = false,
                DueDate = new DateTime(2026, 09, 25, 0, 0, 0, DateTimeKind.Utc)
            },
            new TodoTask
            {
                Id = 2,
                Title = "Tâche 2",
                Description = "Description de la tâche 2",
                Priority = PriorityLevel.Medium,
                IsCompleted = true,
                DueDate = new DateTime(2026, 08, 25, 0, 0, 0, DateTimeKind.Utc)
            },
            new TodoTask
            {
                Id = 3,
                Title = "Tâche 3",
                Description = "Description de la tâche 3",
                Priority = PriorityLevel.Low,
                IsCompleted = false,
                DueDate = new DateTime(2026, 12, 25, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
