using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ParkingApi.Configuration;
using ParkingApi.HealthChecks;
using ParkingApi.Middleware;
using ParkingConsole.Repositories;
using ParkingConsole.Services;
var builder = WebApplication.CreateBuilder(args);
// =============================================
// CONFIGURATION
// =============================================

// Lier la section ParkingConfiguration à la classe ParkingSettings
builder.Services.Configure<ParkingSettings>(
    builder.Configuration.GetSection(ParkingSettings.SectionName)
);

// =============================================
// CONFIGURATION DES SERVICES
// =============================================

// Ajouter les contrôleurs
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key, // nom du champ
                    kvp => kvp.Value.Errors
                        .Select(e => e.ErrorMessage)
                        .ToArray()
                );

            return new BadRequestObjectResult(new
            {
                errors
            });
        };
    });


// Ajouter Swagger pour la documentation de l'API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Enregistrer les repositories (Singleton = une seule instance partagée)
builder.Services.AddSingleton<IPlaceRepository, PlaceRepository>();
builder.Services.AddSingleton<ICapteurRepository, CapteurRepository>();
builder.Services.AddSingleton<IEvenementRepository, EvenementRepository>();

// Enregistrer les services métier (Scoped = une instance par requête HTTP)
builder.Services.AddScoped<IPlaceService, PlaceService>();

// =============================================
// CONSTRUCTION DE L'APPLICATION
// =============================================
builder.Services.AddHealthChecks()
    .AddCheck<PlaceRepositoryHealthCheck>(
        "place_repository",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "ready", "db" }
    );
builder.Services.AddHealthChecks()
    .AddCheck<OccupationRateHealthCheck>("occupation_rate");

var app = builder.Build();

// =============================================
// CONFIGURATION DU PIPELINE MIDDLEWARE
// =============================================

// Activer Swagger uniquement en développement
app.UseMiddleware<ErrorHandlingMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// Redirection automatique HTTPS
app.UseHttpsRedirection();

// Activer l'autorisation (pour l'instant pas d'authentification)
app.UseAuthorization();

// Mapper les contrôleurs (routes /api/...)
app.MapControllers();
app.MapHealthChecks("/health");
// =============================================
// DÉMARRAGE DE L'APPLICATION
// =============================================
Console.WriteLine("🚀 API Parking Intelligent démarrée !");
Console.WriteLine("📖 Documentation Swagger : https://localhost:7XXX/swagger");

// =============================================
// MIDDLEWARE PIPELINE
// =============================================

// Middleware de gestion d'erreurs global (en premier)

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

