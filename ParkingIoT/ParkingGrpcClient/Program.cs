using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using ParkingGrpc.Protos;
using ParkingGrpcClient;

Console.WriteLine("╔══════════════════════════════════════════╗");
Console.WriteLine("║   CLIENT gRPC - Parking Intelligent     ║");
Console.WriteLine("╚══════════════════════════════════════════╝\n");

Console.WriteLine("Choisissez un mode :");
Console.WriteLine("1. Tests unary (TP 5)");
Console.WriteLine("2. Server streaming (WatchPlaces)");
Console.WriteLine("3. Client streaming (Simulation capteur)");
Console.WriteLine("4. Bidirectional streaming (Contrôle interactif)");
Console.WriteLine("5. Comparaison streaming");
Console.WriteLine("5. Tout");


Console.Write("\nVotre choix : ");

var choice = Console.ReadLine();

if (choice == "3" )
{
    await SensorSimulator.RunAsync();
}

if (choice == "4" )
{
    await InteractiveControl.RunAsync();
}
if (choice == "5")
{
    await StreamingComparison.RunAsync();
}
// =============================================
// SERVER STREAMING
// =============================================

if (choice == "2")
{
    await WatchPlacesClient.RunAsync();
}


// =============================================
// TESTS UNARY
// =============================================

if (choice == "1" || choice == "3")
{

    using var channel = GrpcChannel.ForAddress("http://localhost:5000");
    var client = new PlaceService.PlaceServiceClient(channel);

    Console.WriteLine("✅ Connexion au serveur gRPC établie\n");


    // =============================================
    // TEST 1 : GetAllPlaces
    // =============================================

    Console.WriteLine("━━━ Test 1 : GetAllPlaces ━━━\n");

    var allPlacesResponse = await client.GetAllPlacesAsync(new ParkingGrpc.Protos.Empty());

    Console.WriteLine($"Nombre de places récupérées : {allPlacesResponse.Places.Count}\n");

    foreach (var place in allPlacesResponse.Places)
    {
        string etat = place.EstOccupee ? "OCCUPÉE" : "LIBRE";
        Console.WriteLine($"  Place n°{place.Numero} | Étage {place.Etage} | {etat}");
    }


    // =============================================
    // TEST 2 : GetPlace
    // =============================================

    Console.WriteLine("\n━━━ Test 2 : GetPlace (ID 1) ━━━\n");

    var placeRequest = new PlaceRequest { Id = 1 };
    var placeResponse = await client.GetPlaceAsync(placeRequest);

    if (placeResponse.Success)
    {
        var place = placeResponse.Place;

        Console.WriteLine($"✅ {placeResponse.Message}");
        Console.WriteLine($"Place n°{place.Numero}");
        Console.WriteLine($"Étage : {place.Etage}");
        Console.WriteLine($"État : {(place.EstOccupee ? "Occupée" : "Libre")}");
        Console.WriteLine($"Description : {place.Description}");
    }
    else
    {
        Console.WriteLine($"❌ {placeResponse.Message}");
    }


    // =============================================
    // TEST 3 : Place inexistante
    // =============================================

    Console.WriteLine("\n━━━ Test 3 : GetPlace (ID 999) ━━━\n");

    var invalidRequest = new PlaceRequest { Id = 999 };
    var invalidResponse = await client.GetPlaceAsync(invalidRequest);

    Console.WriteLine(invalidResponse.Success
        ? $"✅ {invalidResponse.Message}"
        : $"❌ {invalidResponse.Message}");


    // =============================================
    // TEST 4 : UpdatePlace (occuper)
    // =============================================

    Console.WriteLine("\n━━━ Test 4 : Occuper la place 2 ━━━\n");

    var updateRequest = new UpdatePlaceRequest
    {
        Id = 2,
        EstOccupee = true
    };

    var updateResponse = await client.UpdatePlaceAsync(updateRequest);

    if (updateResponse.Success)
    {
        var place = updateResponse.Place;
        Console.WriteLine($"✅ {updateResponse.Message}");
        Console.WriteLine($"Place n°{place.Numero} est maintenant {(place.EstOccupee ? "OCCUPÉE" : "LIBRE")}");
    }


    // =============================================
    // TEST 5 : Libérer la place
    // =============================================

    Console.WriteLine("\n━━━ Test 5 : Libérer la place 2 ━━━\n");

    var releaseRequest = new UpdatePlaceRequest
    {
        Id = 2,
        EstOccupee = false
    };

    var releaseResponse = await client.UpdatePlaceAsync(releaseRequest);

    if (releaseResponse.Success)
    {
        var place = releaseResponse.Place;
        Console.WriteLine($"✅ {releaseResponse.Message}");
        Console.WriteLine($"Place n°{place.Numero} est maintenant {(place.EstOccupee ? "OCCUPÉE" : "LIBRE")}");
    }

    // =============================================
    // TEST 8 : JSON vs Protobuf
    // =============================================

    Console.WriteLine("\n━━━ Test 8 : JSON vs Protobuf ━━━\n");

    var allPlaces = await client.GetAllPlacesAsync(new ParkingGrpc.Protos.Empty());

    var jsonBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(allPlaces.Places);
    Console.WriteLine($"Taille JSON : {jsonBytes.Length} octets");

    var protoBytes = allPlaces.ToByteArray();
    Console.WriteLine($"Taille Protobuf : {protoBytes.Length} octets");


    Console.WriteLine("\n╔══════════════════════════════════════════╗");
    Console.WriteLine("║   Tests terminés avec succès !           ║");
    Console.WriteLine("╚══════════════════════════════════════════╝");


    // =============================================
    // BENCHMARK
    // =============================================

    Console.WriteLine("\n⏱️ Lancer benchmark REST vs gRPC ? (o/n)");
    var rep = Console.ReadLine();

    if (rep?.ToLower() == "o")
    {
        Console.WriteLine("Assurez-vous que l'API REST est lancée");
        Console.ReadLine();

        var benchmark = new Benchmark();
        await benchmark.RunBenchmark(1000);
    }
}