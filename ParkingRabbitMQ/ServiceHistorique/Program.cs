using ServiceHistorique.EventStore;
using ServiceHistorique.Projections;
using ServiceHistorique.Services;

var builder = WebApplication.CreateBuilder(args);

// Event Store en Singleton (partagé entre RabbitMQ listener et API)
builder.Services.AddSingleton<IEventStore, InMemoryEventStore>();

// Projection (stateless — recalcule à chaque appel depuis l'Event Store)
builder.Services.AddTransient<IPlaceProjection, PlaceProjection>();

// Listener RabbitMQ en arrière-plan
builder.Services.AddHostedService<RabbitMqListenerService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new() { Title = "Service Historique — Event Store", Version = "v1" }));

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

// Port différent du ServicePlaces
app.Run("http://localhost:5002");

 