using Microsoft.AspNetCore.Mvc;

namespace ServiceQualite.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly string _serviceName;

    public StatusController(IConfiguration config)
    {
        _serviceName = config["ServiceName"] ?? "Service inconnu";
    }

    [HttpGet]
    public IActionResult Get() =>
        Ok(new { Service = _serviceName, Status = "OK", Heure = DateTime.UtcNow });
}
