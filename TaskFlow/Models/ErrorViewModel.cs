// View model fourni par le template ASP.NET Core : il alimente la page
// Views/Shared/Error.cshtml affichée par app.UseExceptionHandler("/Home/Error")
// quand une exception non gérée remonte en production.
namespace TaskFlow.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
