
using System.ComponentModel.DataAnnotations;

namespace ParkingApi.Models;

public class UpdatePlaceRequest
{
    [Required(ErrorMessage = "Le champ EstOccupee est obligatoire")]
    public bool EstOccupee { get; set; }
}
