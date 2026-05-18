using Subscriber.Services;

Console.Title = "Subscriber — Parking IoT Dashboard";
Console.WriteLine("======================================");
Console.WriteLine("  Subscriber MQTT — Parking Dashboard");
Console.WriteLine("======================================");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

var service = new SubscriberService();

try
{
    await service.ConnecterEtAbonnerAsync(cts.Token);
    await service.AttendreAsync(cts.Token);
}
catch (OperationCanceledException) { }
finally
{
    await service.ArretAsync();
    Console.WriteLine("[INFO] Subscriber arrete.");
}
