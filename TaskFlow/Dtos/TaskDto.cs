using TaskFlow.Models;

namespace TaskFlow.Dtos;

// ===================== DTO (Data Transfer Object) =====================
//
// Un DTO est une forme de données taillée pour L'ÉCHANGE (ici, le JSON de l'API),
// distincte de l'ENTITÉ (TodoTask, qui reflète la table).
//
// Pourquoi ne pas exposer l'entité directement dans l'API ?
//   1. Sécurité : l'entité contient UserId, des propriétés de navigation
//      (Category, Comments…) qu'on ne veut PAS divulguer ni sérialiser.
//   2. Contrat stable : le JSON ne change pas quand on modifie le schéma SQL.
//   3. Documentation : OpenAPI/Scalar décrit précisément un type NOMMÉ (un `record`),
//      alors qu'un objet anonyme apparaît comme un schéma vague.
//
// On utilise des `record` : types immuables concis, parfaits pour un DTO.

/// <summary>Représentation d'une tâche renvoyée par l'API.</summary>
/// <param name="Id">Identifiant unique de la tâche, généré par le serveur.</param>
/// <param name="Title">Titre de la tâche.</param>
/// <param name="Description">Description libre, ou <c>null</c> si absente.</param>
/// <param name="Priority">Niveau de priorité : <c>Low</c>, <c>Medium</c> ou <c>High</c>.</param>
/// <param name="IsCompleted"><c>true</c> si la tâche est terminée.</param>
/// <param name="DueDate">Date d'échéance (format ISO 8601).</param>
/// <param name="CategoryId">Identifiant de la catégorie rattachée, ou <c>null</c>.</param>
/// <param name="CategoryName">Nom de la catégorie, ou <c>null</c> si aucune.</param>
/// <param name="CategoryColor">Couleur (hex) de la catégorie, ou <c>null</c> si aucune.</param>
public record TaskDto(
    int Id,
    string Title,
    string? Description,
    PriorityLevel Priority,
    bool IsCompleted,
    DateTime DueDate,
    int? CategoryId,
    string? CategoryName,
    string? CategoryColor);
