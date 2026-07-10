# TaskFlow

Application web de **gestion de tâches** (todo-list collaborative) développée en
**ASP.NET Core 10 MVC** avec **Entity Framework Core**, **ASP.NET Core Identity**
et une **API REST** documentée. Le projet sert de support d'apprentissage à la
transition **C# WinForms/WPF → ASP.NET Core** : le code est abondamment commenté
dans cette optique.

> Nouveau sur ASP.NET Core et venant du desktop (WinForms/WPF) ? Commence par le
> **[Guide débutant](docs/GUIDE-DEBUTANT-ASPNETCORE.md)** : il explique chaque
> brique (pipeline, injection de dépendances, MVC, EF Core, Identity, tests) en
> partant de ce que tu connais déjà.

---

## Sommaire

- [Fonctionnalités](#fonctionnalités)
- [Stack technique](#stack-technique)
- [Prérequis](#prérequis)
- [Démarrage rapide](#démarrage-rapide)
- [Structure de la solution](#structure-de-la-solution)
- [Base de données & migrations](#base-de-données--migrations)
- [API REST & documentation](#api-rest--documentation)
- [Tests](#tests)
- [Sécurité](#points-de-sécurité)
- [Pistes d'amélioration](#pistes-damélioration)

---

## Fonctionnalités

- **Authentification** complète (inscription, connexion, déconnexion) via ASP.NET Core Identity.
- **CRUD des tâches** : titre, description, priorité, échéance, catégorie, statut, assignation à un utilisateur.
- **Tableau de bord** : compteurs (total, terminées, en cours, en retard), pourcentage d'avancement, top 5 des tâches urgentes.
- **Liste des tâches** avec **recherche**, **filtres** (statut / priorité / catégorie) et **pagination**.
- **Catégories** colorées (CRUD).
- **Commentaires** sur chaque tâche.
- **Pièces jointes** (upload / téléchargement / suppression, stockées sur disque).
- **API REST** (`/api/TaskApi`) sécurisée par **JWT**, avec documentation interactive **Scalar**.

Chaque tâche/commentaire/pièce jointe est **cloisonné par utilisateur** : on ne
voit et ne modifie que ses propres données (protection contre les failles IDOR).

---

## Stack technique

| Domaine | Technologie |
|---|---|
| Framework | ASP.NET Core **10** (MVC + Razor Views) |
| Langage | C# 13 (`net10.0`, nullable + implicit usings activés) |
| ORM | Entity Framework Core 10 |
| Base de données | SQLite (fichier `taskflow.db`) |
| Authentification web | ASP.NET Core Identity (cookies) |
| Authentification API | JWT Bearer |
| Documentation API | OpenAPI natif (`Microsoft.AspNetCore.OpenApi`) + Scalar |
| UI | Razor + Bootstrap 5 + jQuery Validation |
| Tests | xUnit + Moq + EF Core InMemory |

---

## Prérequis

- **.NET SDK 10.0** ou supérieur → `dotnet --version` doit renvoyer `10.x`.
- Un éditeur : Visual Studio 2022+, VS Code (extension C# Dev Kit) ou Rider.
- Aucun serveur de base de données à installer : **SQLite** est un simple fichier.

> Il n'y a pas de fichier `.sln` : on cible directement les `.csproj`. Tu peux en
> créer un si tu préfères ouvrir la solution d'un bloc :
> `dotnet new sln && dotnet sln add TaskFlow/TaskFlow.csproj TaskFlow.Tests/TaskFlow.Tests.csproj`

---

## Démarrage rapide

```bash
# 1. Restaurer les dépendances NuGet (l'équivalent de "récupérer les DLL")
dotnet restore TaskFlow/TaskFlow.csproj

# 2. Créer / mettre à jour la base SQLite à partir des migrations
#    (installe l'outil une seule fois si besoin : dotnet tool install --global dotnet-ef)
dotnet ef database update --project TaskFlow

# 3. Lancer l'application
dotnet run --project TaskFlow
```

L'application démarre sur :

- **Application web** : <https://localhost:7087> (ou <http://localhost:5151>)
- **Documentation API (Scalar)** : <https://localhost:7087/scalar/v1> *(uniquement en environnement Development)*

Au premier lancement, 4 catégories et 3 tâches de démonstration sont déjà en base
(données de *seed*). Crée un compte via **Register** pour commencer.

> **Astuce** : pour repartir d'une base propre, supprime `TaskFlow/taskflow.db`
> puis relance `dotnet ef database update --project TaskFlow`.

---

## Structure de la solution

```
MVCTaskFlow/
├─ TaskFlow/                       # Le projet web (application ASP.NET Core MVC)
│  ├─ Program.cs                   # Point d'entrée : services + pipeline HTTP
│  ├─ appsettings.json             # Configuration (chaîne de connexion, logs…)
│  ├─ Controllers/                 # FINS : traduisent HTTP <-> appels de services
│  │  ├─ HomeController.cs         #   tableau de bord + page d'erreur
│  │  ├─ TodoController.cs         #   tâches (filtres/pagination)
│  │  ├─ CategoryController.cs     #   catégories
│  │  ├─ CommentController.cs      #   commentaires
│  │  ├─ AttachmentController.cs   #   pièces jointes
│  │  └─ API/                      #   contrôleurs REST (renvoient du JSON)
│  │     ├─ TaskApiController.cs   #     endpoints CRUD des tâches (JWT)
│  │     └─ AuthApiController.cs   #     endpoint /login qui délivre le JWT
│  ├─ Services/                    # ⭐ COUCHE MÉTIER : interfaces + implémentations
│  │  ├─ ITaskService / TaskService            #   logique des tâches + dashboard
│  │  ├─ ICategoryService / CategoryService
│  │  ├─ ICommentService / CommentService
│  │  ├─ IAttachmentService / AttachmentService
│  │  └─ IJwtTokenService / JwtTokenService    #   fabrication des jetons JWT
│  ├─ Dtos/                        # Objets d'échange JSON de l'API (TaskDto, SaveTaskDto…)
│  ├─ Models/                      # Entités (= tables) + ViewModels (= écrans)
│  │  ├─ TodoTask.cs, Category.cs, Comment.cs, Attachment.cs
│  │  └─ DashboardViewModel.cs, ErrorViewModel.cs
│  ├─ Data/AppDbContext.cs         # Le "chef d'orchestre" EF Core (DbSet, relations, seed)
│  ├─ Helpers/PaginatedList.cs     # Pagination générique réutilisable
│  ├─ Migrations/                  # Historique versionné du schéma de la base
│  ├─ Views/                       # Pages Razor (.cshtml) — le HTML généré
│  └─ wwwroot/                     # Fichiers statiques (CSS, JS, uploads, libs)
│
└─ TaskFlow.Tests/                 # Projet de tests unitaires (xUnit)
   ├─ TestHelpers.cs               # fabriques partagées (InMemory, mocks)
   ├─ Services/TaskServiceTests.cs          # logique métier (base InMemory)
   ├─ Services/JwtTokenServiceTests.cs
   ├─ Controllers/TodoControllerTests.cs    # contrôleur MVC (service mocké)
   ├─ Controllers/TaskApiControllerTests.cs # contrôleur API (service mocké)
   └─ Models/TodoTaskTests.cs
```

**Architecture en couches** — la règle d'or : le contrôleur ne fait QUE du HTTP,
la logique métier vit dans les services, l'accès aux données dans EF Core. Chaque
contrôleur dépend d'une **interface** de service (pas d'une classe concrète), ce qui
découple les couches et rend les tests triviaux (on injecte un mock de l'interface).

```
Navigateur → Program.cs (pipeline) → Routage → TodoController.Index()   (HTTP)
          → ITaskService.GetPagedAsync()                                (métier)
          → AppDbContext (EF Core → SQL SQLite)                         (données)
          → View "Todo/Index.cshtml" → HTML

App mobile / JS → TaskApiController → ITaskService (le MÊME) → EF Core → JSON (DTO)
```

Le service web (`TodoController`) et le service API (`TaskApiController`) partagent
**le même `ITaskService`** : la logique métier n'est écrite qu'une seule fois.

---

## Base de données & migrations

Le schéma est décrit **en code C#** (les classes de `Models/`) et versionné par
des **migrations** dans `Migrations/`. Une migration est un fichier généré qui
décrit *comment passer d'un état de la base au suivant* — équivalent versionné
et rejouable de scripts `ALTER TABLE`.

```bash
# Créer une nouvelle migration après avoir modifié un modèle
dotnet ef migrations add NomDeLaMigration --project TaskFlow

# Appliquer les migrations en attente à la base
dotnet ef database update --project TaskFlow

# Annuler la dernière migration NON appliquée
dotnet ef migrations remove --project TaskFlow
```

Relations et comportements de suppression configurés dans `AppDbContext` :

- Supprimer une **catégorie** → les tâches liées passent à *sans catégorie* (`SetNull`).
- Supprimer une **tâche** → ses commentaires et pièces jointes sont supprimés (`Cascade`).

---

## API REST & documentation

L'API est exposée sous `/api/TaskApi` et **sécurisée par JWT**. Elle suit les
conventions REST :

| Verbe | Route | Rôle |
|---|---|---|
| `GET` | `/api/TaskApi` | Liste des tâches de l'utilisateur |
| `GET` | `/api/TaskApi/{id}` | Détail d'une tâche |
| `POST` | `/api/TaskApi` | Créer une tâche (→ 201 Created) |
| `PUT` | `/api/TaskApi/{id}` | Remplacer une tâche (→ 204 No Content) |
| `PATCH` | `/api/TaskApi/{id}` | Basculer terminé/en cours |
| `DELETE` | `/api/TaskApi/{id}` | Supprimer une tâche (→ 204 No Content) |
| `POST` | `/api/AuthApi/login` | S'authentifier et récupérer un token JWT |

**Documentation interactive** : en environnement *Development*, ouvre
<https://localhost:7087/scalar/v1>. Le document OpenAPI brut est sur `/openapi/v1.json`.

**Comment tester l'API** :

1. `POST /api/AuthApi/login` avec `{ "email": "...", "password": "..." }` → récupère le champ `token`.
2. Dans Scalar, clique sur **Authentication / Bearer** et colle le token.
3. Appelle n'importe quel endpoint `/api/TaskApi`.

En ligne de commande :

```bash
# 1. Obtenir un token
curl -k -X POST https://localhost:7087/api/AuthApi/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Password1"}'

# 2. Appeler l'API avec le token
curl -k https://localhost:7087/api/TaskApi \
  -H "Authorization: Bearer <TON_TOKEN>"
```

---

## Tests

Le projet `TaskFlow.Tests` utilise **xUnit** (framework de test), **Moq**
(simulation de dépendances) et **EF Core InMemory** (base de données factice en
mémoire, pour ne pas toucher au vrai SQLite).

```bash
# Lancer tous les tests
dotnet test TaskFlow.Tests/TaskFlow.Tests.csproj
```

**Couverture actuelle** (26 tests) :

| Fichier | Ce qui est couvert |
|---|---|
| `Services/TaskServiceTests.cs` | Logique métier sur base InMemory : isolation par utilisateur, filtres, création, anti-IDOR (édition/màj d'une tâche d'autrui), suppression, bascule, calcul du tableau de bord |
| `Services/JwtTokenServiceTests.cs` | Génération du JWT (claims, expiration) et échec si clé absente |
| `Controllers/TodoControllerTests.cs` | Contrôleur MVC avec `ITaskService` **mocké** : View/Redirect/NotFound |
| `Controllers/TaskApiControllerTests.cs` | Contrôleur API mocké : codes REST (200/201/204/404) et mapping DTO |
| `Models/TodoTaskTests.cs` | Valeurs par défaut, bascule de statut, valeurs de l'enum, échéance |

La logique étant désormais dans les **services**, c'est là qu'on la teste (base
InMemory) ; les **contrôleurs** se testent avec des **mocks d'interfaces**, sans base
de données. Le flux JWT complet (register → login → appel API protégé) a en plus été
validé en bout de chaîne. Voir la section *Tests* du
[Guide débutant](docs/GUIDE-DEBUTANT-ASPNETCORE.md) pour la marche à suivre.

---

## Points de sécurité

Le projet applique déjà plusieurs bonnes pratiques ; certaines restent à durcir
**avant une mise en production** :

**En place :**
- Cloisonnement par utilisateur sur toutes les opérations, centralisé dans les services (anti-IDOR).
- **Authentification API par JWT réellement appliquée** : `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]` sur l'API (voir l'encadré ci-dessous).
- `[ValidateAntiForgeryToken]` sur tous les POST des formulaires (anti-CSRF).
- `[Bind]` (MVC) et **DTO d'entrée** `SaveTaskDto` (API) en liste blanche (anti-*over-posting*).
- Validation stricte de la couleur de catégorie par regex (anti-XSS).
- Upload de fichiers : liste blanche d'extensions, taille maximale, nom stocké en GUID (anti-traversée de chemin).
- Mots de passe hachés par Identity (jamais en clair) ; verrouillage après échecs de connexion.
- Clé secrète JWT **sortie du code**, lue en configuration avec échec rapide si absente.
- Packages : metapackage hérité `Microsoft.AspNetCore` supprimé, `Microsoft.OpenApi` forcé à une version corrigée (2.7.5). **Build : 0 avertissement.**

> #### ⚠️ Le piège JWT vs Cookie (corrigé)
> `AddDefaultIdentity()` fixe le schéma d'authentification **par défaut** sur le
> **cookie**. Un simple `[Authorize]` sur l'API utiliserait donc le cookie, jamais
> le JWT — le token de `/api/AuthApi/login` serait inutile. Il faut **nommer
> explicitement** le schéma Bearer sur les contrôleurs d'API. Vérifié : sans token,
> l'API renvoie désormais `401` (et non une redirection vers la page de login).

**Reste à durcir pour la production :**
- 🟡 En production, fournir `Jwt:SecretKey` via variable d'environnement / *user-secrets* / coffre-fort (en dev il est dans `appsettings.Development.json`).
- 🟡 `RequireConfirmedAccount = false` : la confirmation d'e-mail est désactivée (pratique en dev, à activer en prod).

---

## Pistes d'amélioration

Les gros chantiers architecturaux ont été traités (couche de services + interfaces,
DTO d'API, correctif JWT, doc OpenAPI enrichie, montée de couverture des tests).
Restent des pistes secondaires :

- Ajouter la **gestion des rôles** (admin/user) — l'infrastructure `AddRoles` est déjà en place mais inutilisée.
- Pousser les **agrégats du tableau de bord côté SQL** (`Count`/`GroupBy`) si le volume de tâches devient important.
- Ajouter des tests pour `CommentService` et `AttachmentService` (upload/validation).
- En production : *user-secrets*/coffre-fort pour la configuration sensible, activation de la confirmation d'e-mail.

---

## Licence

Projet pédagogique — libre d'utilisation dans le cadre de l'apprentissage.
