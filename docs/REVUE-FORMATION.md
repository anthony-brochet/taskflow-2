# Revue de formation — ce qui a changé et pourquoi

Ce document récapitule les **défauts relevés dans le code initial de la formation**
et les **corrections/ajouts** apportés. Objectif : comme le code d'origine n'a pas
été conservé, tu retrouves ici, « en creux », ce qu'il contenait — et surtout la
**leçon** de chaque point pour progresser.

> Légende de gravité : 🔴 critique · 🟠 important · 🟡 mineur / confort.

---

## 1. Bugs fonctionnels

| # | Défaut dans le code initial | Correction | Leçon |
|---|---|---|---|
| 🔴 | **JWT jamais appliqué** : `AddDefaultIdentity()` fixe le schéma d'auth par défaut sur le **cookie** ; `[Authorize]` sur l'API utilisait donc le cookie, pas le JWT. Le token de `/api/AuthApi/login` ne servait à rien. | `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]` sur l'API. | Quand une app mêle Identity (cookie) et une API (JWT), il faut **nommer explicitement** le schéma sur l'API. |
| 🟠 | **`HomeController` sans action `Error`** alors que `UseExceptionHandler("/Home/Error")` et la vue `Error.cshtml` existent → page d'erreur cassée en production. | Ajout de l'action `Error()` avec `ErrorViewModel`. | Un `UseExceptionHandler("/X/Y")` implique qu'une action `Y` existe réellement. |
| 🟠 | **Barre de filtres Razor dans le `else`** de `@if (!Model.Any())` (`Todo/Index.cshtml`) : quand un filtre ne renvoyait aucun résultat, la barre disparaissait → impossible de réinitialiser. | Barre de filtres sortie du `if/else`, toujours affichée. | Ne jamais enfermer un contrôle de navigation dans la branche « liste non vide ». |
| 🟠 | **Test `Index_ReturnsOnlyUserTasks` faux** : `AddRange(...)` **sans** `SaveChangesAsync()` → la base InMemory était vide, le test « passait » pour une mauvaise raison. | Ajout de `SaveChangesAsync()`. | En EF, rien n'est persisté tant qu'on ne sauve pas (voir aussi la couche données). |
| 🟠 | **Test `DeleteConfirmed` inopérant** : tâche insérée avec `Id = 51`, mais `DeleteConfirmed(1)` appelé → rien n'était supprimé, le test ne testait rien. | Appel corrigé sur le bon id. | Un test vert ne prouve rien s'il ne teste pas ce qu'on croit. Lire le message d'échec. |

---

## 2. Sécurité

| # | Défaut / manque | Correction | Leçon |
|---|---|---|---|
| 🔴 | **Clé secrète JWT codée en dur** (et dupliquée) : `"TaskFlow-Super-Cle-Secrete-Only-Dev"` dans `Program.cs` et `AuthApiController`. | Clé lue en configuration (`Jwt:SecretKey`), **échec rapide** si absente, dev dans `appsettings.Development.json`. | Un secret dans le code source finit dans Git. Configuration + coffre-fort en prod. |
| 🟠 | **API bindait l'entité `TodoTask`** directement → over-posting possible (`Id`, `UserId`, `IsCompleted`). | DTO d'entrée `SaveTaskDto` (sans `Id`/`UserId`). | L'équivalent API du `[Bind]` : n'accepter en entrée que les champs autorisés. |
| 🟡 | Cloisonnement par utilisateur présent mais **dupliqué dans chaque contrôleur**. | Centralisé dans les services (`ExistsForUserAsync`, filtres `UserId`). | Une règle de sécurité écrite une seule fois est une règle qu'on n'oublie pas. |

> ✅ Points **déjà bons** dans le code initial (à conserver) : `[ValidateAntiForgeryToken]`
> sur les POST, `[Bind]` sur `Create` MVC, regex anti-XSS sur la couleur de catégorie,
> upload durci (liste blanche + GUID + taille max), mots de passe hachés par Identity.

---

## 3. Architecture

| # | Défaut | Correction | Leçon |
|---|---|---|---|
| 🔴 | **Aucune interface, aucune couche métier** : « fat controllers » contenant requêtes EF, filtres, règles et calculs. Logique **dupliquée** entre `TodoController` (web) et `TaskApiController` (API). | Couche de **services avec interfaces** (`ITaskService`…), contrôleurs **fins**, logique écrite une seule fois et partagée web/API. | Séparer les couches (présentation / métier / données). Dépendre d'**interfaces** (inversion de dépendance). Voir la section 15 du guide débutant. |
| 🟠 | API renvoyant des **objets anonymes** → contrat JSON flou, doc OpenAPI vague. | **DTO nommés** (`TaskDto`) → schémas précis dans OpenAPI/Scalar. | Un type nommé = un contrat documentable et stable. |

---

## 4. Packages & configuration

| # | Défaut | Correction |
|---|---|---|
| 🟠 | `Microsoft.OpenApi 2.0.0` (transitif) **vulnérable** — avertissement `NU1903`. | Version forcée à **2.7.5** (corrigée). |
| 🟠 | Référence `Microsoft.AspNetCore 2.3.11` : metapackage **hérité de .NET Core 2.x**, inutile — avertissement `NU1510`. | Supprimée. |
| 🟡 | `.gitignore` présent **uniquement** dans le sous-dossier `TaskFlow/` (ne couvrait ni `TaskFlow.tests/bin`, ni le `.DS_Store` racine). | `.gitignore` **racine** ajouté. |

