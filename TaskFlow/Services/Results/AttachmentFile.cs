namespace TaskFlow.Services.Results;

/// <summary>
/// Descripteur d'un fichier à renvoyer au navigateur (téléchargement), produit par
/// le service. Type de RÉSULTAT interne, PAS un DTO : il porte un
/// <c>PhysicalPath</c> (chemin disque du serveur) qu'on ne sérialise jamais vers un
/// client. D'où sa place dans <c>Services/Results/</c> et son nom sans suffixe « Dto ».
/// </summary>
public record AttachmentFile(string PhysicalPath, string ContentType, string DownloadName);
