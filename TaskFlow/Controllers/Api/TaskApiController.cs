using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Dtos;
using TaskFlow.Models;
using TaskFlow.Services;

namespace TaskFlow.Controllers.Api;

// ================== CONTRÔLEUR D'API REST ==================
//
// Différence avec les contrôleurs MVC : celui-ci hérite de ControllerBase (pas
// de vues) et renvoie des DONNÉES (JSON via des DTO). Il est appelé par du code
// (app mobile, JavaScript, Postman), pas par un humain qui clique.
//
// Comme les contrôleurs MVC, il est désormais FIN : la logique est dans
// ITaskService (le MÊME service que le TodoController web — zéro duplication).
// Il ne fait que : (dé)sérialiser DTO <-> entité et choisir le code HTTP.
//
//   [ApiController]  -> validation auto du modèle (400 si DTO invalide), binding JSON.
//   [Route("api/[controller]")] -> URL /api/TaskApi.
//   [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
//     -> exige un JWT valide. ⚠️ On PRÉCISE le schéma Bearer : sans ça, [Authorize]
//        utiliserait le schéma par défaut (le cookie Identity, fixé par
//        AddDefaultIdentity), et le JWT ne serait jamais validé. Piège classique.
//   [Produces("application/json")] -> documente que l'API répond en JSON.
[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Produces("application/json")]
public class TaskApiController(ITaskService taskService, UserManager<IdentityUser> userManager)
    : ControllerBase
{
    private readonly ITaskService _taskService = taskService;
    private readonly UserManager<IdentityUser> _userManager = userManager;

    /// <summary>Liste les tâches de l'utilisateur authentifié.</summary>
    /// <remarks>
    /// Renvoie **toutes** les tâches appartenant au porteur du jeton JWT.
    /// Aucun paramètre : le périmètre est déterminé par l'utilisateur authentifié.
    /// </remarks>
    /// <response code="200">La liste des tâches (éventuellement vide).</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TaskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TaskDto>>> GetAll()
    {
        var userId = _userManager.GetUserId(User);

        // On réutilise le service : page unique très large pour tout récupérer.
        var page = await _taskService.GetPagedAsync(userId, null, null, null, 1, int.MaxValue);

        return Ok(page.Select(ToDto));
    }

    /// <summary>Récupère une tâche par son identifiant.</summary>
    /// <param name="id">Identifiant **numérique** de la tâche à récupérer (ex. <c>42</c>).</param>
    /// <response code="200">La tâche demandée.</response>
    /// <response code="404">Aucune tâche avec cet identifiant n'appartient à l'utilisateur.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> GetById(int id)
    {
        var task = await _taskService.GetDetailsAsync(_userManager.GetUserId(User), id);
       
        if (task == null) return NotFound(new { message = "Tâche non trouvée." });

        return Ok(ToDto(task));
    }

    /// <summary>Crée une nouvelle tâche.</summary>
    /// <param name="input">
    /// Données de la tâche à créer. Les champs <c>Id</c> et <c>UserId</c> sont **ignorés** :
    /// le serveur les fixe lui-même (protection contre l'over-posting).
    /// </param>
    /// <response code="201">Tâche créée. L'en-tête <c>Location</c> pointe vers la nouvelle ressource.</response>
    /// <response code="400">Données invalides (ex. titre manquant).</response>
    [HttpPost]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskDto>> Create(SaveTaskDto input)
    {
        // On construit l'entité à partir du DTO : le client ne peut donc PAS
        // fixer Id/UserId (over-posting impossible côté API).
        var task = new TodoTask
        {
            Title = input.Title,
            Description = input.Description,
            Priority = input.Priority,
            IsCompleted = input.IsCompleted,
            DueDate = input.DueDate,
            CategoryId = input.CategoryId
        };

        await _taskService.CreateAsync(_userManager.GetUserId(User), task);

        // 201 Created + en-tête Location pointant vers GetById(id).
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, ToDto(task));
    }

    /// <summary>Remplace une tâche existante.</summary>
    /// <remarks>Remplacement **complet** (sémantique PUT) : tous les champs fournis écrasent l'existant.</remarks>
    /// <param name="id">Identifiant de la tâche à modifier.</param>
    /// <param name="input">Nouvelle version **complète** de la tâche.</param>
    /// <response code="204">Tâche mise à jour, aucun contenu renvoyé.</response>
    /// <response code="404">Aucune tâche avec cet identifiant n'appartient à l'utilisateur.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, SaveTaskDto input)
    {
        var task = new TodoTask
        {
            Id = id,
            Title = input.Title,
            Description = input.Description,
            Priority = input.Priority,
            IsCompleted = input.IsCompleted,
            DueDate = input.DueDate,
            CategoryId = input.CategoryId
        };

        bool updated = await _taskService.UpdateAsync(_userManager.GetUserId(User), id, task);
       
        if (!updated) return NotFound(new { message = "Tâche non trouvée." });

        return NoContent();
    }

    /// <summary>Supprime une tâche.</summary>
    /// <param name="id">Identifiant de la tâche à supprimer. **Action irréversible.**</param>
    /// <response code="204">Tâche supprimée, aucun contenu renvoyé.</response>
    /// <response code="404">Aucune tâche avec cet identifiant n'appartient à l'utilisateur.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        bool deleted = await _taskService.DeleteAsync(_userManager.GetUserId(User), id);
        
        if (!deleted) return NotFound(new { message = "Tâche non trouvée." });

        return NoContent();
    }

    /// <summary>Bascule le statut terminé / en cours d'une tâche.</summary>
    /// <remarks>Inverse le champ <c>IsCompleted</c> : **terminé → en cours** ou l'inverse.</remarks>
    /// <param name="id">Identifiant de la tâche dont on bascule le statut.</param>
    /// <response code="200">Renvoie le nouvel état, ex. <c>{ "id": 42, "isCompleted": true }</c>.</response>
    /// <response code="404">Aucune tâche avec cet identifiant n'appartient à l'utilisateur.</response>
    [HttpPatch("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Toggle(int id)
    {
        bool? newState = await _taskService.ToggleAsync(_userManager.GetUserId(User), id);
        
        if (newState == null) return NotFound(new { message = "Tâche non trouvée." });

        return Ok(new { Id = id, IsCompleted = newState.Value });
    }

    // Mapping entité -> DTO, centralisé.
    private static TaskDto ToDto(TodoTask t) => new(
        t.Id, t.Title, t.Description, t.Priority, t.IsCompleted, t.DueDate,
        t.CategoryId, t.Category?.Name, t.Category?.Color);
}