Résultat : **build à 0 avertissement**.

---

## 5. Documentation

| Manque initial | Ajout |
|---|---|
| Pas de `README`. | `README.md` : présentation, compilation, exécution, structure, API, tests, sécurité. |
| Doc API pauvre (aucun résumé, aucun `ProducesResponseType`). | `[ProducesResponseType]` + résumés XML `///` → OpenAPI/Scalar enrichi. |
| Pas de guide d'ensemble. | `docs/GUIDE-DEBUTANT-ASPNETCORE.md` (17 sections, transition WinForms→ASP.NET). |
| — | Ce document (`docs/REVUE-FORMATION.md`). |

---

## 6. Conventions (tous corrigés)

| Point | Constat initial | Correction |
|---|---|---|
| Dossier API | `Controllers/API` (majuscules) ≠ namespace `Api` | ✅ `Controllers/Api`. |
| Projet de test | `TaskFlow.tests` (minuscule) + sous-dossier `Tests/` → namespace redondant `TaskFlow.Tests.Tests` | ✅ Renommé `TaskFlow.Tests`, sous-dossier aplati, namespaces `TaskFlow.Tests.*`. |
| Style de namespace | Mélange `namespace X { }` (bloc) et `namespace X;` (file-scoped) | ✅ Uniformisé en **file-scoped** partout (hors `Migrations/`, code généré). |
| Un type par fichier | `TaskDtos.cs`/`AuthDtos.cs` groupaient plusieurs records ; l'enum `PriorityLevel` était dans `TodoTask.cs` | ✅ Un type par fichier (`TaskDto.cs`, `SaveTaskDto.cs`, `AuthTokenDto.cs`, `LoginResponseDto.cs`, `PriorityLevel.cs`, `UploadResult.cs`, `AttachmentFile.cs`). |
| Placement des types de résultat | `UploadResult`/`AttachmentFile` (résultats internes renvoyés par un service) trainaient dans `Services/`, puis un temps dans `Dtos/` (incohérent : tout `Dtos/` finit en `*Dto`) | ✅ Placés dans `Services/Results/`. Ce ne sont **ni des services** (pas de suffixe `Service`) **ni des DTO** (jamais sérialisés ; `AttachmentFile` porte même un chemin disque serveur). `Dtos/` = uniquement les contrats JSON de l'API. |

---

## 7. Tests — avant / après

| | Avant | Après |
|---|---|---|
| Nombre | 10 (dont 2 cassés) | **26** (tous verts) |
| Où la logique est testée | Contrôleur + base InMemory | **Services** (base InMemory) — là où vit la logique |
| Contrôleurs | Testés avec une vraie base | Testés avec **mocks d'interfaces** (sans base) |
| API / JWT | Non testés | `TaskApiControllerTests`, `JwtTokenServiceTests` + **test de bout en bout** (register→login→appel API) |

**Bonus — une leçon apparue pendant la refonte** : un DTO `record` avec
`[property: Required]` compilait et passait les tests **unitaires** (qui
construisent le DTO en direct), mais l'API renvoyait **500** en réel, car MVC exige
la validation sur le **paramètre** du constructeur. Seul un **test d'intégration**
(lancer l'app, appeler l'API) l'a révélé. Morale : tester au bon niveau, et un
test unitaire vert ne remplace pas un essai réel.

---

## 8. Affinage : dénormalisation → navigation (donnée d'état courant)

Le code initial stockait `TodoTask.AssignedToUserName` (l'e-mail de l'assigné, copié
au moment de l'écriture) « pour éviter une jointure ». Deux problèmes :
- **Péremption** : si l'assigné change d'e-mail, la tâche gardait l'ancien. Or une
  assignation est un **état courant** — on veut toujours l'e-mail *actuel*.
- La jointure évitée se fait sur la **clé primaire** d'AspNetUsers (indexée) : elle
  est bon marché. L'« optimisation » ne se justifiait pas.

**Correction** : on ne stocke plus que la clé étrangère `AssignedToUserId`, avec une
vraie **propriété de navigation** `AssignedToUser` (relation configurée dans
`AppDbContext`, `OnDelete: SetNull`). L'e-mail est **résolu à la lecture** via
`.Include(t => t.AssignedToUser)`. Colonne `AssignedToUserName` supprimée (migration
`ReplaceAssignedToUserNameWithFk`). Bonus : le service `TaskService` n'a même plus
besoin de `UserManager`.

> **La nuance à retenir** : `Comment.UserName`, lui, est **conservé** — mais comme
> un **instantané historique assumé** (« qui a écrit ça, tel qu'il s'appelait à
> l'époque », comme l'auteur d'un commit Git). Dénormaliser est bon quand on veut
> figer un instantané ; mauvais quand on veut refléter un état courant.

---

## 9. En résumé

Le code initial était **propre et déjà sécurisé sur plusieurs points**, mais il
souffrait de deux faiblesses majeures — **l'authentification JWT non appliquée**
(sécurité) et **l'absence de couche de services / d'interfaces** (architecture) —
plus quelques bugs (page d'erreur, filtres Razor, tests faux) et des points de
configuration. Tout est corrigé, vérifié (build 0 avertissement, 26 tests verts,
flux JWT validé de bout en bout) et documenté.
