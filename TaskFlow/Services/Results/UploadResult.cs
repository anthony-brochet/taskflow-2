namespace TaskFlow.Services.Results;

// Type de RÉSULTAT (pas un DTO) : renvoyé par un service à un contrôleur, en
// interne. Il n'est jamais sérialisé en JSON ni exposé à un client — d'où sa place
// dans Services/Results/ et non dans Dtos/, et l'absence de suffixe « Dto ».

/// <summary>Résultat d'un upload : succès, ou message d'erreur à afficher.</summary>
public record UploadResult(bool Success, string? ErrorMessage = null, int TaskId = 0);
