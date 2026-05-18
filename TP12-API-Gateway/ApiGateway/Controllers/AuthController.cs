using ApiGateway.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    // Utilisateurs en memoire (en production : base de donnees + hash des mots de passe)
    private static readonly Dictionary<string, (string PasswordHash, string Role)> Users = new()
    {
        ["admin"]    = ("admin123",   "Administrator"),
        ["operateur"] = ("oper456",   "Operator"),
        ["lecteur"]   = ("read789",   "Reader"),
    };

    public AuthController(JwtService jwtService, ILogger<AuthController> logger)
    {
        _jwtService = jwtService;
        _logger = logger;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { Error = "Identifiants requis" });
        }

        if (!Users.TryGetValue(request.Username, out var user) ||
            user.PasswordHash != request.Password)
        {
            _logger.LogWarning("[Auth] Echec de connexion pour : {User}", request.Username);
            return Unauthorized(new { Error = "Identifiants incorrects" });
        }

        var token = _jwtService.GenerateToken(request.Username, user.Role);

        return Ok(new
        {
            Token = token,
            TokenType = "Bearer",
            ExpiresIn = 3600,
            User = request.Username,
            Role = user.Role
        });
    }
}

public record LoginRequest(string Username, string Password);
