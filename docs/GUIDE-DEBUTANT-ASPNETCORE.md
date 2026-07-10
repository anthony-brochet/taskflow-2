# Guide débutant — Comprendre TaskFlow quand on vient de WinForms/WPF

Ce guide t'accompagne dans la lecture du projet **en partant de ce que tu connais
déjà** (le desktop C#). Il ne remplace pas les commentaires du code : il donne la
**vue d'ensemble** et le **modèle mental** qui manquent souvent quand on débute
sur le web. Lis-le une fois en entier, puis reviens-y section par section en
ouvrant les fichiers cités.

## Table des matières

1. [Le changement de modèle mental : desktop → web](#1-le-changement-de-modèle-mental--desktop--web)
2. [Le point d'entrée : `Program.cs`](#2-le-point-dentrée--programcs)
3. [L'injection de dépendances (DI)](#3-linjection-de-dépendances-di)
4. [Le patron MVC](#4-le-patron-mvc-modèle--vue--contrôleur)
5. [Le routage : comment une URL trouve son code](#5-le-routage--comment-une-url-trouve-son-code)
6. [Les vues Razor vs XAML/Designer](#6-les-vues-razor-vs-xamldesigner)
7. [Entity Framework Core : la base de données en objets](#7-entity-framework-core--la-base-de-données-en-objets)
8. [Les migrations](#8-les-migrations--faire-évoluer-la-base-sans-sql-à-la-main)
9. [L'authentification : Identity (web) et JWT (API)](#9-lauthentification--identity-web-et-jwt-api)
10. [MVC vs API REST](#10-mvc-classique-vs-api-rest)
11. [`async`/`await` partout : pourquoi ?](#11-asyncawait-partout--pourquoi)
12. [La validation des données](#12-la-validation-des-données)
13. [Faire passer des données à la vue : Model, ViewBag, TempData](#13-faire-passer-des-données-à-la-vue--model-viewbag-tempdata)
14. [Les tests unitaires](#14-les-tests-unitaires)
15. [L'architecture en couches : contrôleurs fins, services, interfaces](#15-larchitecture-en-couches--contrôleurs-fins-services-interfaces)
16. [Glossaire express](#16-glossaire-express)
17. [Par où commencer à lire le code](#17-par-où-commencer-à-lire-le-code)

---

## 1. Le changement de modèle mental : desktop → web

C'est **la** marche à franchir. En WinForms/WPF, ton application est un **processus
unique et permanent** : elle démarre, garde tout en mémoire (les objets, l'état des
contrôles, l'utilisateur courant…) jusqu'à ce qu'on la ferme. Un clic sur un bouton
appelle directement une méthode qui a accès à tout cet état.

Sur le web, c'est radicalement différent :

| WinForms / WPF (desktop) | ASP.NET Core (web) |
|---|---|
| Un processus par utilisateur, sur **sa** machine | **Un** serveur pour **tous** les utilisateurs |
| L'application reste en mémoire en permanence | Chaque **requête HTTP** est traitée puis **oubliée** |
| L'état vit dans les champs de la classe `Form` | **Sans état** (*stateless*) : rien n'est gardé entre deux requêtes |
| `button_Click` appelle ta méthode directement | Le navigateur envoie une **requête**, le serveur renvoie une **réponse** (HTML ou JSON) |
| Tu ouvres/fermes toi-même la connexion SQL | Le framework crée/détruit les objets **par requête** |

**La conséquence la plus importante** : le serveur ne « se souvient » de rien d'une
requête à l'autre. Comment sait-il alors *qui* est connecté ? Grâce à un **cookie**
(un petit jeton renvoyé par le navigateur à chaque requête) que le middleware
d'authentification relit à chaque fois. C'est pour ça que tu verras partout dans le
code `_userManager.GetUserId(User)` : à chaque requête, on **redemande** qui est
l'utilisateur, car on ne peut pas le stocker « quelque part » comme un champ de Form.

> **Analogie** : un formulaire WinForms, c'est une conversation continue avec un
> interlocuteur qui te reconnaît. Le web, c'est une succession de lettres : à chaque
> lettre, l'expéditeur doit **re-signer** (le cookie) pour qu'on sache qui écrit.

---

## 2. Le point d'entrée : `Program.cs`

📄 `TaskFlow/Program.cs`

En WinForms, `Program.cs` contenait `Application.Run(new Form1())`. Ici aussi c'est
le point de départ, mais il fait deux choses bien distinctes, dans l'ordre :

**Phase 1 — Enregistrer les services** (`builder.Services.Add...`)
On déclare tous les objets que l'application saura fabriquer à la demande : le
`DbContext` EF Core, Identity, l'authentification JWT, MVC, OpenAPI… C'est le
**catalogue** des dépendances (voir section 3).

**Phase 2 — Construire le pipeline HTTP** (`app.Use...`)
On assemble la **chaîne de traitement** que **chaque requête** va traverser. C'est
une file de « middlewares » (maillons) :

```
Requête → HttpsRedirection → Routing → Authentication → Authorization → [ton Controller] → Réponse
```

**L'ORDRE COMPTE** (contrairement à l'enregistrement des services, où il est libre).
Exemple concret du code : `UseAuthentication()` doit venir **avant**
`UseAuthorization()`. Logique : il faut d'abord établir *qui tu es* (authentification)
avant de vérifier *si tu as le droit* (autorisation). Inverser les deux casse le login.

C'est un concept **sans équivalent WinForms** : imagine une série de filtres que
toute action traverse obligatoirement, à l'aller (requête) et au retour (réponse).

---

## 3. L'injection de dépendances (DI)

C'est **omniprésent** en ASP.NET Core, alors qu'en WinForms tu faisais surtout des
`new` manuels. Regarde n'importe quel contrôleur :

```csharp
public class TodoController(AppDbContext context, UserManager<IdentityUser> userManager) : Controller
```

Tu ne fais **jamais** `new AppDbContext()`. Tu **déclares dans le constructeur** ce
dont tu as besoin, et le framework te le **fournit tout prêt**. C'est ça, l'injection
de dépendances : les objets sont réclamés, pas fabriqués.

| WinForms | ASP.NET Core |
|---|---|
| `var ctx = new AppDbContext();` dans chaque méthode | `AppDbContext` reçu dans le constructeur |
| Tu gères l'ouverture/fermeture de la connexion | Le framework crée un `DbContext` par requête et le libère à la fin |
| Difficile à tester (dépendances en dur) | Facile à tester (on injecte un faux — voir section 14) |

**Pourquoi c'est mieux ?**
1. **Durée de vie gérée pour toi** : un `DbContext` neuf par requête, détruit à la fin. Tu ne fermes jamais rien à la main.
2. **Testabilité** : dans les tests, on injecte une base InMemory et un `UserManager` simulé (Moq) à la place des vrais.
3. **Une seule source de vérité** : la config des services est centralisée dans `Program.cs`.

> La syntaxe `class TodoController(AppDbContext context) : Controller` est le
> **constructeur primaire** (C# 12+) : une écriture compacte du constructeur +
> affectation des champs. Équivaut à déclarer un constructeur classique qui range
> `context` dans un champ privé.

---

## 4. Le patron MVC (Modèle – Vue – Contrôleur)

MVC découpe chaque écran en trois responsabilités. Le parallèle avec WinForms :

| Rôle MVC | Équivalent WinForms | Dans ce projet |
|---|---|---|
| **Model** | Ta classe métier / DataSet | `Models/TodoTask.cs`, `Category.cs`… |
| **View** | Le Designer (`.Designer.cs` + contrôles) | `Views/Todo/Index.cshtml` (HTML Razor) |
| **Controller** | Le code-behind (`Form1.cs` + `button_Click`) | `Controllers/TodoController.cs` |

**Un contrôleur = un ancien code-behind.** Chaque **action** (méthode publique) est
comme un gestionnaire d'événement : elle reçoit une requête, fait le travail, et
renvoie une **réponse**.

> 💡 Dans ce projet, les contrôleurs sont **fins** : ils ne contiennent PAS la
> logique métier (requêtes base, règles), qui vit dans la **couche de services**
> (`ITaskService`…). Le contrôleur ne fait que traduire HTTP ↔ appel de service.
> C'est détaillé à la **[section 15](#15-larchitecture-en-couches--contrôleurs-fins-services-interfaces)** — garde ça en tête en lisant les exemples ci-dessous.

Exemple, `TodoController.Index(...)` :
1. Lit qui est connecté (`_userManager.GetUserId(User)`).
2. Construit une requête LINQ filtrée sur les tâches de cet utilisateur.
3. Applique filtres, recherche, tri, pagination.
4. Renvoie `View(paginatedTasks)` → génère le HTML de `Views/Todo/Index.cshtml`.

Les **types de retour** d'une action remplacent les « et maintenant j'affiche quoi ? »
du desktop :

| Retour | Signification |
|---|---|
| `View(model)` | Génère une page HTML à partir d'une vue Razor |
| `RedirectToAction("Index")` | Renvoie le navigateur vers une autre URL |
| `NotFound()` | HTTP 404 |
| `BadRequest()` | HTTP 400 |
| `Ok(objet)` | HTTP 200 + JSON (API) |

**Le patron « Post/Redirect/Get »** (visible dans `CategoryController`, `TodoController`) :
après un POST qui modifie la base, on ne renvoie **jamais** une vue directement, on
**redirige**. Sinon, un simple F5 (rafraîchir) renverrait le formulaire une 2ᵉ fois
et créerait un doublon. Ce réflexe web n'existe pas en WinForms.

---

## 5. Le routage : comment une URL trouve son code

En WinForms, tu ouvrais un formulaire avec `new Form2().Show()`. Sur le web, c'est
**l'URL** qui décide quel code s'exécute. Deux styles coexistent dans le projet :

**Routage par convention** (contrôleurs MVC) — défini dans `Program.cs` :
```
pattern: "{controller=Home}/{action=Index}/{id?}"
```
Ça se lit :
- `/Todo/Edit/5` → `TodoController.Edit(5)`
- `/Category` → `CategoryController.Index()` (action `Index` par défaut)
- `/` → `HomeController.Index()` (contrôleur *et* action par défaut)
- le `?` sur `id` = paramètre optionnel

Aucune table de routes à maintenir : le **nom** de la classe et de la méthode suffit.

**Routage par attribut** (contrôleurs d'API) — défini sur la classe :
```csharp
[Route("api/[controller]")]   // → /api/TaskApi
[HttpGet("{id}")]             // → GET /api/TaskApi/5
```
Ici, chaque endpoint déclare **explicitement** son URL et son verbe HTTP. C'est la
convention pour les API REST (voir section 10).

---

## 6. Les vues Razor vs XAML/Designer

📄 `TaskFlow/Views/`

Une vue `.cshtml` est un **fichier HTML** dans lequel on peut injecter du C# avec `@`.
C'est le pendant web du Designer WinForms ou du XAML WPF, mais **côté serveur** : le
C# s'exécute sur le serveur pour **produire du HTML**, qui est ensuite envoyé au
navigateur (le navigateur ne voit jamais ton C#).

```cshtml
@model DashboardViewModel                     @* type de données reçu du contrôleur *@
<h1>Bonjour</h1>
<p>Tâches terminées : @Model.Completed</p>     @* @ = « insère ici la valeur C# » *@
@foreach (var t in Model.TopTasks) { <li>@t.Title</li> }
```

**Les Tag Helpers** (`asp-for`, `asp-action`, `asp-controller`…) sont l'équivalent
web du *data binding* WPF. Exemple :

```cshtml
<label asp-for="Title"></label>     @* génère <label> avec le libellé [Display] du modèle *@
<input asp-for="Title" />           @* génère un <input> lié à la propriété Title *@
<span asp-validation-for="Title"></span>  @* affiche le message de validation *@
```

C'est ASP.NET qui, à partir de tes **Data Annotations** sur le modèle (`[Display]`,
`[Required]`…), génère le bon HTML, le bon libellé et la validation. Un seul endroit
à changer (le modèle) et tout suit — comme un binding WPF, mais résolu côté serveur.

**Fichiers spéciaux** :
- `Views/Shared/_Layout.cshtml` : le **gabarit commun** (en-tête, menu, pied de page) — l'équivalent d'une MasterPage / d'une fenêtre parente. Chaque vue vient s'insérer dedans.
- `Views/_ViewStart.cshtml` : dit d'appliquer ce layout par défaut.
- `Views/_ViewImports.cshtml` : les `@using` et Tag Helpers disponibles partout.

---

## 7. Entity Framework Core : la base de données en objets

📄 `TaskFlow/Data/AppDbContext.cs`, `TaskFlow/Models/*.cs`

En WinForms, pour parler à SQL tu écrivais souvent :
```csharp
using var conn = new SqlConnection(cs);
using var cmd = new SqlCommand("SELECT * FROM Tasks WHERE UserId=@u", conn);
// … lecture manuelle du DataReader …
```

EF Core **supprime tout ça**. Trois idées clés :

**1. Une classe = une table.** `TodoTask.cs` **est** la table `TodoTasks`. Chaque
propriété est une colonne. Les `[Required]`, `[StringLength]` configurent à la fois
le schéma SQL **et** les règles de validation.

**2. Le `DbContext` est le point d'accès.** Chaque `DbSet<T>` est une « table typée » :
```csharp
public DbSet<TodoTask> TodoTasks { get; set; }   // ≈ la table TodoTasks
```
On l'interroge en **LINQ**, et EF traduit en SQL :
```csharp
await _context.TodoTasks.Where(t => t.UserId == userId).ToListAsync();
// EF génère : SELECT ... FROM TodoTasks WHERE UserId = @userId
```

**3. Requête différée (`IQueryable`).** Point subtil mais central, très visible dans
`TodoController.Index` :
```csharp
IQueryable<TodoTask> query = _context.TodoTasks.Where(...);   // rien n'est exécuté
query = query.Where(...);                                     // on empile des filtres
query = query.OrderBy(...);                                   // toujours rien
var list = await query.ToListAsync();                         // ← ICI seulement, EF parle à SQL
```
Tant que tu n'appelles pas `ToListAsync()` / `CountAsync()` / `FirstOrDefaultAsync()`,
tu construis juste une **recette**. EF l'exécute en **une seule requête SQL optimisée**
au dernier moment. C'est ce qui permet d'ajouter des filtres conditionnels sans
multiplier les allers-retours base.

**Quelques mots-clés que tu croiseras beaucoup :**
- `.Include(t => t.Category)` : charge une relation liée (sinon `t.Category` est `null`). Équivaut à un `JOIN`.
- `.AsNoTracking()` : « lecture seule, ne surveille pas ces objets » → plus rapide. À utiliser dès qu'on affiche sans modifier.
- `.Select(t => new { ... })` : ne ramène que les colonnes utiles (projection).
- `_context.Add(x)` puis `await _context.SaveChangesAsync()` : EF génère l'`INSERT`/`UPDATE`/`DELETE`. **Rien n'est écrit en base tant que `SaveChangesAsync` n'est pas appelé** (c'est exactement le bug qu'on a corrigé dans un test !).

**Le suivi des modifications (change tracking)** — voir `TodoController.Edit` :
```csharp
var task = await _context.TodoTasks.FirstOrDefaultAsync(...);  // EF "suit" cet objet
task.Title = taskModified.Title;                               // on modifie ses champs
await _context.SaveChangesAsync();                             // EF détecte le changement → UPDATE ciblé
```
Tu ne écris jamais l'`UPDATE` : EF compare l'état de l'objet à ce qu'il avait lu et
génère la commande SQL minimale. Magique au début, logique ensuite.

---

## 8. Les migrations : faire évoluer la base sans SQL à la main

📄 `TaskFlow/Migrations/`

Puisque le schéma vit dans le code C#, comment le refléter dans la vraie base ? Avec
des **migrations** : des fichiers générés qui décrivent *comment passer d'une version
du schéma à la suivante*. C'est un **historique versionné** de ta base, rejouable sur
n'importe quelle machine.

Le cycle typique :
```bash
# 1. Tu modifies un modèle (ex. tu ajoutes une propriété à TodoTask)
# 2. Tu génères la migration correspondante
dotnet ef migrations add AddDueDateReminder --project TaskFlow
# 3. Tu l'appliques à la base
dotnet ef database update --project TaskFlow
```

Dans ce projet, l'historique raconte l'évolution de l'app : `InitialCreate` →
`AddIdentity` → `AddUserIdToTodoTask` → `AddCategories` → `AddComments` →
`AddAttachments` → `AddAssignedTo` → `ConfigureDeleteBehaviors`. Chaque nom te dit
quelle fonctionnalité a été ajoutée. C'est l'équivalent, pour la base, de tes commits
Git pour le code.

> Le `HasData(...)` dans `AppDbContext` insère des **données de départ** (4 catégories,
> 3 tâches de démo) au moment des migrations — pratique pour ne pas démarrer sur une
> base vide.

---

## 9. L'authentification : Identity (web) et JWT (API)

Le projet gère l'identité à **deux niveaux**, car un site web et une API n'ont pas
les mêmes contraintes.

**ASP.NET Core Identity (côté site web)** — 📄 `Program.cs` + pages `/Identity/Account/*`
- Fournit tout le socle : inscription, connexion, hachage des mots de passe, gestion des rôles.
- `AddDefaultIdentity<IdentityUser>()` enregistre `UserManager` (créer/modifier des users) et `SignInManager` (connecter/déconnecter).
- Le mécanisme utilisé est le **cookie** : après login, le serveur dépose un cookie que le navigateur renvoie à chaque requête. Le middleware `UseAuthentication()` le relit et reconstitue « l'utilisateur courant » (`User`). C'est ce qui compense l'absence d'état vue en section 1.
- L'attribut `[Authorize]` sur un contrôleur exige d'être connecté ; sinon → redirection vers la page de login.

**JWT Bearer (côté API)** — 📄 `AuthApiController.cs` + `TaskApiController.cs`
- Une API est appelée par du **code** (appli mobile, JavaScript, Postman), pas par un navigateur qui gère les cookies. On utilise donc un **jeton** (token).
- `POST /api/AuthApi/login` vérifie l'e-mail + mot de passe, puis fabrique un **JWT** : une chaîne signée qui contient l'identité de l'utilisateur (ses *claims*) et une date d'expiration.
- Le client renvoie ensuite ce token dans l'en-tête `Authorization: Bearer <token>` à chaque appel. Le serveur vérifie la **signature** (avec la clé secrète) pour s'assurer qu'il n'a pas été falsifié.
- Avantage : **sans état**. Le serveur n'a rien à stocker ; tout est dans le token signé.

| | Cookie (site) | JWT (API) |
|---|---|---|
| Client visé | Navigateur | Code / app mobile |
| Stockage | Cookie géré par le navigateur | Le client garde le token |
| Vérification | Middleware relit le cookie | Signature vérifiée à chaque requête |

> ⚠️ La clé secrète JWT est **codée en dur** en dev (`"TaskFlow-Super-Cle-Secrete-Only-Dev"`).
> En production, elle doit venir de la configuration (`Jwt:SecretKey`), jamais du code
> source. Voir la section *Sécurité* du README.

---

## 10. MVC classique vs API REST

C'est une distinction que tu n'avais pas en WinForms. Les deux styles cohabitent :

| | Contrôleur MVC (`TodoController`) | Contrôleur API (`TaskApiController`) |
|---|---|---|
| Hérite de | `Controller` | `ControllerBase` |
| Renvoie | des **vues HTML** | des **données JSON** |
| Pour qui | un humain qui clique dans un navigateur | du code (JS, mobile, Postman…) |
| Authentification | Cookie | JWT |
| Routage | par convention | par attribut (`[Route]`, `[HttpGet]`…) |

Une **API REST** utilise les **verbes HTTP** pour exprimer l'intention, et des **codes
de statut** pour le résultat. C'est un langage standard que tout le monde comprend :

| Verbe | Intention | Code de succès typique |
|---|---|---|
| `GET` | Lire | 200 OK |
| `POST` | Créer | 201 Created |
| `PUT` | Remplacer | 204 No Content |
| `PATCH` | Modifier partiellement | 200 OK |
| `DELETE` | Supprimer | 204 No Content |
| — | Erreur « pas trouvé » | 404 Not Found |
| — | Erreur « requête invalide » | 400 Bad Request |
| — | Erreur « pas authentifié » | 401 Unauthorized |

Dans `TaskApiController`, chaque méthode renvoie donc `Ok(...)`, `NotFound(...)`,
`CreatedAtAction(...)`, `NoContent()`… au lieu de vues. Le `[ApiController]` en tête
active des automatismes (validation → 400 automatique, binding JSON du corps, etc.).

Un détail important et bien vu dans ce projet : l'API ne renvoie **jamais l'entité
brute** mais une **projection** (`.Select(t => new { ... })`). Ça évite de divulguer
des champs sensibles (`UserId`) et fige le contrat JSON indépendamment de la table.

---

## 11. `async`/`await` partout : pourquoi ?

Tu remarqueras que presque toutes les actions sont `public async Task<IActionResult>`
et enchaînent des `await`. En desktop, l'`async` servait surtout à ne pas geler l'UI.
Sur un serveur, l'enjeu est différent et plus critique : la **scalabilité**.

Un serveur traite **des centaines de requêtes simultanées** avec un **nombre limité
de threads**. Quand une action attend la base de données (`await ...ToListAsync()`),
`await` **libère le thread** pour qu'il aille servir une autre requête pendant l'attente,
au lieu de rester bloqué à ne rien faire. Résultat : le serveur encaisse beaucoup plus
de trafic avec les mêmes ressources.

**Règle pratique** : dès qu'une méthode fait des entrées/sorties (base, fichier,
réseau), utilise sa version `...Async` et `await`-la. Tu propages `async` de proche en
proche jusqu'à l'action du contrôleur, qui devient `async Task<IActionResult>`.

---

## 12. La validation des données

En WinForms, tu validais à la main dans l'événement `Validating` de chaque contrôle.
Ici, c'est **centralisé sur le modèle** via les Data Annotations :

```csharp
[Required(ErrorMessage = "Le titre est requis.")]
[StringLength(200, ErrorMessage = "…200 caractères.")]
public string Title { get; set; } = string.Empty;
```

Ces attributs servent **trois fois** :
1. **Côté navigateur** (JavaScript non-intrusif) : message immédiat sans aller-retour serveur.
2. **Côté serveur** : dans le contrôleur, `if (!ModelState.IsValid)` renvoie le formulaire avec les erreurs. **Ne fais jamais confiance au seul contrôle navigateur** (il est contournable).
3. **Schéma SQL** : `[StringLength(200)]` devient `NVARCHAR(200)`.

Tu croiseras des annotations de **sécurité** aussi, comme le `[RegularExpression]` sur
`Category.Color` : il impose un code hexadécimal strict, ce qui empêche d'injecter du
code malveillant réinjecté ensuite dans le HTML (faille XSS).

**`[Bind]` — la parade à l'over-posting.** Dans `TodoController.Create` :
```csharp
Create([Bind("Title,Description,Priority,CategoryId,DueDate,AssignedToUserId")] TodoTask task)
```
On liste **explicitement** les champs que le formulaire a le droit de remplir. `UserId`,
`IsCompleted`… en sont exclus : c'est le **serveur** qui les fixe. Sans ça, un
utilisateur pourrait bricoler le formulaire pour écrire dans un champ sensible.

---

## 13. Faire passer des données à la vue : Model, ViewBag, TempData

Trois mécanismes, trois usages. À bien distinguer :

| Mécanisme | Durée de vie | Quand l'utiliser | Exemple dans le code |
|---|---|---|---|
| **Model** (`View(model)`) | La requête courante | La donnée **principale** de la page, **typée** | `View(paginatedTasks)` |
| **ViewBag** / ViewData | La requête courante | Des données **secondaires** (menus déroulants, filtres actifs) | `ViewBag.Categories = new SelectList(...)` |
| **TempData** | Survit à **une** redirection | Un message à afficher **après** un `RedirectToAction` | `TempData["SuccessMessage"] = "Tâche créée !"` |

Le piège classique : après un POST réussi, tu rediriges (Post/Redirect/Get, section 4).
Or `ViewBag` **meurt** à la fin de la requête → le message de succès serait perdu.
`TempData` (stocké en cookie/session) **survit** juste le temps de la redirection, puis
disparaît. C'est exactement pour ça qu'il est utilisé pour les messages « … avec succès ! ».

Le **ViewModel** (`DashboardViewModel`) mérite une mention : c'est une classe **taillée
pour un écran précis**, pas pour une table. Le tableau de bord a besoin de compteurs
agrégés et d'un top 5 : on lui passe un objet dédié plutôt que l'entité brute. C'est
l'équivalent propre et testable du « je remplis les Labels de mon formulaire de synthèse
à la main » du desktop.

---

## 14. Les tests unitaires

📄 `TaskFlow.Tests/`

Le projet de test vérifie automatiquement que le code fait ce qu'on attend. Trois
outils :

- **xUnit** : le framework de test. `[Fact]` = un test ; `[Theory]` + `[InlineData]` = le même test rejoué avec plusieurs jeux de valeurs.
- **Moq** : fabrique de **faux objets**. Le `UserManager` réel a besoin de toute l'infra Identity ; en test, on le **simule** pour qu'il réponde toujours « l'utilisateur connecté est X ».
- **EF Core InMemory** : une base de données **factice en mémoire**, jetable, recréée à chaque test. On ne touche jamais au vrai fichier SQLite.

La structure d'un test suit le patron **AAA** (Arrange / Act / Assert) :
```csharp
[Fact]
public async Task Edit_Get_Returns404_WhenTaskBelongsToAnotherUser()
{
    // ARRANGE : on prépare une base InMemory avec une tâche de user2
    // ACT     : user1 tente d'ouvrir la tâche de user2
    var result = await controller.Edit(51);
    // ASSERT  : on doit obtenir un 404, pas la page d'édition
    Assert.IsType<NotFoundResult>(result);
}
```
Ce test vérifie une **règle de sécurité** (anti-IDOR) : on ne peut pas voir/modifier la
tâche d'autrui. C'est le genre de test qui a le plus de valeur.

> 💡 **Trois vrais bugs attrapés dans ce projet** — très instructifs :
> 1. Un test insérait des données avec `AddRange(...)` mais **oubliait
>    `SaveChangesAsync()`** : la requête ne voyait alors rien (voir section 7 — rien
>    n'est écrit tant qu'on ne sauve pas).
> 2. Un autre insérait une tâche `Id = 51` mais appelait `DeleteConfirmed(1)` :
>    l'id ne correspondait pas, rien n'était supprimé.
> 3. Un DTO `record` déclarait sa validation avec `[property: Required]` : ça
>    compilait et les tests **unitaires** passaient (ils construisaient le DTO en
>    direct), mais l'API renvoyait **500** en vrai, car MVC exige la validation sur
>    le **paramètre** du constructeur. Seul un **test de bout en bout** (lancer
>    l'app, appeler l'API) l'a révélé.
>
> Leçons : (a) un test qui échoue peut révéler un bug **du test** ; (b) les tests
> unitaires ne voient pas tout — **tester au bon niveau** compte, et un test
> d'intégration (drive réel de l'appli) attrape ce que le mock masque.

**Le test de bout en bout (intégration)** — au-delà des tests unitaires, on a
vérifié le parcours réel : lancer l'app, créer un compte, se connecter à l'API
(`/api/AuthApi/login`), récupérer le JWT, appeler `/api/TaskApi` avec le token.
C'est ce qui a prouvé que le **correctif JWT** (section 9/10) fonctionne : sans
token, l'API renvoie bien `401`, et avec un token valide, `200`.

**Ce qui n'est pas encore testé** (à toi de jouer pour progresser) :
`CommentService`, `AttachmentService` (upload/validation de fichiers), et le
`CategoryController`. Écrire ces tests — au niveau **service** de préférence — est
le meilleur exercice pour ancrer tout ce guide.

---

## 15. L'architecture en couches : contrôleurs fins, services, interfaces

C'est **le** point d'architecture le plus important à comprendre pour un poste en
entreprise. Le principe : **séparer les responsabilités en couches**, chacune avec
un seul rôle.

| Couche | Rôle unique | Dans ce projet |
|---|---|---|
| **Présentation** | Le HTTP et l'affichage | `Controllers/` + `Views/` |
| **Métier (services)** | La logique applicative | `Services/` (`ITaskService`…) |
| **Données** | L'accès à la base | `Data/AppDbContext.cs` (EF Core) |

**En WinForms/WPF**, on parlait d'architecture « n-tiers » : une classe *Form*
(présentation), une classe *BusinessLayer* (métier), une classe *DataAccess* (base).
C'est exactement la même idée ici, formalisée par le framework.

### Pourquoi ne pas tout mettre dans le contrôleur ?

Au départ, ce projet avait des « **fat controllers** » : le `TodoController`
contenait les requêtes EF Core, les filtres, les règles de sécurité, les calculs…
Problèmes :
- **Duplication** : le `TodoController` (web) et le `TaskApiController` (API)
  refaisaient la même logique chacun de leur côté.
- **Intestable** : pour tester la logique, il fallait tout l'attirail HTTP.
- **Illisible** : une action mélangeait « quoi faire » et « comment répondre en HTTP ».

Depuis la refonte, la logique vit dans **`TaskService`**, et les deux contrôleurs
l'appellent :

```
TodoController (web)  ─┐
                       ├──►  ITaskService  ──►  AppDbContext (EF Core)  ──►  SQLite
TaskApiController (API)─┘
```

Un contrôleur ne fait plus que **traduire** : requête HTTP → appel de service →
réponse HTTP (`View` / `Redirect` / `Ok` / `NotFound`). Compare :

```csharp
// AVANT (fat controller) : logique + HTTP mélangés
public async Task<IActionResult> Delete(int id)
{
    var userId = _userManager.GetUserId(User);
    var task = await _context.TodoTasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);
    if (task != null) { _context.TodoTasks.Remove(task); await _context.SaveChangesAsync(); }
    return RedirectToAction(nameof(Index));
}

// APRÈS : le contrôleur ne fait QUE du HTTP
public async Task<IActionResult> DeleteConfirmed(int id)
{
    await _taskService.DeleteAsync(_userManager.GetUserId(User), id);
    return RedirectToAction(nameof(Index));
}
```

### Pourquoi une INTERFACE (`ITaskService`) et pas juste la classe ?

C'est le cœur du sujet — et une question classique en entretien. Le contrôleur
déclare dépendre de `ITaskService` (le **contrat**), pas de `TaskService`
(l'**implémentation**) :

```csharp
public class TodoController(ITaskService taskService, ...) : Controller
```

Bénéfices :
1. **Inversion de dépendance** (le « D » de SOLID) : la couche haute (contrôleur)
   dépend d'une abstraction, pas d'un détail concret. On peut remplacer
   l'implémentation (ex. une version qui met en cache, ou qui tape une autre base)
   **sans toucher au contrôleur**.
2. **Testabilité** : en test, on injecte un **mock** de l'interface. C'est ce qui
   permet de tester `TodoController` **sans base de données** (voir section 14).
3. **Lisibilité du contrat** : l'interface liste, en un coup d'œil, tout ce que la
   couche métier sait faire.

### Comment le framework relie l'interface à la classe ?

Par l'**injection de dépendances** (section 3). Dans `Program.cs`, une ligne suffit :

```csharp
builder.Services.AddScoped<ITaskService, TaskService>();
```

Elle dit : « quand quelqu'un demande `ITaskService`, fabrique-lui un `TaskService` ».
`AddScoped` = une instance par requête HTTP (même durée de vie que le `DbContext`
qu'elle utilise). Changer d'implémentation partout dans l'app = changer **cette
seule ligne**.

### Et les DTO de l'API ?

Dans le même esprit de séparation, l'API n'expose pas l'entité `TodoTask` (qui
reflète la table) mais des **DTO** (dossier `Dtos/`) : `TaskDto` en sortie,
`SaveTaskDto` en entrée. Ça sécurise (on ne divulgue pas `UserId`), fige le contrat
JSON, et documente précisément l'API (voir section 10). `SaveTaskDto`, dépourvu de
`Id`/`UserId`, est la version API du `[Bind]` : la parade à l'over-posting.

> **À retenir pour un entretien** : injecter `DbContext` directement dans un
> contrôleur n'est pas « faux » (Microsoft le montre pour les petits exemples, et
> `DbContext` est déjà un Unit of Work + Repository — d'où le fait qu'ajouter un
> *repository générique* par-dessus EF est souvent un anti-pattern). **Mais** une
> **couche de services avec interfaces** est le standard attendu dès qu'il y a de la
> vraie logique métier : elle découple, centralise les règles, et rend le tout
> testable. C'est le juste milieu — ni tout dans le contrôleur, ni sur-abstraction.

---

## 16. Glossaire express

| Terme | En une phrase |
|---|---|
| **Middleware** | Un maillon de la chaîne que traverse chaque requête HTTP. |
| **Pipeline** | La suite ordonnée de middlewares (défini dans `Program.cs`). |
| **DI / Injection de dépendances** | Le framework fabrique et fournit les objets réclamés dans les constructeurs. |
| **Service (couche métier)** | Une classe (derrière une interface) qui porte la logique applicative, appelée par les contrôleurs. |
| **Interface** | Un contrat (`ITaskService`) : la liste des méthodes, sans le code. Le contrôleur en dépend, pas de la classe concrète. |
| **Inversion de dépendance** | Dépendre d'abstractions (interfaces), pas d'implémentations concrètes (le « D » de SOLID). |
| **Fat controller** | Anti-modèle : un contrôleur qui contient la logique métier au lieu de la déléguer à un service. |
| **DbContext** | Le point d'accès EF Core à la base ; chaque `DbSet` est une table. |
| **Entité** | Une classe mappée sur une table (`TodoTask`). |
| **Migration** | Un fichier décrivant une évolution du schéma de la base. |
| **IQueryable** | Une requête LINQ **différée**, exécutée seulement au `ToListAsync()`. |
| **Action** | Une méthode publique d'un contrôleur qui traite une requête. |
| **Model binding** | Le remplissage automatique des paramètres d'action depuis la requête. |
| **Tag Helper** | Attribut `asp-*` qui génère du HTML lié au modèle (équiv. binding). |
| **Claim** | Une info sur l'utilisateur (id, e-mail…) stockée dans le cookie ou le JWT. |
| **JWT** | Jeton signé, sans état, prouvant l'identité pour appeler l'API. |
| **DTO** | Objet de transfert : une forme de données taillée pour l'échange (ici, les projections `Select(new {...})`). |
| **CSRF / Anti-forgery** | Attaque où un site tiers déclenche une action à ta place ; contrée par `[ValidateAntiForgeryToken]`. |
| **IDOR** | Accéder à la ressource d'autrui en changeant l'id ; contré par le filtre `UserId` partout. |
| **XSS** | Injection de code dans une page ; contrée par la validation + l'échappement Razor. |

---

## 17. Par où commencer à lire le code

Ordre de lecture conseillé pour un premier parcours (chaque fichier est commenté) :

1. **`Program.cs`** — la vue d'ensemble : services (dont la couche métier) + pipeline. *(sections 2, 3, 15)*
2. **`Models/TodoTask.cs`** et **`Models/Category.cs`** — les entités et la validation. *(sections 7, 12)*
3. **`Data/AppDbContext.cs`** — DbSet, relations, seed. *(sections 7, 8)*
4. **`Services/ITaskService.cs`** puis **`TaskService.cs`** — le contrat métier et son implémentation. *(sections 15, 7)*
5. **`Controllers/TodoController.cs`** — un contrôleur FIN qui délègue au service. *(sections 4, 15)*
6. **`Controllers/CategoryController.cs`** — le CRUD MVC le plus simple. *(sections 4, 5, 13)*
7. **`Helpers/PaginatedList.cs`** — la pagination générique en action. *(section 7)*
8. **`Dtos/`** puis **`Controllers/Api/AuthApiController.cs`** et **`TaskApiController.cs`** — les DTO, l'API + JWT. *(sections 9, 10, 15)*
9. **`TaskFlow.Tests/`** — comment on vérifie tout ça (services + contrôleurs mockés). *(section 14)*

Bonne exploration — et n'hésite pas à modifier, casser, relancer les tests : c'est la
meilleure façon d'apprendre. 🚀
