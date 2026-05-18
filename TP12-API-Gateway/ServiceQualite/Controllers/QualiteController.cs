using Microsoft.AspNetCore.Mvc;
using ServiceQualite.Services;

namespace ServiceQualite.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QualiteController : ControllerBase
{
    private readonly IQualiteRepository _repo;
    public QualiteController(IQualiteRepository repo) => _repo = repo;

    [HttpGet] 
    public IActionResult Get() 
    {
        var val = _repo.GetDerniereMesure();
        if (val == null) return NotFound("Aucune donnee disponible");
        return Ok(val);
    }

    [HttpGet("historique")] 
    public IActionResult GetHistorique() => Ok(_repo.GetHistorique());
}
