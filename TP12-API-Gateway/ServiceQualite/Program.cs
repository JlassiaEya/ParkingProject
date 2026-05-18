using ServiceQualite.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IQualiteRepository, QualiteRepository>();
builder.Services.AddHostedService<MqttQualiteBackgroundService>();
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
