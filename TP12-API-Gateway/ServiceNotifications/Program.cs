using ServiceNotifications.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<INotificationRepository, NotificationRepository>();
builder.Services.AddHostedService<NotificationConsumerService>();
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
