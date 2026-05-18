using ParkingGrpc.Services;
using ParkingConsole.Repositories;
using ParkingConsole.Services;

var builder = WebApplication.CreateBuilder(args);

// =============================================
// SERVICES
// =============================================

// Ajouter les services gRPC
builder.Services.AddGrpc();

// Enregistrer les repositories et services métier
builder.Services.AddSingleton<IPlaceRepository, PlaceRepository>();
builder.Services.AddSingleton<ICapteurRepository, CapteurRepository>();
builder.Services.AddSingleton<IEvenementRepository, EvenementRepository>();
builder.Services.AddScoped<IPlaceService, PlaceService>();
builder.Services.AddSingleton<PlaceEventService>();
var app = builder.Build();

// =============================================
// MIDDLEWARE
// =============================================

// Mapper le service gRPC
app.MapGrpcService<PlaceGrpcService>();

// Endpoint pour vérifier que le serveur fonctionne
app.MapGet("/", () => "Communication avec les endpoints gRPC doit être effectuée via un client gRPC. Pour apprendre à créer un client, visitez : https://go.microsoft.com/fwlink/?linkid=2086909");

// =============================================
// DÉMARRAGE
// =============================================

Console.WriteLine("🚀 Serveur gRPC démarré !");
Console.WriteLine("📡 Écoute sur : http://localhost:5000 et https://localhost:5001");

app.Run();
