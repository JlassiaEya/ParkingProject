using CapteurSimule.Services;

Console.Title = "CapteurSimule — Parking IoT";
Console.WriteLine("======================================");
Console.WriteLine("  Simulateur de Capteurs IoT — TP8");
Console.WriteLine("======================================");

// Token d'annulation pour arrêt propre (Ctrl+C)
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    Console.WriteLine("\n[INFO] Arret demande...");
    e.Cancel = true;
    cts.Cancel();
};

var service = new CapteurService();

try
{
    // 1. Se connecter au broker
    await service.ConnecterAsync(cts.Token);

    // 2. Démarrer la publication en boucle
    await service.DemarrerPublicationAsync(cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("[INFO] Simulation terminee.");
}
finally
{
    await service.ArretAsync();
    Console.WriteLine("[INFO] Deconnecte du broker. Au revoir !");
}
