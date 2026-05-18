using ServicePlaces.GrpcServices;
using ServicePlaces.Models;
using ServicePlaces.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──────────────────────────────────────────────────────────

builder.Services.AddSingleton<IPlaceRepository, PlaceRepository>();
builder.Services.AddSingleton<AlerteRabbitMqService>();
builder.Services.AddHostedService<MqttBackgroundService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Service Places — API",
        Version = "v1",
        Description = "Microservice de gestion des places de parking (IoT + REST)"
    });
});
builder.Services.Configure<MqttOptions>(
    builder.Configuration.GetSection("Mqtt"));
// Ajouter dans les services
builder.Services.AddGrpc();
builder.Services.AddSingleton<PlaceChangeNotifier>();



// ✅ CORRECTION 1 : forcer 301 au lieu de 307
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status301MovedPermanently;
    options.HttpsPort = 5443;
});

var app = builder.Build();
app.MapGrpcService<PlaceGrpcService>();
var repo = app.Services.GetRequiredService<IPlaceRepository>() as PlaceRepository;
var alertes = app.Services.GetRequiredService<AlerteRabbitMqService>();
if (repo is not null) repo.AlerteService = alertes;

// ── Pipeline ──────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI();

//app.UseHttpsRedirection(); 

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.MapControllers();


app.Run();