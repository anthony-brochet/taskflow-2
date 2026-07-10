using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using TaskFlow.Data;
using TaskFlow.Services;

// =====================================================================
// POINT D'ENTRÉE de l'application (équivalent du Main() + Application.Run()
// d'un projet WinForms). Depuis .NET 6, plus de classe Program ni de méthode
// Main visibles : ce sont les "top-level statements", exécutés de haut en bas.
//
// Le fichier se lit en DEUX PHASES :
//   PHASE 1 (builder.Services...)  -> on ENREGISTRE les services dans le conteneur
//                                     d'injection de dépendances. "Voici les objets
//                                     que je veux pouvoir demander dans mes constructeurs."
//   PHASE 2 (app.Use... / app.Map...) -> on assemble le PIPELINE HTTP : la file de
//                                     middlewares que CHAQUE requête traverse dans
//                                     l'ordre. L'ORDRE COMPTE (voir plus bas).
// =====================================================================
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ---- PHASE 1 : enregistrement des services ----

// Active le moteur MVC (contrôleurs + vues Razor).
builder.Services.AddControllersWithViews();

// ---- Couche de SERVICES (logique métier) ----
// On enregistre chaque couple interface -> implémentation dans le conteneur DI.
// AddScoped = une instance par requête HTTP (même durée de vie que le DbContext
// qu'ils utilisent). Les contrôleurs réclament les INTERFACES ; le conteneur
// injecte l'implémentation. Pour changer d'implémentation (ou injecter un mock en
// test), il suffit de modifier cette ligne — les contrôleurs ne bougent pas.
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Enregistre le DbContext EF Core. Grâce à ça, n'importe quel contrôleur peut
// recevoir un AppDbContext dans son constructeur (injection). Le framework en
// crée un neuf par requête HTTP et le libère à la fin : tu ne gères jamais la
// durée de vie de la connexion toi-même (contrairement à un using SqlConnection).
// --- Choix du fournisseur de base selon l'ENVIRONNEMENT --------------------
// Railway (et Heroku) injectent une variable d'environnement DATABASE_URL quand
// une base PostgreSQL est attachée. On s'en sert comme "interrupteur" :
//   - DATABASE_URL présente  -> PRODUCTION : PostgreSQL (contexte Postgres)
//   - DATABASE_URL absente    -> DÉVELOPPEMENT : SQLite (contexte Sqlite, fichier local)
//
// Astuce d'architecture : on enregistre le contexte DÉRIVÉ (Sqlite/Postgres) avec
// SON propre jeu de migrations, PUIS on fait résoudre AppDbContext vers ce contexte
// actif. Résultat : tous les contrôleurs, services et Identity continuent d'injecter
// AppDbContext sans savoir quel fournisseur tourne derrière — rien d'autre à changer.
string? databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(databaseUrl))
{
    // PRODUCTION — PostgreSQL sur Railway. DATABASE_URL arrive au format URI
    // (postgresql://user:pass@host:port/db) que Npgsql ne lit pas tel quel :
    // on le convertit en chaîne clé-valeur (voir BuildNpgsqlConnectionString en bas).
    string connectionString = BuildNpgsqlConnectionString(databaseUrl);
    builder.Services.AddDbContext<PostgresAppDbContext>(options =>
        options.UseNpgsql(connectionString));

    builder.Services.AddScoped<AppDbContext>(sp =>
        sp.GetRequiredService<PostgresAppDbContext>());
}
else
{
    // DÉVELOPPEMENT — SQLite (fichier taskflow.db défini par DefaultConnection).
    builder.Services.AddDbContext<SqliteAppDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddScoped<AppDbContext>(sp =>
        sp.GetRequiredService<SqliteAppDbContext>());
}

// AddDefaultIdentity : pour enregistrer les services Identity
// UserManager : créer / modifier / supprimer des users
// SignInManager : connecter / déconnecter user
// PasswordHasher : hacher les mots de passe (jamais en clair)
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    // Règles du mot de passe - à adapter selon les besoins
    options.Password.RequireDigit = true; // Doit avoir un chiffre
    options.Password.RequiredLength = 8; // Minimum de caractères
    options.Password.RequireUppercase = false; // Majuscule non obligatoire
    options.Password.RequireNonAlphanumeric = false; // Caractères spéciaux non obligatoire
    options.SignIn.RequireConfirmedAccount = false; // Désactiver la confirmation email
})
.AddRoles<IdentityRole>() // AddRoles : gestion des rôles (admin, user, etc.)
.AddEntityFrameworkStores<AppDbContext>(); // Permet de stocker les utilisateurs dans AppDbContext (SQLite)

// Clé de signature du JWT lue depuis la configuration (appsettings.Development.json
// en dev, variable d'environnement / user-secrets en prod). On ÉCHOUE VITE si elle
// est absente plutôt que de retomber sur une clé codée en dur : un secret dans le
// code source finit dans Git et compromet toute l'API.
string jwtSecretKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException(
        "Configuration 'Jwt:SecretKey' manquante. Renseignez-la dans " +
        "appsettings.Development.json (dev) ou via une variable d'environnement " +
        "/ les user-secrets (prod).");

builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateIssuer = false,
        ValidateAudience = false,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSecretKey)
        )
    };
});

