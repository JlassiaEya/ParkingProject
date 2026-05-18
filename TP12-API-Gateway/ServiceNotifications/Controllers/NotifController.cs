using Microsoft.AspNetCore.Mvc;
using ServiceNotifications.Services;

namespace ServiceNotifications.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotifController : ControllerBase
{
    private readonly INotificationRepository _repo;

    public NotifController(INotificationRepository repo) => _repo = repo;

    [HttpGet]
    public IActionResult Get() => Ok(_repo.GetDernieresAlertes());
}
