using System.ComponentModel.DataAnnotations;

namespace ParkingApi.Models;

/// <summary>
/// DTO pour la création d'une nouvelle place
/// </summary>
public class CreatePlaceRequest
{
    [Required(ErrorMessage = "Le numéro de place est obligatoire")]
    [Range(1, 999, ErrorMessage = "Le numéro doit être entre 1 et 999")]
    public int? Numero { get; set; }

    [Required(ErrorMessage = "L'étage est obligatoire")]
    [Range(1, 10, ErrorMessage = "L'étage doit être entre 1 et 10")]
    public int Etage { get; set; }

    [StringLength(200, ErrorMessage = "La description ne peut pas dépasser 200 caractères")]
    public string Description { get; set; } = "";
}
