namespace ParkingApi.Models;

/// <summary>
/// Réponse en cas d'erreur de validation
/// </summary>
public class ValidationErrorResponse
{
    public string Type { get; set; } = "ValidationError";
    public string Title { get; set; } = "Une ou plusieurs erreurs de validation se sont produites.";
    public int Status { get; set; } = 400;
    public Dictionary<string, List<string>> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
