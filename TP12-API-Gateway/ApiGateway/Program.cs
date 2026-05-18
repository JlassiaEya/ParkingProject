using ApiGateway.Middleware;
using ApiGateway.Services;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();
builder.Services.AddHttpClient<ProxyService>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    });
builder.Services.AddSingleton<JwtService>();     // 
builder.Services.AddSingleton<GatewayStats>();   // Ex1: Stats
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "API Gateway - Parking IoT", Version = "v1" });
    // Bouton Authorize dans Swagger pour entrer le JWT
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Entrez votre token JWT : Bearer {token}"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement {
        [new Microsoft.OpenApi.Models.OpenApiSecurityScheme {
            Reference = new() {
                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        }] = Array.Empty<string>()
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ─── PIPELINE DE MIDDLEWARES (ordre important) ───────────────────────────
// 0. Statistiques (Total Requests)
app.Use(async (context, next) =>
{
    var stats = context.RequestServices.GetRequiredService<GatewayStats>();
    stats.IncrementTotalRequests();
    await next();
});

// 1. Rate limiting (avant tout, meme avant l'auth)
app.UseMiddleware<RateLimitingMiddleware>();

// 2. Authentification
app.UseMiddleware<JwtAuthenticationMiddleware>();  // <- Remplace AuthenticationMiddleware

// 3. Routing + Controllers
app.MapControllers();

app.Run();
