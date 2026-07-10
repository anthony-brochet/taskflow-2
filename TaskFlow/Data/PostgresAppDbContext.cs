using Microsoft.EntityFrameworkCore;

namespace TaskFlow.Data;

// Contexte utilisé en PRODUCTION (PostgreSQL, base Railway).
//
// Même principe que SqliteAppDbContext : un type dédié pour que son jeu de
// migrations (Migrations/Postgres) soit distinct de celui de SQLite. Les deux
// fournisseurs génèrent un SQL différent (types de colonnes, séquences, etc.),
// d'où l'intérêt d'avoir un jeu de migrations par fournisseur.
// Tout le schéma (DbSet, relations, seed) est hérité d'AppDbContext.
public class PostgresAppDbContext : AppDbContext
{
    public PostgresAppDbContext(DbContextOptions<PostgresAppDbContext> options)
        : base(options) { }
}