// OpenAPI natif (.NET 9/10, package Microsoft.AspNetCore.OpenApi) : génère le
// document JSON qui DÉCRIT l'API REST (TaskApiController). Scalar (voir plus bas)
// le transforme en page interactive pour tester l'API dans le navigateur.
// NB : plus besoin de AddEndpointsApiExplorer() ici — c'était l'ancienne
// plomberie de Swagger/Swashbuckle, désormais inutile avec OpenAPI natif.
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Components ??= new();

        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        
        document.Components.SecuritySchemes.Add("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            // Description en **markdown** : Scalar la rend dans le volet d'authentification.
            Description = """
                Authentification par jeton **JWT**.

                **Comment obtenir un jeton :**
                1. Appeler `POST /api/AuthApi/login` avec votre `Email` et votre `Password`.
                2. Copier le champ `token` de la réponse.
                3. Le coller ci-dessous : Scalar l'enverra automatiquement dans l'en-tête
                   `Authorization: Bearer <token>` de chaque requête.

                Le jeton **expire** (voir `expiresAtUtc` dans la réponse de login) ; il faut alors
                se reconnecter pour en obtenir un nouveau.
                """
        });

        // Tâche déjà terminée.
        return Task.CompletedTask;
    });
});

// build() clôt la phase 1 : le conteneur de services est figé, on obtient l'app.
WebApplication app = builder.Build();

// --- Migration automatique en PRODUCTION -----------------------------------
// Sur Railway, la base PostgreSQL démarre VIDE : il faut appliquer les migrations
// au démarrage pour créer le schéma (et injecter le seed). En dev on ne le fait PAS
// automatiquement — on garde la main via `dotnet ef database update`.
// On résout AppDbContext (donc le contexte Postgres actif) le temps d'un scope.
if (app.Environment.IsProduction())
{
    using IServiceScope scope = app.Services.CreateScope();
    AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// ---- PHASE 2 : construction du pipeline HTTP ----
// Chaque app.Use...() ajoute un maillon. Une requête descend la chaîne, la réponse
// remonte. L'ordre est un vrai contrat, pas un détail cosmétique.

// En production seulement : capture les exceptions et affiche /Home/Error au lieu
// de la pile d'appels brute. En dev, on préfère la page d'erreur détaillée.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// UseRouting : détermine QUELLE action va traiter l'URL (mais ne l'exécute pas
// encore). Doit venir avant Authentication/Authorization pour que ceux-ci
// connaissent l'endpoint ciblé et ses attributs [Authorize].
app.UseRouting();

// UseAuthentication : doit-être avant UseAuthorization
// Authentication : "Qui-est-tu ?" (lit le cookie, identifie l'utilisateur)
// Authorization : "As-tu-les droits ?" (verifie Authorize)
// Important : si les deux lignes sont inversées, le login ne marchera pas.
app.UseAuthentication();

app.UseAuthorization();

// Sert les fichiers de wwwroot (CSS, JS, images). MapStaticAssets est la version
// optimisée .NET 9/10 (empreinte + compression) de l'ancien UseStaticFiles.
// On lie ensuite chaque groupe de routes à ces assets via .WithStaticAssets()
// (voir MapControllerRoute / MapRazorPages) pour activer le fingerprinting dans
// les vues (asp-append-version, ImageTagHelper, etc.).
app.MapStaticAssets();

// ROUTAGE PAR CONVENTION — la clé de "comment une URL trouve son code".
// Le patron {controller=Home}/{action=Index}/{id?} se lit :
//   /Todo/Edit/5  ->  TodoController.Edit(5)
//   /Category     ->  CategoryController.Index()  (action par défaut)
//   /             ->  HomeController.Index()      (controller ET action par défaut)
//   ?             ->  id est optionnel
// Aucune table de routes à maintenir : le nom de la classe et de la méthode SUFFIT.
// (Les contrôleurs d'API, eux, utilisent le routage par ATTRIBUT [Route] — voir
//  TaskApiController.)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Nécessaire pour les pages Identity (login, register, logout...) : ce sont des
// Razor Pages livrées par le package Identity.UI, pas des contrôleurs MVC.
app.MapRazorPages()
    .WithStaticAssets();

// Démarre le serveur web (Kestrel) et bloque le thread : équivalent conceptuel
// d'Application.Run() en WinForms. L'app écoute alors les requêtes jusqu'à l'arrêt.
app.Run();

// ---------------------------------------------------------------------------
// Convertit une URL PostgreSQL au format URI (fournie par Railway/Heroku)
// en chaîne de connexion Npgsql clé-valeur (Host=...;Port=...;Username=...).
// Npgsql ne sait pas consommer directement une URI, d'où cette traduction.
// ---------------------------------------------------------------------------
static string BuildNpgsqlConnectionString(string databaseUrl)
{
    // Si ce n'est pas une URI (déjà au format clé-valeur), on la retourne telle quelle :
    // ça permet aussi de coller une chaîne Npgsql classique dans DATABASE_URL.
    if (!databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
        && !databaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        return databaseUrl;
    }

    var uri = new Uri(databaseUrl);
    string[] userInfo = uri.UserInfo.Split(':', 2);

    var csb = new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.IsDefaultPort ? 5432 : uri.Port,
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
        Database = uri.AbsolutePath.TrimStart('/'),
        // Prefer : utilise TLS s'il est disponible, sinon connexion en clair.
        // Compatible avec le réseau interne Railway (sans TLS) comme avec l'URL publique.
        SslMode = Npgsql.SslMode.Prefer
    };

    return csb.ConnectionString;
}
