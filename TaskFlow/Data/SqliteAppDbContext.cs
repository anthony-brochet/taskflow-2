using Microsoft.EntityFrameworkCore;

namespace TaskFlow.Data;

// Contexte utilisé en DÉVELOPPEMENT (SQLite, fichier taskflow.db).
//
// Il n'ajoute AUCUN comportement : il ne sert qu'à donner une "identité" propre
// au jeu de migrations SQLite. EF Core associe chaque migration à un type de
// DbContext précis ; en ayant un type dédié, les migrations SQLite (dans
// Migrations/Sqlite) restent séparées des migrations PostgreSQL (Migrations/Postgres).
// Tout le schéma (DbSet, relations, seed) est hérité d'AppDbContext.
public class SqliteAppDbContext : AppDbContext
{
    public SqliteAppDbContext(DbContextOptions<SqliteAppDbContext> options)
        : base(options) { }
}
